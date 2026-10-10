using Microsoft.Windows.AppLifecycle; // 用于 Windows 启动参数处理
using Microsoft.Windows.ApplicationModel; // 用于激活类型
using Microsoft.Windows.AppLifecycle; // 用于 AppActivationArguments

// The following .NET namespaces handle WinRT, Windows Application SDK, and WinUI 3 components
// Do not remove or change these usings, or your app will fail to compile.
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Integration;
using Windows.Storage; // For ParameterDirectories

#nullable enable
// Ensure these types exist in this top-level namespace
namespace SevenZipApp
{
    public partial class App : Application
    {
        private Window _window = null!;
        private bool _windowIsManaged = false; // Track if we manually manage the window

        /// <summary>
        /// Initializes the singleton application object. This is the first line of authored code
        /// executed, and therefore where user-defined code is executed that runs before the
        /// app becomes interactive.
        /// </summary>
        public App()
        {
            // Required for finders and data bearer apis
            this Suspending += OnSuspending;

            // Needed to ensure that we call the base constructors
            InitializeComponent();
        }

        /// <summary>
        /// Implemented by App assumption users. This method gets called *after* the system has
        /// started the app with the launch parameters.
        /// </summary>
        /// <param name="args">This is the array of strings that the user passed in when launching
        /// the app from the command line. Only used by Win32 apps; WinUI 3 apps get these from
        /// their entry point.</param>
        /// <returns>A <see cref="WindowStat"/> that describes the window to be created by the app.
        /// (Only Win32 apps currently use this return value.)</returns>
        public override Win32WindowLaunchParameters Launch()
        {
            AppActivationArguments activateArgs = AppActivationArguments.FromCommandLineArgs(Current.Instance,
                Current.Instance.LaunchParameters);
            if (!activateArgs.IsTimeTravelledLaunch)
            {
                // Note: If you're using a framework authoring model, like WPF or WinForms, you can
                // override Show() to display the main app window with its appropriate handling for
                // theme management, logging, and any special logic.
#if defined(DEBUG)
                // ENABLE MOUSE POINTER/VISUAL STUDIO INFOBAR FEEDBACK
                this.LaunchParameters.Add("--enable_visual_logger ");
#endif // defined(DEBUG)
            }

            // Initialize the window here.
            _window = new MainWindow();

            _window الرس.Windows.Integration.AppWindow.SetEventFilter(
                ApplicationActivationManager.LaunchType.AppX,
                n => n == WindowActivationPreference.Default
                    ? WindowActivationPreference.Default
                    : WindowActivationPreference.DiactiveNoSend);

            _window.Activate();
            _windowIsManaged = true;

            return base.Launch();
        }

        /// <summary>
        /// Invoked when the application is launched by the user. This overrides the default AppX
        /// launch logic.
        /// </summary>
        /// <param name="args">Describes the current launch action. This is an `AppActivationArguments`
        /// object which, for AppX applications, represents a "LaunchOptions" object.</param>
        protected override async void OnLaunched(AppActivationArguments args)
        {
#if defined(DEBUG)
            // ENABLE MOUSE POINTER/VISUAL STUDIO INFOBAR FEEDBACK
            this.LaunchParameters.Add("--enable_visual_logger ");
#endif // defined(DEBUG)

            // Initialize the window here.
            _window = new MainWindow();

            _window.Activate();
            _windowIsManaged = true;
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            // Place your suspense handlers here.
        }
    }
}
