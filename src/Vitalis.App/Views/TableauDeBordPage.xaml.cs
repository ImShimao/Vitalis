using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Vitalis.Core.Disques;
using Vitalis.Core.Materiel;
using Vitalis.Core.MisesAJour;
using Vitalis.Core.TableauDeBord;

namespace Vitalis.App.Views;

/// <summary>Page « Tableau de bord » : score global, cartes de synthèse, analyse lancée à la demande.</summary>
public sealed partial class TableauDeBordPage : Page
{
    private CancellationTokenSource? _annulation;

    public TableauDeBordPage()
    {
        InitializeComponent();
        Loaded += (_, _) => AfficherDerniereAnalyse();
        Unloaded += (_, _) => _annulation?.Cancel();
    }

    private async void BoutonAnalyser_Click(object sender, RoutedEventArgs e)
    {
        _annulation?.Cancel();
        _annulation = new CancellationTokenSource();

        BoutonAnalyser.IsEnabled = false;
        BoutonAnnuler.Visibility = Visibility.Visible;
        Progression.Visibility = Visibility.Visible;
        Progression.Value = 0;
        TexteStatut.Text = "Analyse en cours… (la recherche Windows Update prend environ 30 secondes)";

        try
        {
            var progres = new Progress<int>(v => Progression.Value = v);
            var service = new TableauDeBordService(
                new InventaireMateriel(new WmiSource()),
                new AnalyseDisques(new WmiSource(), JournalDisques),
                new AnalyseMisesAJour(new WuaCom(), new WingetCli()));

            var resultat = await service.AnalyserAsync(progres, _annulation.Token);
            AfficherResultat(resultat);
            EnregistrerDerniereAnalyse(resultat.Date);
        }
        catch (OperationCanceledException)
        {
            TexteStatut.Text = "Analyse annulée.";
        }
        catch (Exception ex)
        {
            TexteStatut.Text = $"Analyse impossible : {ex.Message}";
        }
        finally
        {
            BoutonAnalyser.IsEnabled = true;
            BoutonAnnuler.Visibility = Visibility.Collapsed;
            Progression.Visibility = Visibility.Collapsed;
        }
    }

    private void BoutonAnnuler_Click(object sender, RoutedEventArgs e)
    {
        // Les appels COM de Windows Update ne s'interrompent pas en cours d'appel : l'annulation attend la fin de l'appel.
        _annulation?.Cancel();
        TexteStatut.Text = "Annulation demandée…";
    }

    private void AfficherResultat(ResultatTableauDeBord r)
    {
        TexteScore.Text = r.Score is null
            ? "Score global : indisponible"
            : $"Score global : {r.Score} / 100";

        TexteStatut.Text = "Analyse terminée.";
        TexteDerniereAnalyse.Text = $"Dernière analyse : {r.Date.ToString("g", CultureInfo.CurrentCulture)}";
        Cartes.Children.Clear();
        foreach (var c in r.Cartes)
        {
            Cartes.Children.Add(Carte(c));
        }
    }

    private static Border Carte(CarteSynthese c)
    {
        var contenu = new StackPanel { Spacing = 6 };
        contenu.Children.Add(Texte(c.Titre, secondaire: false, gras: true));

        if (c.Etat == Disponibilite.Indisponible)
        {
            contenu.Children.Add(Texte(c.Raison ?? "Indisponible.", secondaire: true));
        }
        else
        {
            if (c.Valeur is not null)
            {
                contenu.Children.Add(Texte(c.Valeur, secondaire: false));
            }

            if (c.Detail is not null)
            {
                contenu.Children.Add(Texte(c.Detail, secondaire: true));
            }

            if (c.Etat == Disponibilite.Partielle && c.Raison is not null)
            {
                contenu.Children.Add(Texte(c.Raison, secondaire: true));
            }
        }

        return new Border
        {
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(8),
            Background = (Brush)Application.Current.Resources["VitalisFond"],
            BorderBrush = (Brush)Application.Current.Resources["VitalisTexteSecondaire"],
            BorderThickness = new Thickness(1),
            Child = contenu,
        };
    }

    private static TextBlock Texte(string texte, bool secondaire, bool gras = false) => new()
    {
        Text = texte,
        TextWrapping = TextWrapping.Wrap,
        FontWeight = gras ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal,
        Foreground = (Brush)Application.Current.Resources[secondaire ? "VitalisTexteSecondaire" : "VitalisTexte"],
    };

    private void AfficherDerniereAnalyse()
    {
        var date = LireDerniereAnalyse();
        TexteDerniereAnalyse.Text = date is null
            ? "Aucune analyse pour l'instant."
            : $"Dernière analyse : {date.Value.ToString("g", CultureInfo.CurrentCulture)}";
    }

    private static DateTime? LireDerniereAnalyse()
    {
        try
        {
            var fichier = Chemin();
            return File.Exists(fichier) && DateTime.TryParse(File.ReadAllText(fichier), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d)
                ? d
                : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static void EnregistrerDerniereAnalyse(DateTime date)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Chemin())!);
            File.WriteAllText(Chemin(), date.ToString("o", CultureInfo.InvariantCulture));
        }
        catch (IOException)
        {
            // La date de dernière analyse est un confort : son échec ne doit pas bloquer l'affichage.
        }
    }

    private static string Chemin() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vitalis", "derniere-analyse.txt");

    private static void JournalDisques(string ligne)
    {
        // Le journal détaillé des disques n'est écrit qu'en administrateur (voir la page Disques).
    }
}
