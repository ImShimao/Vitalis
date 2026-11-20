using Microsoft.UI.Xaml;
using Vitalis.Core.Journal;
using Vitalis.Core.Parametres;

namespace Vitalis.App;

public partial class App : Application
{
    private Window? _fenetre;

    public static string Theme { get; private set; } = "Systeme";

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) =>
        {
            // Journalisation sans message complet (il peut contenir un chemin de profil).
            JournalVitalis.Erreur("Erreur non gérée", e.Exception);
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var dossier = Views.ParametresPage.DossierLocal;
        var prefs = StockagePreferences.Charger(Path.Combine(dossier, "preferences.json"));
        JournalVitalis.Initialiser(dossier, prefs.ModeDiagnostic);
        JournalVitalis.Information($"Démarrage de Vitalis {Vitalis.Core.AppInfo.Version}.");
        Theme = prefs.Theme;

        _fenetre = new MainWindow();
        _fenetre.Activate();
    }

    /// <summary>Applique le thème choisi (« Systeme », « Clair » ou « Sombre ») à la fenêtre principale.</summary>
    public static void AppliquerTheme(string theme)
    {
        Theme = theme;
        if (Current is App app && app._fenetre is MainWindow fenetre)
        {
            fenetre.AppliquerTheme(theme);
        }
    }
}
