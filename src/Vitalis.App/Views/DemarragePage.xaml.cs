using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vitalis.Core.Demarrage;

namespace Vitalis.App.Views;

/// <summary>Page « Démarrage » : programmes lancés avec Windows. Désactivation réversible après confirmation.</summary>
public sealed partial class DemarragePage : Page
{
    private readonly string _fichierSauvegarde = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vitalis", "demarrage-desactive.json");

    public DemarragePage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await AfficherAsync();
    }

    private DemarrageService Service() => Demo.Actif
        ? new(new Demo.Registre(), Path.Combine(Demo.Dossier, "demarrage-desactive.json"))
        : new(new RegistreDemarrageWindows(), _fichierSauvegarde);

    private async void BoutonActualiser_Click(object sender, RoutedEventArgs e) => await AfficherAsync();

    private async Task AfficherAsync(string? confirmation = null)
    {
        BoutonActualiser.IsEnabled = false;
        if (Corps.Children.Count == 0)
        {
            Corps.Children.Add(Ui.Chargement("Lecture des programmes de démarrage…"));
        }

        try
        {
            var (actifs, desactives) = await Task.Run(() =>
            {
                var s = Service();
                return (s.Lister(), s.Desactivees());
            });

            Corps.Children.Clear();
            if (confirmation is not null)
            {
                Corps.Children.Add(Ui.Message(InfoBarSeverity.Success, confirmation));
            }

            var resume = new GrilleAdaptative { LargeurMinimale = 220 };
            resume.Children.Add(Ui.Carte(Ui.Statistique(actifs.Count.ToString(), "Programmes au démarrage", "\uE7E8")));
            resume.Children.Add(Ui.Carte(Ui.Statistique(desactives.Count.ToString(), "Désactivés par Vitalis", "\uE8D8")));
            Corps.Children.Add(resume);

            Corps.Children.Add(Ui.TitreSection("Programmes au démarrage"));
            Corps.Children.Add(actifs.Count == 0
                ? Ui.Vide("\uE7E8", "Aucun programme au démarrage", "Rien n'est lancé automatiquement depuis les clés Run lues par Vitalis.")
                : Ui.ListeCarte(actifs.OrderBy(e => e.Nom, StringComparer.CurrentCultureIgnoreCase)
                    .Select(e => e.Emplacement.StartsWith("HKCU", StringComparison.Ordinal)
                        ? Ligne(e, "Désactiver…", "\uE71A", () => ConfirmerDesactivationAsync(e))
                        // HKLM : modification réservée à l'administrateur, non proposée (le registre la refuserait).
                        : Ui.Ligne(e.Nom, $"{e.Emplacement}  ·  {e.Commande}", Ui.Badge(Niveau.Neutre, "Tous les utilisateurs")))));

            if (desactives.Count > 0)
            {
                Corps.Children.Add(Ui.TitreSection("Désactivés par Vitalis"));
                Corps.Children.Add(Ui.ListeCarte(desactives.Select(e => Ligne(e, "Réactiver", "\uE768", async () =>
                {
                    await Task.Run(() => Service().Restaurer(e));
                    await AfficherAsync($"« {e.Nom} » démarrera de nouveau avec Windows.");
                }))));
            }

            Corps.Children.Add(Ui.Legende("Seuls les programmes de votre session (clé Run de l'utilisateur) peuvent être désactivés ici. Les autres sont affichés pour information."));
        }
        catch (Exception ex)
        {
            Corps.Children.Clear();
            Corps.Children.Add(Ui.Message(InfoBarSeverity.Error, "Lecture impossible", ex.Message));
        }
        finally
        {
            BoutonActualiser.IsEnabled = true;
        }
    }

    private async Task ConfirmerDesactivationAsync(EntreeDemarrage e)
    {
        var dialogue = new ContentDialog
        {
            Title = "Désactiver ce programme au démarrage ?",
            Content = $"« {e.Nom} » ne démarrera plus avec Windows. Sa commande est sauvegardée : vous pourrez la réactiver depuis cette page. Le programme n'est pas supprimé.",
            PrimaryButtonText = "Désactiver",
            CloseButtonText = "Annuler",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
        };

        if (await dialogue.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            await Task.Run(() => Service().Desactiver(e, confirme: true));
            await AfficherAsync($"« {e.Nom} » est désactivé. « Réactiver » le rétablit à l'identique.");
        }
        catch (Exception ex)
        {
            Corps.Children.Insert(0, Ui.Message(InfoBarSeverity.Error, "Désactivation impossible", ex.Message));
        }
    }

    private static UIElement Ligne(EntreeDemarrage e, string libelle, string glyphe, Func<Task> action)
    {
        var bouton = Ui.Bouton(libelle, glyphe);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(bouton, $"{libelle} {e.Nom}");
        bouton.Click += async (_, _) =>
        {
            bouton.IsEnabled = false;
            await action();
            bouton.IsEnabled = true;
        };

        var ligne = Ui.Ligne(e.Nom, $"{e.Emplacement}  ·  {e.Commande}", bouton);
        ToolTipService.SetToolTip(ligne, e.Commande);
        return ligne;
    }
}
