using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Vitalis.Core.Performances;

namespace Vitalis.App.Views;

/// <summary>Page « Performances » : benchmarks CPU, RAM, disque ; historique et comparaison.</summary>
public sealed partial class PerformancesPage : Page
{
    private static readonly string Dossier = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vitalis");
    private static readonly string FichierHistorique = Path.Combine(Dossier, "performances.json");

    private CancellationTokenSource? _annulation;

    public PerformancesPage()
    {
        InitializeComponent();
        Loaded += (_, _) => AfficherDerniere();
        Unloaded += (_, _) => _annulation?.Cancel();
    }

    private async void BoutonLancer_Click(object sender, RoutedEventArgs e)
    {
        _annulation?.Cancel();
        _annulation = new CancellationTokenSource();
        var jeton = _annulation.Token;

        BoutonLancer.IsEnabled = false;
        Attente.IsActive = true;
        TexteStatut.Text = "Mesures en cours : ne pas utiliser intensivement la machine pendant ce temps.";

        try
        {
            var resultats = await Task.Run(() => new List<ResultatBenchmark>
            {
                Benchmarks.Cpu(jeton),
                Benchmarks.Ram(jeton),
                Benchmarks.Disque(Path.GetTempPath(), jeton),
                Benchmarks.GpuIndisponible(),
            }, jeton);

            var analyse = new AnalysePerformance(DateTime.Now, resultats);
            var precedente = HistoriquePerformances.Charger(FichierHistorique).LastOrDefault();
            HistoriquePerformances.Ajouter(FichierHistorique, analyse);

            Afficher(analyse, precedente);
        }
        catch (OperationCanceledException)
        {
            TexteStatut.Text = "Mesures annulées.";
        }
        catch (Exception ex)
        {
            TexteStatut.Text = $"Mesures impossibles : {ex.Message}";
        }
        finally
        {
            Attente.IsActive = false;
            BoutonLancer.IsEnabled = true;
        }
    }

    private void AfficherDerniere()
    {
        var derniere = HistoriquePerformances.Charger(FichierHistorique).LastOrDefault();
        if (derniere is not null)
        {
            var avant = HistoriquePerformances.Charger(FichierHistorique).SkipLast(1).LastOrDefault();
            Afficher(derniere, avant);
        }
    }

    private void Afficher(AnalysePerformance analyse, AnalysePerformance? precedente)
    {
        ListeResultats.Children.Clear();
        TexteStatut.Text = $"Analyse du {analyse.Date:g}" + (precedente is null ? " (première analyse : pas de comparaison)." : $", comparée à celle du {precedente.Date:g}.");

        foreach (var r in analyse.Resultats)
        {
            var variation = precedente is null ? null : HistoriquePerformances.Variation(precedente, analyse, r.Composant);
            var resultat = r.Mesure
                ? $"{r.Valeur} {r.Unite}" + (variation is null ? string.Empty : $" · {(variation >= 0 ? "+" : string.Empty)}{variation} % vs précédente")
                : "non mesuré";

            ListeResultats.Children.Add(new Border
            {
                Padding = new Thickness(12),
                CornerRadius = new CornerRadius(6),
                BorderBrush = (Brush)Application.Current.Resources["VitalisTexteSecondaire"],
                BorderThickness = new Thickness(1),
                Child = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        Texte(r.Composant + " · " + resultat, secondaire: false, gras: true),
                        Texte("Méthode : " + r.Methode, secondaire: true),
                        Texte("Limites : " + r.Limites, secondaire: true),
                    },
                },
            });
        }
    }

    private static TextBlock Texte(string texte, bool secondaire, bool gras = false) => new()
    {
        Text = texte,
        TextWrapping = TextWrapping.Wrap,
        FontWeight = gras ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal,
        Foreground = (Brush)Application.Current.Resources[secondaire ? "VitalisTexteSecondaire" : "VitalisTexte"],
    };
}
