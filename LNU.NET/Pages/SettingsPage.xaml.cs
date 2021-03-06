﻿#region Using
using static Wallace.UWP.Helpers.Tools.UWPStates;
using static LNU.NET.Pages.SettingsPage.InsideResources;

using Edi.UWP.Helpers;
using Wallace.UWP.Helpers.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using LNU.Core.Tools;
using LNU.NET.Controls;
using Windows.Storage;
using Windows.Storage.FileProperties;
using LNU.NET.Tools;
using LNU.NET.Pages.FeaturesPages;
using LNU.Core.Models;
using Wallace.UWP.Helpers.Tools;
using Windows.UI;
#endregion

namespace LNU.NET.Pages {
 
    public sealed partial class SettingsPage : Page {

        #region Constructor
        public SettingsPage() {
            this.InitializeComponent();
            Current = this;
            this.NavigationCacheMode = NavigationCacheMode.Required;
            InitSettingsPageState();
        }
        #endregion

        #region Events
        protected override void OnNavigatedTo(NavigationEventArgs e) {
            MainPage.ChangeTitlePath(2, GetUIString("SettingsString"));
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var item = (sender as Pivot).SelectedItem as PivotItem;
            if (item.Name == PrivacyPolicy.Name)
                PolicyRing.IsActive = true;
        }

        private async void FeedBackBtn_Click(object sender, RoutedEventArgs e) {
            await ReportError(null, "N/A", true);
        }

        private void Switch_Toggled(object sender, RoutedEventArgs e) {
            GetSwitchHandler((sender as ToggleSwitch).Name)
               .Invoke((sender as ToggleSwitch).Name);
        }

        private void LanguageCombox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var newLanguage = GetLanguageTag(
                    GetComboItemInstance(
                        (e.AddedItems.FirstOrDefault() as ComboBoxItem)
                        .Name as string));
            SettingsHelper.SaveSettingsValue(SettingsSelect.Language, newLanguage);
            if (isInitViewOrNot) { isInitViewOrNot = false; return; }
            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = newLanguage;
            new ToastSmooth(GetUIString("ReStartToChangeLanguage")).Show();
        }

