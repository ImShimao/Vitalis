using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vitalis.Core.Journal;
using Vitalis.Core.Materiel;
using Vitalis.Core.MisesAJour;
using Vitalis.Core.Pilotes;

namespace Vitalis.App.Views;

/// <summary>
/// Page « Pilotes » : chaque pilote comparé au catalogue Windows Update (installé → disponible), carte graphique et
/// carte mère avec leurs pages officielles, puis la liste complète avec recherche. Installation après confirmation seulement.
/// </summary>
public sealed partial class PilotesPage : Page
{
    // Au-delà, la liste devient lente à construire : l'utilisateur affine avec la recherche.
    private const int MaxLignes = 150;

    private readonly StackPanel _liste = new() { Spacing = 12 };
    private readonly List<Button> _boutonsInstallation = [];
    private ResultatPilotes? _resultat;
    private DispatcherTimer? _delaiRecherche;
    private readonly List<Action> _lignes = [];
    private readonly ContentControl _bilan = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch };

    public PilotesPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            Installations.Change += Rafraichir;
            await ChargerAsync(forcer: false);
            Rafraichir();
        };
        Unloaded += (_, _) => Installations.Change -= Rafraichir;
    }

    /// <summary>État des installations de la session (elles continuent quand on change de page).</summary>
    private void Rafraichir()
    {
        BoutonActualiser.IsEnabled = !Installations.EnCours && !EtatAnalyse.Pilotes.EnCours;
        _boutonsInstallation.ForEach(b => b.IsEnabled = !Installations.EnCours);
        _lignes.ForEach(l => l());
        Ui.AfficherBilan(_bilan, Installations.Bilan(Installations.GroupePilotes));
    }

    private async void BoutonActualiser_Click(object sender, RoutedEventArgs e) => await ChargerAsync(forcer: true);

    private Task ChargerAsync(bool forcer) => Ui.ChargerAsync(
        EtatAnalyse.Pilotes, forcer, Corps, BoutonActualiser,
        "Lecture des pilotes installés et comparaison avec le catalogue Windows Update… (10 à 40 secondes)",
        AnalyserAsync,
        r =>
        {
            _resultat = r;
            Filtres.Visibility = r.Installes.Pilotes.Elements.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            Afficher();
            Rafraichir();
        });

    private static async Task<ResultatPilotes> AnalyserAsync(CancellationToken ct)
    {
        var installes = Task.Run(() => new AnalysePilotes(new WmiSource()).Analyser(DateTime.Now), ct);
        var catalogue = Task.Run(() =>
        {
            try
            {
                return (Liste: CataloguePilotes.Rechercher(), Raison: (string?)null);
            }
            catch (Exception e)
            {
                return (Liste: (IReadOnlyList<PiloteDisponible>)[], Raison: $"Catalogue Windows Update indisponible ({e.GetType().Name} : {e.Message.Trim()})");
            }
        }, ct);
        var materiel = EtatAnalyse.Materiel.Valeur is { } m
            ? Task.FromResult<RapportMateriel?>(m)
            : Task.Run<RapportMateriel?>(async () =>
            {
                try
                {
                    return await new InventaireMateriel(new WmiSource()).AnalyserAsync(ct);
                }
                catch (Exception)
                {
                    return null;
                }
            }, ct);

        await Task.WhenAll(installes, catalogue, materiel);
        var (liste, raison) = catalogue.Result;
        var comparaisons = CataloguePilotes.Comparer(liste.Where(d => !d.Masque).ToList(), installes.Result.Pilotes.Elements);
        var section = raison is null
            ? new Section<ComparaisonPilote>(Disponibilite.Disponible, comparaisons)
            : new Section<ComparaisonPilote>(Disponibilite.Indisponible, [], raison);
        return new ResultatPilotes(installes.Result, section, materiel.Result);
    }

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
        if (_resultat is not null)
        {
            AfficherListe();
        }
    }

    private void Afficher()
    {
        if (_resultat is not { } r)
        {
            return;
        }

        _boutonsInstallation.Clear();
        _lignes.Clear();
        var tous = r.Installes.Pilotes.Elements;
        var disponibles = r.Disponibles.Elements;

        var resume = new GrilleAdaptative { LargeurMinimale = 200 };
        resume.Children.Add(Ui.Carte(Ui.Statistique(tous.Count.ToString(CultureInfo.CurrentCulture), "Pilotes installés", "")));
        resume.Children.Add(Ui.Carte(Ui.Statistique(
            r.Disponibles.Etat == Disponibilite.Indisponible ? "?" : disponibles.Count.ToString(CultureInfo.CurrentCulture),
            "Mises à jour de pilotes disponibles", "")));
        resume.Children.Add(Ui.Carte(Ui.Statistique(r.Installes.NombreAnciens.ToString(CultureInfo.CurrentCulture), "Pilotes de fabricant de plus de deux ans", "")));
        resume.Children.Add(Ui.Carte(Ui.Statistique(tous.Count(p => p.Generique).ToString(CultureInfo.CurrentCulture), "Pilotes génériques Windows", "")));
        Corps.Children.Add(resume);

        AfficherDisponibles(r.Disponibles);
        AfficherMateriel(r.Materiel);

        Corps.Children.Add(Ui.TitreSection("Tous les pilotes"));
        if (r.Installes.Pilotes.Etat == Disponibilite.Indisponible)
        {
            Corps.Children.Add(Ui.Message(InfoBarSeverity.Warning, "Pilotes indisponibles", r.Installes.Pilotes.Raison));
            return;
        }

        Corps.Children.Add(Ui.Legende("« Ancien » : pilote de fabricant daté de plus de deux ans. Ce n'est pas une panne : vérifiez sur le site du fabricant. Les pilotes génériques Windows sont mis à jour par Windows Update."));
        ((Filtres.Parent ?? Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(Filtres)) as Panel)?.Children.Remove(Filtres);
        Corps.Children.Add(Filtres);
        (_liste.Parent as Panel)?.Children.Remove(_liste);
        Corps.Children.Add(_liste);
        AfficherListe();
    }

    /// <summary>Pilotes plus récents du catalogue Windows Update, avec la version installée en face.</summary>
    private void AfficherDisponibles(Section<ComparaisonPilote> section)
    {
        Corps.Children.Add(Ui.TitreSection("Mises à jour de pilotes"));
        (_bilan.Parent as Panel)?.Children.Remove(_bilan);
        Corps.Children.Add(_bilan);
        if (section.Etat == Disponibilite.Indisponible)
        {
            Corps.Children.Add(Ui.Message(InfoBarSeverity.Warning, "Comparaison impossible", section.Raison));
            return;
        }

        if (section.Elements.Count == 0)
        {
            var facultatives = Ui.Bouton("Mises à jour facultatives", "");
            facultatives.Click += async (_, _) => await Windows.System.Launcher.LaunchUriAsync(new Uri(LiensFabricants.MisesAJourFacultatives));
            Corps.Children.Add(Ui.Vide("", "Pilotes à jour", "Le catalogue officiel Windows Update ne propose aucun pilote plus récent pour ce PC. Pour la carte graphique, vérifiez aussi le site du fabricant ci-dessous.", facultatives));
            return;
        }

        var lignes = section.Elements.Select(c => (Comparaison: c, Etat: new ContentControl(), Bouton: Ui.Bouton("Installer", ""))).ToList();
        var tout = Ui.BoutonAccent($"Tout installer ({lignes.Count})", "");
        _boutonsInstallation.Add(tout);
        tout.Click += async (_, _) =>
        {
            var restants = lignes.Where(l => l.Bouton.Visibility == Visibility.Visible).ToList();
            if (await ConfirmerAsync("Installer les pilotes ?",
                $"{Ui.Pluriel(restants.Count, "pilote")} du catalogue officiel Windows Update vont être téléchargés et installés. Un redémarrage peut être demandé.", "Installer"))
            {
                await InstallerAsync(restants);
            }
        };

        foreach (var l in lignes)
        {
            _boutonsInstallation.Add(l.Bouton);
            var courante = l;
            _lignes.Add(() => Ui.AppliquerSuivi(courante.Etat, courante.Bouton, Installations.Suivi(courante.Comparaison.Disponible.UpdateId),
                () => Ui.Badge(Niveau.Attention, "Plus récent disponible"), "Installé"));
            l.Bouton.Click += async (_, _) =>
            {
                if (await ConfirmerAsync("Installer ce pilote ?",
                    $"{Nom(courante.Comparaison)} : le pilote proposé par Windows Update sera téléchargé et installé. Un redémarrage peut être demandé.", "Installer"))
                {
                    await InstallerAsync([courante]);
                }
            };
        }

        Corps.Children.Add(tout);
        Corps.Children.Add(Ui.ListeCarte(lignes.Select(l =>
        {
            var c = l.Comparaison;
            var installe = c.Installe is { } p
                ? $"Installé : {p.Version ?? "version inconnue"} du {Date(p.Date)}"
                : "Installé : non identifié";
            var propose = $"Proposé : pilote du {Date(c.Disponible.Date)}" + (c.Disponible.Fournisseur is null ? string.Empty : $" par {c.Disponible.Fournisseur}");
            var droite = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            l.Etat.VerticalAlignment = VerticalAlignment.Center;
            droite.Children.Add(l.Etat);
            droite.Children.Add(l.Bouton);
            return (UIElement)Ui.Ligne(Nom(c), $"{installe}  →  {propose}", droite, Ui.Icone("", 18));
        })));
        Corps.Children.Add(Ui.Legende("Source : catalogue Windows Update, où Microsoft publie les pilotes signés et validés par les fabricants pour votre matériel exact."));
    }

    private static string Nom(ComparaisonPilote c) => c.Installe?.Appareil ?? c.Disponible.Modele ?? c.Disponible.Titre;

    /// <summary>Carte graphique, carte mère et BIOS : les pilotes que Windows Update ne couvre pas toujours, avec les pages officielles.</summary>
    private void AfficherMateriel(RapportMateriel? m)
    {
        if (m is null)
        {
            return;
        }

        Corps.Children.Add(Ui.TitreSection("Carte graphique, carte mère et BIOS"));
        var grille = new GrilleAdaptative { LargeurMinimale = 380 };
        foreach (var g in m.CartesGraphiques.Elements.Where(g => g.Nom is not null))
        {
            var ancien = AnalysePilotes.EstAncien(g.DatePilote, DateTime.Now);
            grille.Children.Add(FicheMateriel("", g.Nom!, g.DatePilote is null ? null : (ancien ? Ui.Badge(Niveau.Attention, "Plus de deux ans") : Ui.Badge(Niveau.Succes, "Récent")),
                [("Version du pilote", g.VersionPilote), ("Date du pilote", g.DatePilote is null ? null : Date(g.DatePilote))],
                LiensFabricants.Trouver(g.Nom),
                "Les pilotes graphiques évoluent vite (jeux, corrections) : le site du fabricant a souvent une version plus récente que Windows Update."));
        }

        var cm = m.CarteMere.Elements.FirstOrDefault();
        var bios = m.Bios.Elements.FirstOrDefault();
        if (cm is not null || bios is not null)
        {
            grille.Children.Add(FicheMateriel("", cm is null ? "Carte mère" : $"{cm.Fabricant} {cm.Modele}".Trim(),
                bios?.DateSortie is { } d && AnalysePilotes.EstAncien(d, DateTime.Now) ? Ui.Badge(Niveau.Attention, "BIOS de plus de deux ans") : null,
                [("BIOS", bios is null ? null : $"{bios.Fabricant} {bios.Version}".Trim()), ("Date du BIOS", bios?.DateSortie is null ? null : Date(bios.DateSortie))],
                LiensFabricants.Trouver(cm?.Fabricant),
                "Chipset, réseau, audio et BIOS : sur la page de votre modèle exact. Ne mettez à jour le BIOS que si une correction vous concerne."));
        }

        Corps.Children.Add(grille);
    }

    private static UIElement FicheMateriel(string glyphe, string titre, UIElement? badge, (string, string?)[] proprietes, LienFabricant? lien, string conseil)
    {
        var p = new StackPanel { Spacing = 12 };
        p.Children.Add(Ui.EnTeteCarte(glyphe, titre, badge));
        p.Children.Add(Ui.Proprietes(proprietes));
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        if (lien is { EstVerifie: true })
        {
            actions.Children.Add(LienBouton($"Pilotes {lien.Nom}", lien.Url!, lien.VerifieLe, accent: true));
            if (lien.UrlOutil is not null && lien.UrlOutil != lien.Url)
            {
                actions.Children.Add(LienBouton(lien.Outil!, lien.UrlOutil, lien.VerifieLe, accent: false));
            }
        }
        else
        {
            actions.Children.Add(LienBouton("Aide Microsoft", LiensFabricants.AideMicrosoft, LiensFabricants.DateVerification, accent: false));
        }

        p.Children.Add(actions);
        p.Children.Add(Ui.Legende(conseil));
        return Ui.Carte(p);
    }

    private static Button LienBouton(string texte, string url, string? verifieLe, bool accent)
    {
        var b = accent ? Ui.BoutonAccent(texte, "") : Ui.Bouton(texte, "");
        ToolTipService.SetToolTip(b, $"Page officielle, vérifiée le {verifieLe}");
        b.Click += async (_, _) => await Windows.System.Launcher.LaunchUriAsync(new Uri(url));
        return b;
    }

    private void AfficherListe()
    {
        if (_resultat is not { } r)
        {
            return;
        }

        _liste.Children.Clear();
        var filtre = Recherche.Text.Trim();
        var liste = r.Installes.Pilotes.Elements
            .Where(p => !AnciensSeulement.IsOn || p.Ancien)
            .Where(p => filtre.Length == 0
                || p.Appareil.Contains(filtre, StringComparison.CurrentCultureIgnoreCase)
                || (p.Fabricant?.Contains(filtre, StringComparison.CurrentCultureIgnoreCase) ?? false))
            .OrderByDescending(p => p.Ancien)
            .ThenBy(p => p.Generique)
            .ThenBy(p => p.Appareil, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        if (liste.Count == 0)
        {
            _liste.Children.Add(Ui.Vide("", "Aucun pilote ne correspond", "Modifiez la recherche ou le filtre."));
            return;
        }

        _liste.Children.Add(Ui.ListeCarte(liste.Take(MaxLignes).Select(Ligne)));
        if (liste.Count > MaxLignes)
        {
            _liste.Children.Add(Ui.Legende($"{MaxLignes} pilotes affichés sur {liste.Count}. Utilisez la recherche pour trouver les autres."));
        }
    }

    private static string Date(DateTime? d) => d is null ? "date inconnue" : d.Value.ToString("d MMM yyyy", CultureInfo.CurrentCulture);

    private static UIElement Ligne(Pilote p)
    {
        var detail = $"{p.Fabricant ?? "Fabricant inconnu"}  ·  version {p.Version ?? "n/d"}  ·  {Date(p.Date)}";

        var droite = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        if (p.Lien is { EstVerifie: true } lien)
        {
            var b = new HyperlinkButton { Content = $"Site {lien.Nom}", NavigateUri = new Uri(lien.Url!) };
            ToolTipService.SetToolTip(b, $"Page officielle, vérifiée le {lien.VerifieLe}");
            droite.Children.Add(b);
        }

        droite.Children.Add(p.Generique ? Ui.Badge(Niveau.Neutre, "Windows")
            : p.Ancien ? Ui.Badge(Niveau.Attention, "Ancien")
            : Ui.Badge(Niveau.Succes, "Récent"));
        return Ui.Ligne(p.Appareil, detail, droite);
    }

    private async Task<bool> ConfirmerAsync(string titre, string message, string action)
    {
        var dialogue = new ContentDialog
        {
            Title = titre,
            Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
            PrimaryButtonText = action,
            CloseButtonText = "Annuler",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };
        return await dialogue.ShowAsync() == ContentDialogResult.Primary;
    }

    private static async Task InstallerAsync(List<(ComparaisonPilote Comparaison, ContentControl Etat, Button Bouton)> lignes)
    {
        var ids = lignes.Select(l => l.Comparaison.Disponible.UpdateId).ToList();
        await Installations.LancerEnBlocAsync(Installations.GroupePilotes, ids,
            etape => Task.Run(() => InstallateurWindowsUpdate.Installer(etape, identifiants: ids)),
            r => new BilanInstallation(r.Reussi, r.Reussi ? "Pilotes installés" : "Installation des pilotes", r.Message, r.RedemarrageRequis));
    }
}
