using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vitalis.Core.Materiel;

namespace Vitalis.App.Views;

/// <summary>Page « Matériel » : fiche des composants (processeur, mémoire, carte mère, graphique, écrans, réseau, audio).</summary>
public sealed partial class MaterielPage : Page
{
    public MaterielPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await ChargerAsync(forcer: false);
    }

    private async void BoutonActualiser_Click(object sender, RoutedEventArgs e) => await ChargerAsync(forcer: true);

    private Task ChargerAsync(bool forcer) => Ui.ChargerAsync(
        EtatAnalyse.Materiel, forcer, Corps, BoutonActualiser,
        "Lecture des composants… (quelques secondes)",
        ct => new InventaireMateriel(new WmiSource()).AnalyserAsync(ct),
        Afficher);

    private void Afficher(RapportMateriel r)
    {
        var cpu = r.Processeur.Elements.FirstOrDefault();
        var memoire = Conversions.MemoireTotaleOctets(r.Memoire.Elements);
        var gpu = r.CartesGraphiques.Elements.FirstOrDefault();

        // Résumé en trois chiffres.
        var resume = new GrilleAdaptative { LargeurMinimale = 220 };
        resume.Children.Add(Ui.Carte(Ui.Statistique(cpu?.Nom ?? "inconnu", "Processeur", "\uE950")));
        resume.Children.Add(Ui.Carte(Ui.Statistique(memoire is null ? "inconnue" : Ui.Gio(memoire), $"Mémoire vive · {Ui.Pluriel(r.Memoire.Elements.Count, "barrette")}", "\uE964")));
        resume.Children.Add(Ui.Carte(Ui.Statistique(gpu?.Nom ?? "inconnue", "Carte graphique", "\uE7F4")));
        Corps.Children.Add(resume);

        var grille = new GrilleAdaptative { LargeurMinimale = 420 };
        grille.Children.Add(Fiche("\uE950", "Processeur", r.Processeur, p =>
        [
            ("Modèle", p.Nom),
            ("Fabricant", p.Fabricant),
            ("Cœurs / threads", p.Coeurs is null ? null : $"{p.Coeurs} cœurs · {p.ThreadsLogiques?.ToString(CultureInfo.CurrentCulture) ?? "?"} threads"),
            ("Fréquence maximale", p.FrequenceMaxMHz is null ? null : $"{p.FrequenceMaxMHz / 1000.0:0.00} GHz"),
            ("Cache L2 / L3", p.CacheL2Ko is null && p.CacheL3Ko is null ? null : $"{Ko(p.CacheL2Ko)} / {Ko(p.CacheL3Ko)}"),
        ]));

        grille.Children.Add(Fiche("\uE964", "Mémoire vive", r.Memoire, m =>
        [
            ($"Emplacement {m.Emplacement ?? "?"}", $"{Ui.Gio(m.CapaciteOctets)}" + (m.VitesseMHz is null ? string.Empty : $" · {m.VitesseMHz} MHz") + (m.Fabricant is null ? string.Empty : $" · {m.Fabricant.Trim()}")),
        ], fusionner: true));

        grille.Children.Add(FicheCarteMere(r));

        grille.Children.Add(Fiche("\uE7F4", "Carte graphique", r.CartesGraphiques, g =>
        [
            ("Modèle", g.Nom),
            ("Mémoire (WMI, plafonnée à 4 Gio)", g.MemoireOctets is null ? null : Ui.Gio(g.MemoireOctets)),
            ("Pilote", g.VersionPilote is null ? null : $"{g.VersionPilote}" + (g.DatePilote is null ? string.Empty : $" du {g.DatePilote.Value.ToString("d MMMM yyyy", CultureInfo.CurrentCulture)}")),
            ("Affichage", g.LargeurPx is null ? null : $"{g.LargeurPx} × {g.HauteurPx} px" + (g.FrequenceHz is null ? string.Empty : $" à {g.FrequenceHz} Hz")),
        ]));

        grille.Children.Add(Fiche("\uE7F8", "Écrans", r.Ecrans, e => [(e.Fabricant ?? "Fabricant inconnu", e.Nom)], fusionner: true));
        grille.Children.Add(Fiche("\uE839", "Réseau", r.Reseau, a => [(a.Nom ?? "Adaptateur", a.Description)], fusionner: true));
        grille.Children.Add(Fiche("\uE767", "Audio", r.Audio, a => [(a.Nom ?? "Périphérique", a.Fabricant)], fusionner: true));
        Corps.Children.Add(grille);
    }

    private static string Ko(int? ko) => ko is null ? "?" : ko >= 1024 ? $"{ko / 1024.0:0.#} Mo" : $"{ko} Ko";

    private static UIElement FicheCarteMere(RapportMateriel r)
    {
        var p = new StackPanel { Spacing = 14 };
        p.Children.Add(Ui.EnTeteCarte("\uE8EA", "Carte mère et BIOS", BadgeSection(r.CarteMere.Etat)));
        var cm = r.CarteMere.Elements.FirstOrDefault();
        var bios = r.Bios.Elements.FirstOrDefault();
        p.Children.Add(Ui.Proprietes(
        [
            ("Carte mère", cm is null ? null : $"{cm.Fabricant} {cm.Modele}".Trim()),
            ("Révision", cm?.Version),
            ("BIOS", bios is null ? null : $"{bios.Fabricant} {bios.Version}".Trim()),
            ("Date du BIOS", bios?.DateSortie?.ToString("d MMMM yyyy", CultureInfo.CurrentCulture)),
        ]));
        return Ui.Carte(p);
    }

    /// <summary>Fiche d'une famille. <paramref name="fusionner"/> : une ligne par élément dans une seule grille.</summary>
    private static UIElement Fiche<T>(string glyphe, string titre, Section<T> section, Func<T, (string, string?)[]> lignes, bool fusionner = false)
    {
        var p = new StackPanel { Spacing = 14 };
        p.Children.Add(Ui.EnTeteCarte(glyphe, titre, BadgeSection(section.Etat)));

        if (section.Elements.Count == 0)
        {
            p.Children.Add(Ui.Texte(section.Raison ?? "Rien de détecté.", secondaire: true));
            return Ui.Carte(p);
        }

        if (fusionner)
        {
            p.Children.Add(Ui.Proprietes(section.Elements.SelectMany(lignes)));
        }
        else
        {
            foreach (var e in section.Elements)
            {
                p.Children.Add(Ui.Proprietes(lignes(e)));
            }
        }

        if (section.Etat == Disponibilite.Partielle && section.Raison is not null)
        {
            p.Children.Add(Ui.Legende(section.Raison));
        }

        return Ui.Carte(p);
    }

    private static Border BadgeSection(Disponibilite etat) => etat switch
    {
        Disponibilite.Disponible => Ui.Badge(Niveau.Succes, "Lu"),
        Disponibilite.Partielle => Ui.Badge(Niveau.Attention, "Partiel"),
        _ => Ui.Badge(Niveau.Neutre, "Indisponible"),
    };
}