        /// <summary>
        /// *(Important) Change divide percent value when slider action finished.
        /// </summary>
        /// <param name="sender">slider</param>
        /// <param name="e">args</param>
        private void SplitSizeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e) {
            isNeedSaveSliderValue = false;
            if (isInitSliderValueOrNot) {
                isInitSliderValueOrNot = false;
                return;
            }
            if (timerForSlider == null) {
                timerForSlider = new DispatcherTimer();
                timerForSlider.Tick += (obj, args) => {
                    if (isNeedSaveSliderValue) {
                        timerForSlider.Stop();
                        ChangeSplitViewWidth(SplitSizeSlider.Value);
                        timerForSlider = null;
                    }
                    isNeedSaveSliderValue = true;
                };
                timerForSlider.Interval = new TimeSpan(0, 0, 0, 0, 200);
                timerForSlider.Start();
            }
            if (!isInitSliderValueOrNot)
                SettingsHelper.SaveSettingsValue(SettingsSelect.SplitViewMode, e.NewValue / 100);
        }

        private async void CacheClearBtn_Click(object sender, RoutedEventArgs e) {
            CacheClearBtn.IsEnabled = false;
            ClearRing.IsActive = true;
            await ClearCacheSize();
            CacheClearBtn.IsEnabled = true;
            ClearRing.IsActive = false;
        }

        private void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args) {
            
        }

        private void WebView_ContentLoading(WebView sender, WebViewContentLoadingEventArgs args) {
            PolicyRing.IsActive = true;
        }

        private void WebView_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args) {
            PolicyRing.IsActive = false;
            SetVisibility(PolicyRing, false);
        }

        #endregion

        #region Methods

        private async void InitSettingsPageState() {
            VersionMessage.Text = GetUIString("VersionMessage") + Utils.GetAppVersion();
            ThemeSwitch.IsOn = (bool?)SettingsHelper.ReadSettingsValue(SettingsConstants.IsDarkThemeOrNot) ?? true;
            LanguageCombox.SelectedItem = GetComboItemFromTag((string)SettingsHelper.ReadSettingsValue(SettingsSelect.Language) ?? ConstFields.English_US);
            PureItemSwitch.IsOn = (bool?)SettingsHelper.ReadSettingsValue(SettingsSelect.IsPureColorItem) ?? false;
            ScreenSwitch.IsOn = (bool?)SettingsHelper.ReadSettingsValue(SettingsSelect.IsDivideScreen) ?? true;
            SplitSizeSlider.Value = 100 * ((double?)SettingsHelper.ReadSettingsValue(SettingsSelect.SplitViewMode) ?? 0.6);
            ScreenSwitch.IsEnabled = !IsMobile;
            SplitSizeSlider.IsEnabled = !IsMobile;
            await ShowCacheSize();
        }

        private static void ChangeSplitViewWidth(double value) {
            if (ContentPage.Current != null)
                MainPage.DivideWindowRange(
                    ContentPage.Current, 
                    divideNum : value / 100, 
                    isDivideScreen: Current.ScreenSwitch.IsOn);
            if (LoginPage.Current != null)
                MainPage.DivideWindowRange(
                    LoginPage.Current,
                    divideNum : value / 100,
                    isDivideScreen: Current.ScreenSwitch.IsOn);
            if (WebViewPage.Current != null)
                MainPage.DivideWindowRange(
                    WebViewPage.Current,
                    divideNum: value / 100,
                    isDivideScreen: Current.ScreenSwitch.IsOn);
            if (ChangePassPage.Current != null)
                MainPage.DivideWindowRange(
                    ChangePassPage.Current,
                    divideNum: value / 100,
                    isDivideScreen: Current.ScreenSwitch.IsOn);
            if (SchedulePage.Current != null)
                MainPage.DivideWindowRange(
                    SchedulePage.Current,
                    divideNum: value / 100,
                    isDivideScreen: Current.ScreenSwitch.IsOn);
        }

        #region Toggle Events

        private void OnThemeSwitchToggled(ToggleSwitch sender) {
            SettingsHelper.SaveSettingsValue(SettingsConstants.IsDarkThemeOrNot, sender.IsOn);
            MainPage.Current.RequestedTheme = sender.IsOn ? ElementTheme.Dark : ElementTheme.Light;
            if (isInitViewOrNot)
                return;
            StatusBarInit.InitDesktopStatusBar(!sender.IsOn, Colors.Black, Color.FromArgb(255, 67, 104, 203), Colors.White, Color.FromArgb(255, 202, 0, 62));
            StatusBarInit.InitMobileStatusBar(!sender.IsOn, Colors.Black, Color.FromArgb(255, 67, 104, 203), Colors.White, Color.FromArgb(255, 202, 0, 62));
            if (SchedulePage.Current != null)
                MainPage.Current.NavigateToBase?.Invoke(
                    this,
                    new NavigateParameter {
                        MessageBag = GetUIString("LNU_Index_SC"),
                        NaviType = NavigateType.Schedule,
                        ToFetchType = DataFetchType.NULL,
                        ToUri = new Uri("http://jwgl.lnu.edu.cn/pls/wwwbks/xk.CourseView")
                    }, 
                    MainPage.InnerResources.GetFrameInstance(NavigateType.Schedule),
                    MainPage.InnerResources .GetPageType( NavigateType.Schedule));
        }

        private void OnScreenSwitchToggled(ToggleSwitch sender) {
            SettingsHelper.SaveSettingsValue(SettingsSelect.IsDivideScreen, sender.IsOn);
            if (LoginPage.Current != null)
                MainPage.DivideWindowRange(
                    LoginPage.Current,
                    divideNum: (double?)SettingsHelper.ReadSettingsValue(SettingsSelect.SplitViewMode) ?? 0.6,
                    isDivideScreen: sender.IsOn);
            if (ContentPage.Current != null)
                MainPage.DivideWindowRange(
                    ContentPage.Current,
                    divideNum: (double?)SettingsHelper.ReadSettingsValue(SettingsSelect.SplitViewMode) ?? 0.6,
                    isDivideScreen: sender.IsOn);
            if (WebViewPage.Current != null)
                MainPage.DivideWindowRange(
                    WebViewPage.Current,
                    divideNum: (double?)SettingsHelper.ReadSettingsValue(SettingsSelect.SplitViewMode) ?? 0.6,
                    isDivideScreen: sender.IsOn);
            if (ChangePassPage.Current != null)
                MainPage.DivideWindowRange(
                    ChangePassPage.Current,
                    divideNum: (double?)SettingsHelper.ReadSettingsValue(SettingsSelect.SplitViewMode) ?? 0.6,
                    isDivideScreen: sender.IsOn);
            if (SchedulePage.Current != null)
                MainPage.DivideWindowRange(
                    SchedulePage.Current,
                    divideNum: (double?)SettingsHelper.ReadSettingsValue(SettingsSelect.SplitViewMode) ?? 0.6,
                    isDivideScreen: sender.IsOn);
        }

        private void OnPureItemSwitchToggled(ToggleSwitch sender) {
            SettingsHelper.SaveSettingsValue(SettingsSelect.IsPureColorItem, sender.IsOn);
            if (IndexPage.Current != null)
                IndexPage.InsideMapHelper.ChangeImageVisibility(!sender.IsOn);
        }

        #endregion

        #region Cache Methods

        private async Task ShowCacheSize() {
            var localCF = ApplicationData.Current.LocalCacheFolder;
            var folders = await localCF.GetFoldersAsync();
            double sizeOfCache = 0.00;
            BasicProperties propertiesOfItem;
            foreach (var item in folders) {
                var files = await item.GetFilesAsync(Windows.Storage.Search.CommonFileQuery.DefaultQuery);
                foreach (var file in files) {
                    propertiesOfItem = await file.GetBasicPropertiesAsync();
                    sizeOfCache += propertiesOfItem.Size;
                }
            }
            var sizeInMb = sizeOfCache / (1024 * 1024);
            CacheSizeTitle.Text = sizeInMb - 0 > 0.00000001 ? sizeInMb.ToString("#.##") + "MB" : "0 MB";
        }

        private async Task ClearCacheSize() {
            var localCF = ApplicationData.Current.LocalCacheFolder;
            var folders = await localCF.GetFoldersAsync();
            foreach (var item in folders) {
                var files = await item.GetFilesAsync(Windows.Storage.Search.CommonFileQuery.DefaultQuery);
                foreach (var file in files) {
                    await file.DeleteAsync();
                }
            }
            await ShowCacheSize();
        }

        #endregion

        #region Error Reporter
        /// <summary>
        /// ReportError Method
        /// </summary>
        /// <param name="viewName"></param>
        /// <param name="msg"></param>
        /// <param name="pageSummary"></param>
        /// <param name="includeDeviceInfo"></param>
        /// <returns></returns>
        public static async Task ReportError(string msg = null, string pageSummary = "N/A", bool includeDeviceInfo = true) {
            var deviceInfo = new EasClientDeviceInformation();

            string subject = GetUIString("Feedback_Subject");
            string body = $"{GetUIString("Feedback_Body")}：{msg}  " +
                          $"（{GetUIString("Feedback_Version")}：{Utils.GetAppVersion()} ";

            if (includeDeviceInfo) {
                body += $", {GetUIString("Feedback_FriendlyName")}：{deviceInfo.FriendlyName}, " +
                          $"{GetUIString("Feedback_OS")}：{deviceInfo.OperatingSystem}, " +
                          $"SKU：{deviceInfo.SystemSku}, " +
                          $"{GetUIString("Feedback_SPN")}：{deviceInfo.SystemProductName}, " +
                          $"{GetUIString("Feedback_SMF")}：{deviceInfo.SystemManufacturer}, " +
                          $"{GetUIString("Feedback_SFV")}：{deviceInfo.SystemFirmwareVersion}, " +
                          $"{GetUIString("Feedback_SHV")}：{deviceInfo.SystemHardwareVersion}）";
            } else {
                body += ")";
            }

            string to = "miao17game@qq.com";
            await Tasks.OpenEmailComposeAsync(to, subject, body);
        }

        #endregion

        #endregion

        #region Inside resources class
        internal static class InsideResources {

            public static ComboBoxItem GetComboItemInstance(string buttonName) { return comboItemsMaps.ContainsKey(buttonName) ? comboItemsMaps[buttonName] : null; }
            public static string GetLanguageTag(ComboBoxItem button) { return languageSaveTagsMaps.ContainsKey(button) ? languageSaveTagsMaps[button] : null; }
            public static ComboBoxItem GetComboItemFromTag(string tag) { return languageSaveTagsMaps.ContainsValue(tag) ? languageSaveTagsMaps.Where(i => i.Value == tag).FirstOrDefault().Key : null; }
            static private Dictionary<string, ComboBoxItem> comboItemsMaps = new Dictionary<string, ComboBoxItem> {
                { Current.enUSSelect.Name,Current.enUSSelect},
                { Current.zhCNSelect.Name,Current.zhCNSelect},
        };
            static private Dictionary<ComboBoxItem, string> languageSaveTagsMaps = new Dictionary<ComboBoxItem, string> {
                { Current.enUSSelect,ConstFields.English_US},
                { Current.zhCNSelect,ConstFields.Chinese_CN},
        };

            public static ToggleSwitch GetSwitchInstance(string str) { return switchSettingsMaps.ContainsKey(str) ? switchSettingsMaps[str] : null; }
            static private Dictionary<string, ToggleSwitch> switchSettingsMaps = new Dictionary<string, ToggleSwitch> {
                { Current.ThemeSwitch.Name,Current.ThemeSwitch},
                { Current.ScreenSwitch.Name,Current.ScreenSwitch},
                { Current.PureItemSwitch.Name,Current.PureItemSwitch },
        };

            public static SwitchEventHandler GetSwitchHandler(string switchName) { return switchHandlerMaps.ContainsKey(switchName) ? switchHandlerMaps[switchName] : null; }
            static private Dictionary<string, SwitchEventHandler> switchHandlerMaps = new Dictionary<string, SwitchEventHandler> {
                { Current.ThemeSwitch.Name, new SwitchEventHandler(instance=> { Current.OnThemeSwitchToggled(GetSwitchInstance(instance)); }) },
                { Current.ScreenSwitch.Name, new SwitchEventHandler(instance=> { Current.OnScreenSwitchToggled(GetSwitchInstance(instance)); }) },
                { Current.PureItemSwitch.Name, new SwitchEventHandler(instance=> { Current.OnPureItemSwitchToggled(GetSwitchInstance(instance)); }) },
        };

        }
        #endregion

        #region Properties and state
        /// <summary>
        /// If first load this page or not.
        /// </summary>
        private bool isInitViewOrNot = true;
        private bool isInitSliderValueOrNot = true;
        private bool isNeedSaveSliderValue = true;
        private DispatcherTimer timerForSlider;
        public static SettingsPage Current;
        public delegate void SwitchEventHandler(string instance);
        #endregion

    }
}
