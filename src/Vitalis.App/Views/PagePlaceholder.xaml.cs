using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Vitalis.Core.Navigation;

namespace Vitalis.App.Views;

/// <summary>Page pas encore disponible : titre, et ce qui manque pour la construire.</summary>
public sealed partial class PagePlaceholder : Page
{
    public PagePlaceholder()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not PageDefinition page)
        {
            return;
        }

        TitrePage.Text = page.Titre;
        TexteResume.Text = page.Cle == "sante-live"
            ? "Températures, fréquences et ventilateurs en temps réel."
            : $"Prévue à l'étape {page.EtapeDisponible}.";
        Corps.Children.Clear();
        Corps.Children.Add(Ui.Vide("\uE95E", "Bientôt disponible", page.Cle == "sante-live"
            ? "La lecture des capteurs en temps réel demande un pilote matériel (LibreHardwareMonitor). Ce choix touche à la sécurité du PC : il attend une décision avant d'être intégré."
            : $"Cette page sera remplie à l'étape {page.EtapeDisponible}."));
    }
}
