using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vitalis.Core.Materiel;
using Vitalis.Core.Pilotes;

namespace Vitalis.App.Views;

/// <summary>Page « Pilotes » : liste, recherche, règle des deux ans, liens officiels vérifiés. Lecture seule.</summary>
public sealed partial class PilotesPage : Page
{
    // Au-delà, la liste devient lente à construire : l'utilisateur affine avec la recherche.
    private const int MaxLignes = 150;

    private readonly StackPanel _liste = new() { Spacing = 12 };
    private RapportPilotes? _rapport;
    private DispatcherTimer? _delaiRecherche;

    public PilotesPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await ChargerAsync(forcer: false);
    }

    private async void BoutonActualiser_Click(object sender, RoutedEventArgs e) => await ChargerAsync(forcer: true);

    // La lecture WMI des pilotes prend plusieurs secondes : elle tourne hors du thread d'interface.
    private Task ChargerAsync(bool forcer) => Ui.ChargerAsync(
        EtatAnalyse.Pilotes, forcer, Corps, BoutonActualiser,
        "Lecture des pilotes installés… (5 à 10 secondes)",
        ct => Task.Run(() => new AnalysePilotes(new WmiSource()).Analyser(DateTime.Now), ct),
        r =>
        {
            _rapport = r;
            Filtres.Visibility = r.Pilotes.Elements.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            Afficher();
        });

    private void Recherche_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Attend une courte pause de frappe avant de reconstruire la liste.
        _delaiRecherche ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _delaiRecherche.Stop();
        _delaiRecherche.Tick -= SurDelai;
        _delaiRecherche.Tick += SurDelai;
        _delaiRecherche.Start();
    }

    private void SurDelai(object? sender, object e)
    {
        _delaiRecherche?.Stop();
        AfficherListe();
    }

    private void AnciensSeulement_Toggled(object sender, RoutedEventArgs e)
    {
        if (_rapport is null)
        {
            return;
        }

        AfficherListe();
    }

    private void Afficher()
    {
        if (_rapport is not { } rapport)
        {
            return;
        }

        if (rapport.Pilotes.Etat == Disponibilite.Indisponible)
        {
            Corps.Children.Add(Ui.Message(InfoBarSeverity.Warning, "Pilotes indisponibles", rapport.Pilotes.Raison));
            return;
        }

        var tous = rapport.Pilotes.Elements;
        var resume = new GrilleAdaptative { LargeurMinimale = 220 };
        resume.Children.Add(Ui.Carte(Ui.Statistique(tous.Count.ToString(CultureInfo.CurrentCulture), "Pilotes installés", "\uE772")));
        resume.Children.Add(Ui.Carte(Ui.Statistique(rapport.NombreAnciens.ToString(CultureInfo.CurrentCulture), "Anciens (plus de deux ans)", "\uE7BA")));
        resume.Children.Add(Ui.Carte(Ui.Statistique(LiensFabricants.DateVerification, "Vérification des liens fabricants", "\uE71B")));
        Corps.Children.Add(resume);
        Corps.Children.Add(Ui.Legende("Un pilote « ancien » n'est pas forcément à remplacer : beaucoup de pilotes Windows génériques ne changent jamais. Vérifiez d'abord sur le site du fabricant."));
        ((Filtres.Parent ?? Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(Filtres)) as Panel)?.Children.Remove(Filtres);
        Corps.Children.Add(Filtres);
        (_liste.Parent as Panel)?.Children.Remove(_liste);
        Corps.Children.Add(_liste);
        AfficherListe();
    }

    private void AfficherListe()
    {
        if (_rapport is not { } rapport)
        {
            return;
        }

        _liste.Children.Clear();
        var tous = rapport.Pilotes.Elements;
        var filtre = Recherche.Text.Trim();
        var liste = tous
            .Where(p => !AnciensSeulement.IsOn || p.Ancien)
            .Where(p => filtre.Length == 0
                || p.Appareil.Contains(filtre, StringComparison.CurrentCultureIgnoreCase)
                || (p.Fabricant?.Contains(filtre, StringComparison.CurrentCultureIgnoreCase) ?? false))
            .OrderByDescending(p => p.Ancien)
            .ThenBy(p => p.Appareil, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        if (liste.Count == 0)
        {
            _liste.Children.Add(Ui.Vide("\uE721", "Aucun pilote ne correspond", "Modifiez la recherche ou le filtre."));
            return;
        }

        _liste.Children.Add(Ui.ListeCarte(liste.Take(MaxLignes).Select(Ligne)));
        if (liste.Count > MaxLignes)
        {
            _liste.Children.Add(Ui.Legende($"{MaxLignes} pilotes affichés sur {liste.Count}. Utilisez la recherche pour trouver les autres."));
        }
    }

    private static UIElement Ligne(Pilote p)
    {
        var date = p.Date is null ? "date inconnue" : p.Date.Value.ToString("d MMM yyyy", CultureInfo.CurrentCulture);
        var detail = $"{p.Fabricant ?? "Fabricant inconnu"}  ·  version {p.Version ?? "n/d"}  ·  {date}";

        var droite = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        if (p.Lien is { EstVerifie: true } lien)
        {
            var b = new HyperlinkButton { Content = $"Site {lien.Nom}", NavigateUri = new Uri(lien.Url!) };
            ToolTipService.SetToolTip(b, $"Page officielle, vérifiée le {lien.VerifieLe}");
            droite.Children.Add(b);
        }

        droite.Children.Add(p.Ancien ? Ui.Badge(Niveau.Attention, "Ancien") : Ui.Badge(Niveau.Succes, "Récent"));
        return Ui.Ligne(p.Appareil, detail, droite);
    }
}
