using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vitalis.Core.Materiel;
using Vitalis.Core.MisesAJour;

namespace Vitalis.App.Views;

/// <summary>Page « Mises à jour » : mises à jour Windows et logiciels (winget) en attente. Lecture seule.</summary>
public sealed partial class MisesAJourPage : Page
{
    public MisesAJourPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await ChargerAsync(forcer: false);
    }

    private async void BoutonActualiser_Click(object sender, RoutedEventArgs e) => await ChargerAsync(forcer: true);

    private Task ChargerAsync(bool forcer) => Ui.ChargerAsync(
        EtatAnalyse.MisesAJour, forcer, Corps, BoutonActualiser,
        "Recherche auprès de Windows Update et de winget… (environ 30 secondes, vous pouvez changer de page entre-temps)",
        ct => new AnalyseMisesAJour(new WuaCom(), new WingetCli()).AnalyserAsync(ct),
        Afficher);

    private void Afficher(RapportMisesAJour r)
    {
        var windows = r.EnAttente.Elements.Count;
        var logiciels = r.Logiciels.Elements.Count;
        var date = r.DerniereInstallationReussie is { } d ? d.ToString("d MMMM yyyy", CultureInfo.CurrentCulture) : "inconnue";

        var resume = new GrilleAdaptative { LargeurMinimale = 220 };
        resume.Children.Add(Ui.Carte(Ui.Statistique(
            r.EnAttente.Etat == Disponibilite.Indisponible ? "?" : windows.ToString(CultureInfo.CurrentCulture),
            "Mises à jour Windows en attente", "\uE895")));
        resume.Children.Add(Ui.Carte(Ui.Statistique(
            r.Logiciels.Etat == Disponibilite.Indisponible ? "?" : logiciels.ToString(CultureInfo.CurrentCulture),
            "Logiciels à mettre à jour (winget)", "\uE7B8")));
        resume.Children.Add(Ui.Carte(Ui.Statistique(date, "Dernière installation Windows réussie", "\uE823")));
        Corps.Children.Add(resume);

        if (r.RaisonHistorique is not null)
        {
            Corps.Children.Add(Ui.Message(InfoBarSeverity.Informational, "Historique Windows Update", r.RaisonHistorique));
        }

        // Windows Update
        Corps.Children.Add(Ui.TitreSection("Windows Update"));
        if (r.EnAttente.Etat == Disponibilite.Indisponible)
        {
            Corps.Children.Add(Ui.Message(InfoBarSeverity.Warning, "Recherche Windows Update indisponible", r.EnAttente.Raison));
        }
        else if (windows == 0)
        {
            Corps.Children.Add(Ui.Vide("\uE73E", "Windows est à jour", "Aucune mise à jour en attente."));
        }
        else
        {
            Corps.Children.Add(Ui.ListeCarte(r.EnAttente.Elements.Select(m =>
            {
                var details = new List<string>();
                if (m.Kb.Count > 0)
                {
                    details.Add(string.Join(", ", m.Kb.Select(k => k.StartsWith("KB", StringComparison.OrdinalIgnoreCase) ? k : "KB" + k)));
                }

                if (m.Gravite is not null)
                {
                    details.Add($"gravité : {m.Gravite}");
                }

                if (m.Obligatoire)
                {
                    details.Add("obligatoire");
                }

                return (UIElement)Ui.Ligne(m.Titre, string.Join("  ·  ", details),
                    Ui.Badge(Niveau.Neutre, m.Pilote ? "Pilote" : "Logiciel"),
                    Ui.Icone(m.Pilote ? "\uE772" : "\uE895", 18));
            })));
            Corps.Children.Add(Ui.Legende("Pour installer : Paramètres Windows › Windows Update. Vitalis n'installe rien."));
        }

        // winget
        Corps.Children.Add(Ui.TitreSection("Logiciels (winget)"));
        if (r.Logiciels.Etat == Disponibilite.Indisponible)
        {
            Corps.Children.Add(Ui.Message(InfoBarSeverity.Warning, "winget indisponible", r.Logiciels.Raison));
        }
        else if (logiciels == 0)
        {
            Corps.Children.Add(Ui.Vide("\uE73E", "Logiciels à jour", r.Logiciels.Raison ?? "winget ne signale aucune mise à jour."));
        }
        else
        {
            Corps.Children.Add(Ui.ListeCarte(r.Logiciels.Elements
                .OrderBy(l => l.Nom, StringComparer.CurrentCultureIgnoreCase)
                .Select(l => (UIElement)Ui.Ligne(l.Nom, l.Id, Ui.Texte($"{l.VersionActuelle}  →  {l.VersionDisponible}", secondaire: true)))));
            Corps.Children.Add(Ui.Legende("Pour mettre à jour un logiciel : winget upgrade --id <identifiant>, ou depuis le logiciel lui-même."));
        }
    }
}
