using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vitalis.Core.Materiel;
using Vitalis.Core.MisesAJour;

namespace Vitalis.App.Views;

/// <summary>Page « Mises à jour » : mises à jour Windows et logiciels (winget) en attente, installation après confirmation.</summary>
public sealed partial class MisesAJourPage : Page
{
    // Les installations appartiennent à la session (Installations) : la page ne fait que les afficher.
    // Changer de page ne les arrête pas ; en revenant, Rafraichir remet l'état de chaque ligne.
    private readonly List<Button> _boutonsInstallation = [];
    private readonly List<Action> _lignes = [];
    private readonly ContentControl _bilanWindows = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch };
    private readonly ContentControl _bilanLogiciels = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch };

    public MisesAJourPage()
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

    private void Rafraichir()
    {
        BoutonActualiser.IsEnabled = !Installations.EnCours && !EtatAnalyse.MisesAJour.EnCours;
        foreach (var b in _boutonsInstallation)
        {
            b.IsEnabled = !Installations.EnCours;
        }

        foreach (var ligne in _lignes)
        {
            ligne();
        }

        if (Installations.Suivi(Installations.CleWindows) is { Etat: EtatInstallation.EnCours } w)
        {
            _bilanWindows.Content = Ui.Message(InfoBarSeverity.Informational, "Windows Update", w.Message);
        }
        else
        {
            Ui.AfficherBilan(_bilanWindows, Installations.Bilan(Installations.GroupeWindows));
        }

        Ui.AfficherBilan(_bilanLogiciels, Installations.Bilan(Installations.GroupeLogiciels));
    }

    private async void BoutonActualiser_Click(object sender, RoutedEventArgs e) => await ChargerAsync(forcer: true);

    private Task ChargerAsync(bool forcer) => Ui.ChargerAsync(
        EtatAnalyse.MisesAJour, forcer, Corps, BoutonActualiser,
        "Recherche auprès de Windows Update et de winget… (environ 30 secondes, vous pouvez changer de page entre-temps)",
        ct => new AnalyseMisesAJour(new WuaCom(), new WingetCli()).AnalyserAsync(ct),
        r =>
        {
            Afficher(r);
            Rafraichir();
        });

    private void Afficher(RapportMisesAJour r)
    {
        _boutonsInstallation.Clear();
        _lignes.Clear();
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
        (_bilanWindows.Parent as Panel)?.Children.Remove(_bilanWindows);
        Corps.Children.Add(_bilanWindows);
        if (r.EnAttente.Etat == Disponibilite.Indisponible)
        {
            Corps.Children.Add(Ui.Message(InfoBarSeverity.Warning, "Recherche Windows Update indisponible", r.EnAttente.Raison));
        }
        else if (windows == 0)
        {
            Corps.Children.Add(Ui.Vide("\uE73E", "Windows est à jour", "Aucune mise à jour en attente.", BoutonWindowsUpdate()));
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
            var installer = Ui.BoutonAccent("Installer les mises à jour Windows", "\uE896");
            installer.Click += async (_, _) => await InstallerWindowsAsync(windows);
            _boutonsInstallation.Add(installer);
            Corps.Children.Add(ActionsSection(installer, BoutonWindowsUpdate()));
        }

        // winget
        Corps.Children.Add(Ui.TitreSection("Logiciels (winget)"));
        (_bilanLogiciels.Parent as Panel)?.Children.Remove(_bilanLogiciels);
        Corps.Children.Add(_bilanLogiciels);
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
            var liste = r.Logiciels.Elements.OrderBy(l => l.Nom, StringComparer.CurrentCultureIgnoreCase).ToList();
            var tout = Ui.BoutonAccent($"Tout mettre à jour ({liste.Count})", "");
            _boutonsInstallation.Add(tout);
            var lignes = liste.Select(l => (Logiciel: l, Etat: new ContentControl(), Bouton: Ui.Bouton("Mettre à jour", ""))).ToList();
            foreach (var ligne in lignes)
            {
                _boutonsInstallation.Add(ligne.Bouton);
                var courante = ligne;
                _lignes.Add(() => Ui.AppliquerSuivi(courante.Etat, courante.Bouton, Installations.Suivi(courante.Logiciel.Id),
                    () => Ui.Texte($"{courante.Logiciel.VersionActuelle}  →  {courante.Logiciel.VersionDisponible}", secondaire: true), "À jour"));
                ligne.Bouton.Click += async (_, _) =>
                {
                    if (await ConfirmerAsync("Mettre à jour ce logiciel ?",
                        $"{courante.Logiciel.Nom} passera de la version {courante.Logiciel.VersionActuelle} à {courante.Logiciel.VersionDisponible}, depuis la source officielle winget. Fermez le logiciel avant de continuer.",
                        "Mettre à jour"))
                    {
                        await InstallerLogicielsAsync([courante]);
                    }
                };
            }

            tout.Click += async (_, _) =>
            {
                var restants = lignes.Where(l => l.Bouton.Visibility == Visibility.Visible).ToList();
                if (await ConfirmerAsync("Tout mettre à jour ?",
                    $"{Ui.Pluriel(restants.Count, "logiciel")} vont être mis à jour l'un après l'autre depuis la source officielle winget. Fermez-les avant de continuer. Certains installateurs peuvent afficher leur propre fenêtre ou demander un redémarrage.",
                    "Tout mettre à jour"))
                {
                    await InstallerLogicielsAsync(restants);
                }
            };

            Corps.Children.Add(ActionsSection(tout));
            Corps.Children.Add(Ui.ListeCarte(lignes.Select(l =>
            {
                var droite = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
                l.Etat.VerticalAlignment = VerticalAlignment.Center;
                droite.Children.Add(l.Etat);
                droite.Children.Add(l.Bouton);
                return (UIElement)Ui.Ligne(l.Logiciel.Nom, l.Logiciel.Id, droite);
            })));
            Corps.Children.Add(Ui.Legende("Les mises à jour viennent de la source officielle winget (Microsoft). Les logiciels gérés par leur propre outil (Steam, jeux, etc.) peuvent refuser : mettez-les à jour depuis le logiciel."));
        }
    }

    private static UIElement ActionsSection(params UIElement[] boutons)
    {
        var p = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        foreach (var b in boutons)
        {
            p.Children.Add(b);
        }

        return p;
    }

    private static Button BoutonWindowsUpdate()
    {
        var b = Ui.Bouton("Ouvrir Windows Update", "");
        b.Click += async (_, _) => await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:windowsupdate"));
        return b;
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

    private static async Task InstallerLogicielsAsync(List<(MiseAJourLogiciel Logiciel, ContentControl Etat, Button Bouton)> lignes)
    {
        await Installations.LancerAsync(Installations.GroupeLogiciels, lignes.Select(l => l.Logiciel.Id).ToList(),
            (id, _) => InstallateurWinget.MettreAJourAsync(id),
            resultats =>
            {
                var reussis = resultats.Count(r => r.Reussi);
                var echecs = resultats.Count - reussis;
                var redemarrage = resultats.Any(r => r.RedemarrageRequis);
                var resume = $"{Ui.Pluriel(reussis, "logiciel mis à jour", "logiciels mis à jour")}, {Ui.Pluriel(echecs, "échec")}." +
                    (echecs > 0 ? " Survolez « Échec » pour la raison." : string.Empty) +
                    (redemarrage ? " Redémarrez le PC pour terminer." : string.Empty);
                return new BilanInstallation(echecs == 0, "Mises à jour des logiciels terminées", resume, redemarrage);
            });
    }

    private async Task InstallerWindowsAsync(int nombre)
    {
        if (Installations.EnCours || !await ConfirmerAsync("Installer les mises à jour Windows ?",
            $"Vitalis va télécharger et installer {Ui.Pluriel(nombre, "mise à jour", "mises à jour")} par Windows Update, comme le ferait Windows. Cela peut prendre longtemps, et un redémarrage peut être demandé. Les contrats de licence des mises à jour sont acceptés en votre nom. Vous pouvez changer de page pendant l'installation.",
            "Installer"))
        {
            return;
        }

        await Installations.LancerAsync(Installations.GroupeWindows, [Installations.CleWindows],
            (_, etape) => Task.Run(() => InstallateurWindowsUpdate.Installer(etape)),
            r => new BilanInstallation(r[0].Reussi, r[0].Reussi ? "Mises à jour Windows installées" : "Mises à jour Windows", r[0].Message, r[0].RedemarrageRequis));
    }
}
