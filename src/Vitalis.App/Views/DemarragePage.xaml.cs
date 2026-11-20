using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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
        Loaded += (_, _) => Afficher();
    }

    private DemarrageService Service() => new(new RegistreDemarrageWindows(), _fichierSauvegarde);

    private void Afficher()
    {
        ListeEntrees.Children.Clear();
        ListeDesactivees.Children.Clear();

        try
        {
            var service = Service();
            var actifs = service.Lister();
            var desactives = service.Desactivees();
            TexteResume.Text = $"{actifs.Count} programme(s) au démarrage · {desactives.Count} désactivé(s) par Vitalis.";

            foreach (var e in actifs)
            {
                ListeEntrees.Children.Add(Ligne(e, "Désactiver…", async () => await ConfirmerDesactivationAsync(e)));
            }

            foreach (var e in desactives)
            {
                ListeDesactivees.Children.Add(Ligne(e, "Réactiver", () =>
                {
                    Service().Restaurer(e);
                    Afficher();
                }));
            }
        }
        catch (Exception ex)
        {
            TexteResume.Text = $"Lecture impossible : {ex.Message}";
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

        if (await dialogue.ShowAsync() == ContentDialogResult.Primary)
        {
            Service().Desactiver(e, confirme: true);
            Afficher();
        }
    }

    private static Border Ligne(EntreeDemarrage e, string libelleBouton, Action action)
    {
        var bouton = new Button { Content = libelleBouton };
        bouton.Click += (_, _) => action();
        return Ligne(e, bouton);
    }

    private static Border Ligne(EntreeDemarrage e, string libelleBouton, Func<Task> action)
    {
        var bouton = new Button { Content = libelleBouton };
        bouton.Click += async (_, _) => await action();
        return Ligne(e, bouton);
    }

    private static Border Ligne(EntreeDemarrage e, Button bouton)
    {
        var grille = new Grid();
        grille.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grille.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var texte = new StackPanel { Spacing = 2 };
        texte.Children.Add(new TextBlock
        {
            Text = e.Nom,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.Resources["VitalisTexte"],
        });
        texte.Children.Add(new TextBlock
        {
            Text = $"{e.Emplacement} · {e.Commande}",
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.Resources["VitalisTexteSecondaire"],
        });

        Grid.SetColumn(bouton, 1);
        grille.Children.Add(texte);
        grille.Children.Add(bouton);

        return new Border
        {
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(6),
            BorderBrush = (Brush)Application.Current.Resources["VitalisTexteSecondaire"],
            BorderThickness = new Thickness(1),
            Child = grille,
        };
    }
}
