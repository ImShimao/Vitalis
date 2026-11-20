using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vitalis.Core.Performances;

namespace Vitalis.App.Views;

/// <summary>Page « Performances » : benchmarks CPU, RAM, disque, carte graphique (WinSAT) ; historique et comparaison.</summary>
public sealed partial class PerformancesPage : Page
{
    private static readonly string Dossier = Demo.Actif ? Demo.Dossier : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vitalis");
    private static readonly string FichierHistorique = Path.Combine(Dossier, "performances.json");

    private static string Glyphe(string composant) => composant switch
    {
        _ when composant.StartsWith("Processeur", StringComparison.Ordinal) => "\uE950",
        _ when composant.StartsWith("Mémoire", StringComparison.Ordinal) => "\uE964",
        _ when composant.StartsWith("Disque", StringComparison.Ordinal) => "\uEDA2",
        _ when composant.StartsWith("Carte graphique", StringComparison.Ordinal) => "\uE7F4",
        _ => "\uEC4A",
    };

    private CancellationTokenSource? _annulation;

    public PerformancesPage()
    {
        InitializeComponent();
        Loaded += (_, _) => AfficherDerniere();
        Unloaded += (_, _) => _annulation?.Cancel();
    }

    private void BoutonAnnuler_Click(object sender, RoutedEventArgs e) => _annulation?.Cancel();

    private async void BoutonLancer_Click(object sender, RoutedEventArgs e)
    {
        _annulation?.Cancel();
        _annulation = new CancellationTokenSource();
        var jeton = _annulation.Token;

        BoutonLancer.IsEnabled = false;
        BoutonAnnuler.Visibility = Visibility.Visible;

        var etapes = new (string Nom, Func<ResultatBenchmark> Mesure)[]
        {
            ("Processeur", () => Benchmarks.Cpu(jeton)),
            ("Mémoire", () => Benchmarks.Ram(jeton)),
            ("Disque", () => Benchmarks.Disque(Path.GetTempPath(), jeton)),
        };

        var barre = new ProgressBar { Minimum = 0, Maximum = etapes.Length + 1, Value = 0 };
        var texte = Ui.Texte(string.Empty, secondaire: true);
        Corps.Children.Clear();
        var attente = new StackPanel { Spacing = 10 };
        attente.Children.Add(Ui.EnTeteCarte("\uEC4A", "Mesures en cours"));
        attente.Children.Add(barre);
        attente.Children.Add(texte);
        attente.Children.Add(Ui.Legende("Évitez d'utiliser le PC intensivement pendant ce temps : le résultat serait faussé."));
        Corps.Children.Add(Ui.Carte(attente));

        try
        {
            var resultats = new List<ResultatBenchmark>();
            for (var i = 0; i < etapes.Length; i++)
            {
                texte.Text = $"{etapes[i].Nom} ({i + 1} sur {etapes.Length})…";
                var mesure = etapes[i].Mesure;
                resultats.Add(await Task.Run(mesure, jeton));
                barre.Value = i + 1;
            }

            texte.Text = $"Carte graphique : évaluation officielle de Windows (WinSAT), 1 à 3 minutes ({etapes.Length + 1} sur {etapes.Length + 1})…";
            var raison = await Task.Run(() => Benchmarks.LancerEvaluationWindows(jeton), jeton);
            resultats.AddRange(Benchmarks.WinSat(new Vitalis.Core.Materiel.WmiSource(), raison));
            barre.Value = etapes.Length + 1;
            var analyse = new AnalysePerformance(DateTime.Now, resultats);
            var precedente = HistoriquePerformances.Charger(FichierHistorique).LastOrDefault();
            HistoriquePerformances.Ajouter(FichierHistorique, analyse);
            Afficher(analyse, precedente);
        }
        catch (OperationCanceledException)
        {
            Corps.Children.Clear();
            Corps.Children.Add(Ui.Message(InfoBarSeverity.Informational, "Mesures annulées", "Aucun résultat n'a été enregistré."));
        }
        catch (Exception ex)
        {
            Corps.Children.Clear();
            Corps.Children.Add(Ui.Message(InfoBarSeverity.Error, "Mesures impossibles", ex.Message));
        }
        finally
        {
            BoutonLancer.IsEnabled = true;
            BoutonAnnuler.Visibility = Visibility.Collapsed;
        }
    }

