using Microsoft.UI.Xaml;

namespace Vitalis.App;

public partial class App : Application
{
    private Window? _fenetre;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _fenetre = new MainWindow();
        _fenetre.Activate();
    }
}
