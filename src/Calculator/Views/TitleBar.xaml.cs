using CalculatorApp.ViewModel.Common;
using System;
using System.ComponentModel;
using Windows.ApplicationModel.Core;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace CalculatorApp
{
    public sealed partial class TitleBar : UserControl
    {
        public TitleBar()
        {
            m_coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            m_uiSettings = new UISettings();
            m_accessibilitySettings = new AccessibilitySettings();
            InitializeComponent();

            m_coreTitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(BackgroundElement);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
#if IS_STORE_BUILD
            AppName.Text = AppResourceProvider.GetInstance().GetResourceString("AppName");

#else
            AppName.Text = AppResourceProvider.GetInstance().GetResourceString("DevAppName");
#endif
        }

        public bool IsAlwaysOnTopMode
        {
            get { return (bool)GetValue(IsAlwaysOnTopModeProperty); }
            set { SetValue(IsAlwaysOnTopModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsAlwaysOnTopMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsAlwaysOnTopModeProperty =
            DependencyProperty.Register(nameof(IsAlwaysOnTopMode), typeof(bool), typeof(TitleBar), new PropertyMetadata(default(bool), (sender, args) =>
            {
                var self = (TitleBar)sender;
                self.OnIsAlwaysOnTopModePropertyChanged((bool)args.OldValue, (bool)args.NewValue);
            }));

        public event Windows.UI.Xaml.RoutedEventHandler AlwaysOnTopClick;
        public event Windows.UI.Xaml.RoutedEventHandler BackButtonClick;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Register events
            m_coreTitleBar.IsVisibleChanged += CoreTitleBarIsVisibleChanged;
            m_coreTitleBar.LayoutMetricsChanged += CoreTitleBarLayoutMetricsChanged;

            m_uiSettings.ColorValuesChanged += ColorValuesChanged;
            m_accessibilitySettings.HighContrastChanged += OnHighContrastChanged;
            Window.Current.Activated += OnWindowActivated;

            // Register the system back requested event
            SystemNavigationManager.GetForCurrentView().BackRequested += System_BackRequested;

            // Register RequestedTheme changed callback to update title bar system button colors.
            m_rootFrameRequestedThemeCallbackToken =
                Utils.ThemeHelper.RegisterAppThemeChangedCallback(RootFrame_RequestedThemeChanged);

            // Set properties
            SetTitleBarControlColors();
            SetTitleBarHeightAndPadding();

            // As of Windows 10 1903: when an app runs on a PC (without Tablet mode activated)
            // properties of CoreApplicationViewTitleBar aren't initialized during the first seconds after launch.
            var forceDisplay = AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop"
                && UIViewSettings.GetForCurrentView().UserInteractionMode == UserInteractionMode.Mouse;

            SetTitleBarVisibility(forceDisplay);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Unregister events
            m_coreTitleBar.LayoutMetricsChanged -= CoreTitleBarLayoutMetricsChanged;
            m_coreTitleBar.IsVisibleChanged -= CoreTitleBarIsVisibleChanged;
            m_uiSettings.ColorValuesChanged -= ColorValuesChanged;
            m_accessibilitySettings.HighContrastChanged -= OnHighContrastChanged;
            Window.Current.Activated -= OnWindowActivated;

            SystemNavigationManager.GetForCurrentView().BackRequested -= System_BackRequested;

            Utils.ThemeHelper.
                UnregisterAppThemeChangedCallback(m_rootFrameRequestedThemeCallbackToken);
        }

        private void System_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (!e.Handled && BackButton.IsEnabled)
            {
                var buttonPeer = new ButtonAutomationPeer(BackButton);
                IInvokeProvider invokeProvider = buttonPeer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProvider.Invoke();

                e.Handled = true;
            }
        }

        private void RootFrame_RequestedThemeChanged(DependencyObject sender, DependencyProperty dp)
        {
            if(Frame.RequestedThemeProperty == dp)
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() => { SetTitleBarControlColors(); }));
            }
        }

        private void CoreTitleBarIsVisibleChanged(CoreApplicationViewTitleBar cTitleBar, object args)
        {
            SetTitleBarVisibility(false);
        }

        private void CoreTitleBarLayoutMetricsChanged(CoreApplicationViewTitleBar cTitleBar, object args)
        {
            SetTitleBarHeightAndPadding();
        }

        private void SetTitleBarVisibility(bool forceDisplay)
        {
            LayoutRoot.Visibility =
                forceDisplay || m_coreTitleBar.IsVisible || IsAlwaysOnTopMode ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetTitleBarHeightAndPadding()
        {
            if (m_coreTitleBar.Height == 0)
            {
                // The titlebar isn't init
                return;
            }

            double leftAddition = 0;
            double rightAddition = 0;

            if (FlowDirection == FlowDirection.LeftToRight)
            {
                leftAddition = m_coreTitleBar.SystemOverlayLeftInset;
                rightAddition = m_coreTitleBar.SystemOverlayRightInset;
            }
            else
            {
                leftAddition = m_coreTitleBar.SystemOverlayRightInset;
                rightAddition = m_coreTitleBar.SystemOverlayLeftInset;
            }

            LayoutRoot.Padding = new Thickness(leftAddition, 0, rightAddition, 0);
            this.Height = m_coreTitleBar.Height;
        }

        private void ColorValuesChanged(Windows.UI.ViewManagement.UISettings sender, object e)
        {
            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() => { SetTitleBarControlColors(); }));
        }

        private void SetTitleBarControlColors()
        {
            var applicationView = ApplicationView.GetForCurrentView();
            if (applicationView == null)
            {
                return;
            }

            var applicationTitleBar = applicationView.TitleBar;
            if (applicationTitleBar == null)
            {
                return;
            }

            if (m_accessibilitySettings.HighContrast)
            {
                // Reset to use default colors.
                applicationTitleBar.ButtonBackgroundColor = null;
                applicationTitleBar.ButtonForegroundColor = null;
                applicationTitleBar.ButtonInactiveBackgroundColor = null;
                applicationTitleBar.ButtonInactiveForegroundColor = null;
                applicationTitleBar.ButtonHoverBackgroundColor = null;
                applicationTitleBar.ButtonHoverForegroundColor = null;
                applicationTitleBar.ButtonPressedBackgroundColor = null;
                applicationTitleBar.ButtonPressedForegroundColor = null;
            }
            else
            {
                applicationTitleBar.ButtonBackgroundColor = ButtonBackground?.Color;
                applicationTitleBar.ButtonForegroundColor = ButtonForeground?.Color;
                applicationTitleBar.ButtonInactiveBackgroundColor = ButtonInactiveBackground?.Color;
                applicationTitleBar.ButtonInactiveForegroundColor = ButtonInactiveForeground?.Color;
                applicationTitleBar.ButtonHoverBackgroundColor = ButtonHoverBackground?.Color;
                applicationTitleBar.ButtonHoverForegroundColor = ButtonHoverForeground?.Color;
                applicationTitleBar.ButtonPressedBackgroundColor = ButtonPressedBackground?.Color;
                applicationTitleBar.ButtonPressedForegroundColor = ButtonPressedForeground?.Color;
            }
        }

        private void OnHighContrastChanged(Windows.UI.ViewManagement.AccessibilitySettings sender, object args)
        {
            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() =>
            {
                SetTitleBarControlColors();
                SetTitleBarVisibility(false);
            }));
        }

        private void OnWindowActivated(object sender, WindowActivatedEventArgs e)
        {
            VisualStateManager.GoToState(
                this, e.WindowActivationState == CoreWindowActivationState.Deactivated ? WindowNotFocused.Name : WindowFocused.Name, false);
        }

        private void OnIsAlwaysOnTopModePropertyChanged(bool oldValue, bool newValue)
        {
            SetTitleBarVisibility(false);
            VisualStateManager.GoToState(this, newValue ? "AOTMiniState" : "AOTNormalState", false);
        }

        private void AlwaysOnTopButton_Click(object sender, RoutedEventArgs e)
        {
            AlwaysOnTopClick?.Invoke(this, e);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackButtonClick?.Invoke(this, e);
        }

        // Dependency properties for the color of the system title bar buttons
        public Windows.UI.Xaml.Media.SolidColorBrush ButtonBackground
        {
            get { return (Windows.UI.Xaml.Media.SolidColorBrush)GetValue(ButtonBackgroundProperty); }
            set { SetValue(ButtonBackgroundProperty, value); }
        }
        public static readonly DependencyProperty ButtonBackgroundProperty =
            DependencyProperty.Register(nameof(ButtonBackground), typeof(Windows.UI.Xaml.Media.SolidColorBrush), typeof(TitleBar), new PropertyMetadata(null));

        public Windows.UI.Xaml.Media.SolidColorBrush ButtonForeground
        {
            get { return (Windows.UI.Xaml.Media.SolidColorBrush)GetValue(ButtonForegroundProperty); }
            set { SetValue(ButtonForegroundProperty, value); }
        }
        public static readonly DependencyProperty ButtonForegroundProperty =
            DependencyProperty.Register(nameof(ButtonForeground), typeof(Windows.UI.Xaml.Media.SolidColorBrush), typeof(TitleBar), new PropertyMetadata(null));

        public Windows.UI.Xaml.Media.SolidColorBrush ButtonInactiveBackground
        {
            get { return (Windows.UI.Xaml.Media.SolidColorBrush)GetValue(ButtonInactiveBackgroundProperty); }
            set { SetValue(ButtonInactiveBackgroundProperty, value); }
        }
        public static readonly DependencyProperty ButtonInactiveBackgroundProperty =
            DependencyProperty.Register(nameof(ButtonInactiveBackground), typeof(Windows.UI.Xaml.Media.SolidColorBrush), typeof(TitleBar), new PropertyMetadata(null));

        public Windows.UI.Xaml.Media.SolidColorBrush ButtonInactiveForeground
        {
            get { return (Windows.UI.Xaml.Media.SolidColorBrush)GetValue(ButtonInactiveForegroundProperty); }
            set { SetValue(ButtonInactiveForegroundProperty, value); }
        }
        public static readonly DependencyProperty ButtonInactiveForegroundProperty =
            DependencyProperty.Register(nameof(ButtonInactiveForeground), typeof(Windows.UI.Xaml.Media.SolidColorBrush), typeof(TitleBar), new PropertyMetadata(null));

        public Windows.UI.Xaml.Media.SolidColorBrush ButtonHoverBackground
        {
            get { return (Windows.UI.Xaml.Media.SolidColorBrush)GetValue(ButtonHoverBackgroundProperty); }
            set { SetValue(ButtonHoverBackgroundProperty, value); }
        }
        public static readonly DependencyProperty ButtonHoverBackgroundProperty =
            DependencyProperty.Register(nameof(ButtonHoverBackground), typeof(Windows.UI.Xaml.Media.SolidColorBrush), typeof(TitleBar), new PropertyMetadata(null));

        public Windows.UI.Xaml.Media.SolidColorBrush ButtonHoverForeground
        {
            get { return (Windows.UI.Xaml.Media.SolidColorBrush)GetValue(ButtonHoverForegroundProperty); }
            set { SetValue(ButtonHoverForegroundProperty, value); }
        }
        public static readonly DependencyProperty ButtonHoverForegroundProperty =
            DependencyProperty.Register(nameof(ButtonHoverForeground), typeof(Windows.UI.Xaml.Media.SolidColorBrush), typeof(TitleBar), new PropertyMetadata(null));

        public Windows.UI.Xaml.Media.SolidColorBrush ButtonPressedBackground
        {
            get { return (Windows.UI.Xaml.Media.SolidColorBrush)GetValue(ButtonPressedBackgroundProperty); }
            set { SetValue(ButtonPressedBackgroundProperty, value); }
        }
        public static readonly DependencyProperty ButtonPressedBackgroundProperty =
            DependencyProperty.Register(nameof(ButtonPressedBackground), typeof(Windows.UI.Xaml.Media.SolidColorBrush), typeof(TitleBar), new PropertyMetadata(null));

        public Windows.UI.Xaml.Media.SolidColorBrush ButtonPressedForeground
        {
            get { return (Windows.UI.Xaml.Media.SolidColorBrush)GetValue(ButtonPressedForegroundProperty); }
            set { SetValue(ButtonPressedForegroundProperty, value); }
        }
        public static readonly DependencyProperty ButtonPressedForegroundProperty =
            DependencyProperty.Register(nameof(ButtonPressedForeground), typeof(Windows.UI.Xaml.Media.SolidColorBrush), typeof(TitleBar), new PropertyMetadata(null));

        public Visibility BackButtonVisibility
        {
            get { return (Visibility)GetValue(BackButtonVisibilityProperty); }
            set { SetValue(BackButtonVisibilityProperty, value); }
        }
        public static readonly DependencyProperty BackButtonVisibilityProperty =
            DependencyProperty.Register(
                nameof(BackButtonVisibility), typeof(Visibility), typeof(TitleBar),
                new PropertyMetadata(false, new PropertyChangedCallback((sender, args) => {
                    var self = sender as TitleBar;
                    VisualStateManager.GoToState(
                        self, (Visibility)args.NewValue == Visibility.Visible ? self.BackButtonVisible.Name : self.BackButtonCollapsed.Name, true);
                })));

        private Windows.ApplicationModel.Core.CoreApplicationViewTitleBar m_coreTitleBar;
        private Windows.UI.ViewManagement.UISettings m_uiSettings;
        private Windows.UI.ViewManagement.AccessibilitySettings m_accessibilitySettings;
        private Utils.ThemeHelper.ThemeChangedCallbackToken m_rootFrameRequestedThemeCallbackToken;
    }
}
