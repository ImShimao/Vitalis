using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vitalis.Core.Materiel;
using Vitalis.Core.MisesAJour;

namespace Vitalis.App.Views;

/// <summary>Page « Mises à jour » : mises à jour Windows et logiciels (winget) en attente, installation après confirmation.</summary>
public sealed partial class MisesAJourPage : Page
{
    // Une seule installation à la fois dans toute l'application (winget et Windows Update ne s'enchaînent pas bien en parallèle).
    private static bool _installationEnCours;
    private readonly List<Button> _boutonsInstallation = [];

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
        _boutonsInstallation.Clear();
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
                ligne.Etat.Content = Ui.Texte($"{ligne.Logiciel.VersionActuelle}  →  {ligne.Logiciel.VersionDisponible}", secondaire: true);
                var courante = ligne;
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

    private void Verrouiller(bool verrou)
    {
        _installationEnCours = verrou;
        BoutonActualiser.IsEnabled = !verrou;
        foreach (var b in _boutonsInstallation)
        {
            b.IsEnabled = !verrou;
        }
    }

    private async Task InstallerLogicielsAsync(List<(MiseAJourLogiciel Logiciel, ContentControl Etat, Button Bouton)> lignes)
    {
        if (_installationEnCours || lignes.Count == 0)
        {
            return;
        }

        Verrouiller(true);
        int reussis = 0, echecs = 0;
        var redemarrage = false;
        try
        {
            foreach (var (logiciel, etat, bouton) in lignes)
            {
                etat.Content = Ui.Chargement("Mise à jour…");
                ResultatInstallation r;
                try
                {
                    r = await InstallateurWinget.MettreAJourAsync(logiciel.Id);
                }
                catch (Exception e)
                {
                    r = new ResultatInstallation(false, e.Message);
                }

                Vitalis.Core.Journal.JournalVitalis.Information($"winget {logiciel.Id} : {(r.Reussi ? "réussi" : "échec")}.");
                redemarrage |= r.RedemarrageRequis;
                var badge = r.Reussi ? Ui.Badge(Niveau.Succes, "À jour") : Ui.Badge(Niveau.Critique, "Échec");
                ToolTipService.SetToolTip(badge, r.Message);
                etat.Content = badge;
                if (r.Reussi)
                {
                    reussis++;
                    bouton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    echecs++;
                }
            }
        }
        finally
        {
            Verrouiller(false);
        }

        var resume = $"{Ui.Pluriel(reussis, "logiciel mis à jour", "logiciels mis à jour")}, {Ui.Pluriel(echecs, "échec")}." +
            (echecs > 0 ? " Survolez « Échec » pour la raison." : string.Empty) +
            (redemarrage ? " Redémarrez le PC pour terminer." : string.Empty);
        Corps.Children.Insert(0, Ui.Message(echecs == 0 ? InfoBarSeverity.Success : InfoBarSeverity.Warning, "Mises à jour des logiciels terminées", resume));
    }

    private async Task InstallerWindowsAsync(int nombre)
    {
        if (_installationEnCours || !await ConfirmerAsync("Installer les mises à jour Windows ?",
            $"Vitalis va télécharger et installer {Ui.Pluriel(nombre, "mise à jour", "mises à jour")} par Windows Update, comme le ferait Windows. Cela peut prendre longtemps, et un redémarrage peut être demandé. Les contrats de licence des mises à jour sont acceptés en votre nom.",
            "Installer"))
        {
            return;
        }

        Verrouiller(true);
        var message = Ui.Message(InfoBarSeverity.Informational, "Windows Update", "Préparation…");
        Corps.Children.Insert(0, message);
        try
        {
            var etape = new Progress<string>(t => message.Message = t);
            var r = await Task.Run(() => InstallateurWindowsUpdate.Installer(etape));
            message.Severity = r.Reussi ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
            message.Title = r.Reussi ? "Mises à jour Windows installées" : "Mises à jour Windows";
            message.Message = r.Message;
            if (r.RedemarrageRequis)
            {
                var b = Ui.Bouton("Redémarrer maintenant");
                b.Click += async (_, _) =>
                {
                    if (await ConfirmerAsync("Redémarrer le PC ?", "Enregistrez votre travail : toutes les applications vont se fermer.", "Redémarrer"))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("shutdown", "/r /t 0") { CreateNoWindow = true, UseShellExecute = false });
                    }
                };
                message.ActionButton = b;
            }

            Vitalis.Core.Journal.JournalVitalis.Information($"Windows Update : {(r.Reussi ? "réussi" : "échec")}.");
        }
        catch (Exception e)
        {
            message.Severity = InfoBarSeverity.Error;
            message.Title = "Installation impossible";
            message.Message = $"{e.GetType().Name} : {e.Message}";
        }
        finally
        {
            Verrouiller(false);
        }
    }
}