    private void AfficherDerniere()
    {
        var historique = HistoriquePerformances.Charger(FichierHistorique);
        if (historique.LastOrDefault() is { } derniere)
        {
            Afficher(derniere, historique.SkipLast(1).LastOrDefault());
            return;
        }

        Corps.Children.Clear();
        var lancer = Ui.BoutonAccent("Lancer les mesures", "\uE768");
        lancer.Click += BoutonLancer_Click;
        Corps.Children.Add(Ui.Vide("\uEC4A", "Aucune mesure pour l'instant",
            "Processeur, mémoire, disque, puis carte graphique par l'évaluation officielle de Windows (WinSAT) : 2 à 4 minutes en tout. Les résultats sont gardés pour comparer avec les prochaines.", lancer));
    }

    private void Afficher(AnalysePerformance analyse, AnalysePerformance? precedente)
    {
        Corps.Children.Clear();
        Corps.Children.Add(Ui.Legende($"Mesuré {Ui.Quand(analyse.Date)}"
            + (precedente is null ? "  ·  première mesure : pas encore de comparaison." : $"  ·  comparé à la mesure du {precedente.Date.ToString("g", CultureInfo.CurrentCulture)}.")));

        var grille = new GrilleAdaptative { LargeurMinimale = 240 };
        foreach (var r in analyse.Resultats)
        {
            grille.Children.Add(Carte(r, precedente is null ? null : HistoriquePerformances.Variation(precedente, analyse, r.Composant)));
        }

        Corps.Children.Add(grille);
    }

    private static UIElement Carte(ResultatBenchmark r, double? variation)
    {
        var p = new StackPanel { Spacing = 8 };
        p.Children.Add(Ui.EnTeteCarte(Glyphe(r.Composant), r.Composant));

        if (r.Mesure)
        {
            var valeur = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            // L'unité est vide pour le processeur : elle est déjà dans la valeur (« 120 ms »).
            valeur.Children.Add(new TextBlock { Text = r.Valeur, Style = Ui.Style("SubtitleTextBlockStyle"), TextWrapping = TextWrapping.Wrap, Foreground = Ui.Pinceau("VitalisTexte") });
            var unite = Ui.Texte(r.Unite, secondaire: true);
            unite.VerticalAlignment = VerticalAlignment.Bottom;
            unite.Margin = new Thickness(0, 0, 0, 3);
            valeur.Children.Add(unite);
            p.Children.Add(valeur);

            if (variation is { } v)
            {
                // Variation de ±3 % : dans le bruit de mesure, affichée comme stable.
                p.Children.Add(Math.Abs(v) < 3
                    ? Ui.Badge(Niveau.Neutre, $"Stable ({Signe(v)} %)")
                    : v > 0 ? Ui.Badge(Niveau.Succes, $"{Signe(v)} % vs précédente") : Ui.Badge(Niveau.Attention, $"{Signe(v)} % vs précédente"));
            }
        }
        else
        {
            p.Children.Add(Ui.Badge(Niveau.Neutre, "Non mesuré"));
        }

        var details = new StackPanel { Spacing = 6 };
        details.Children.Add(Ui.Legende("Méthode : " + r.Methode));
        details.Children.Add(Ui.Legende("Limites : " + r.Limites));
        p.Children.Add(new Expander
        {
            Header = "Méthode et limites",
            Content = details,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
        });
        return Ui.Carte(p);
    }

    private static string Signe(double v) => (v >= 0 ? "+" : string.Empty) + v.ToString("0.#", CultureInfo.CurrentCulture);
}
