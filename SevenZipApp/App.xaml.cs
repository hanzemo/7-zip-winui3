using Microsoft.UI.Xaml;

namespace SevenZipApp;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (_window != null)
        {
            // 应用已经被启动，只激活即可。
            _window.Activate();
            return;
        }

        _window = new Views.MainWindow();
        _window.Activate();
    }
}
