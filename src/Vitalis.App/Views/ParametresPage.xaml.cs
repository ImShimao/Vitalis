using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vitalis.Core;
using Vitalis.Core.Journal;
using Vitalis.Core.Parametres;

namespace Vitalis.App.Views;

/// <summary>Page « Paramètres » : thème, mode diagnostic, accès aux journaux, à propos.</summary>
public sealed partial class ParametresPage : Page
{
    private readonly ComboBox _theme = new() { MinWidth = 200 };
    private readonly ToggleSwitch _diagnostic = new() { OnContent = "Activé", OffContent = "Désactivé" };
    private readonly ToggleSwitch _planifiee = new() { OnContent = "Chaque lundi", OffContent = "Désactivée" };
    private readonly ToggleSwitch _notifications = new() { OnContent = "Activées", OffContent = "Désactivées" };
    private readonly InfoBar _statut = new() { IsClosable = true, IsOpen = false, Severity = InfoBarSeverity.Success };
    private bool _chargement;

    public ParametresPage()
    {
        InitializeComponent();
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(_theme, "Thème de l'application");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(_diagnostic, "Mode diagnostic");
        foreach (var (libelle, cle) in new[] { ("Suivre Windows", "Systeme"), ("Clair", "Clair"), ("Sombre", "Sombre") })
        {
            _theme.Items.Add(new ComboBoxItem { Content = libelle, Tag = cle });
        }

        _theme.SelectionChanged += Theme_SelectionChanged;
        _diagnostic.Toggled += Diagnostic_Toggled;
        _planifiee.Toggled += Planifiee_Toggled;
        _notifications.Toggled += Notifications_Toggled;
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(_planifiee, "Analyse planifiée");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(_notifications, "Notifications");
        Construire();
        Loaded += (_, _) => Charger();
    }

    private static string FichierPreferences => Path.Combine(DossierLocal, "preferences.json");

    public static string DossierLocal => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vitalis");

    private void Construire()
    {
        Corps.Children.Add(_statut);

        Corps.Children.Add(Ui.TitreSection("Apparence"));
        Corps.Children.Add(Reglage("\uE790", "Thème", "Clair, sombre, ou identique à Windows.", _theme));

        Corps.Children.Add(Ui.TitreSection("Analyse automatique"));
        Corps.Children.Add(Reglage("", "Analyse planifiée",
            "Chaque lundi à 12 h (ou au prochain démarrage si le PC était éteint), Vitalis analyse le PC sans ouvrir de fenêtre. " +
            "La tâche est créée dans le Planificateur de tâches avec les droits administrateur, nécessaires à la lecture SMART et à Windows Update. Elle ne modifie rien.", _planifiee));
        var tester = Ui.Bouton("Tester", "");
        tester.Click += (_, _) => Program.Notifier("Vitalis", "Les notifications fonctionnent.");
        var notif = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        notif.Children.Add(tester);
        notif.Children.Add(_notifications);
        Corps.Children.Add(Reglage("", "Notifications",
            "Après une analyse planifiée : score, mises à jour Windows, logiciels et pilotes à mettre à jour.", notif));

        Corps.Children.Add(Ui.TitreSection("Diagnostic"));
        Corps.Children.Add(Reglage("\uEBE8", "Mode diagnostic",
            "Journal détaillé, au prochain démarrage. Il ne contient ni nom d'utilisateur, ni nom de PC, ni numéro de série.", _diagnostic));
        var journaux = Ui.Bouton("Ouvrir", "\uE838");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(journaux, "Ouvrir le dossier des journaux");
        journaux.Click += (_, _) => OuvrirDossier(JournalVitalis.DossierJournal(DossierLocal));
        Corps.Children.Add(Reglage("\uE8A5", "Dossier des journaux", "Un fichier par jour, gardé 14 jours. Utile pour signaler un problème.", journaux));

        Corps.Children.Add(Ui.TitreSection("À propos"));
        var verifier = Ui.Bouton("Rechercher une mise à jour", "\uE895");
        verifier.Click += async (_, _) => await VerifierVersionAsync(verifier);
        Corps.Children.Add(Reglage("\uE895", "Mise à jour de Vitalis",
            "Interroge la page des versions de Vitalis sur GitHub, seulement quand vous cliquez. Rien n'est téléchargé automatiquement.", verifier));
        Corps.Children.Add(Reglage("\uE8F1", "Composants tiers",
            "Windows App SDK et .NET (MIT, Microsoft) · CommunityToolkit.Mvvm (MIT) · LibreHardwareMonitorLib (MPL-2.0, non modifiée) · Serilog (Apache-2.0) · System.Management (MIT). Détail : licences-tiers.md.", null));
        Corps.Children.Add(Reglage("\uE9D9", $"{AppInfo.Nom} {AppInfo.Version}",
            "Diagnostic, pilotes et mises à jour du PC. Toutes les données restent sur ce PC : rien n'est envoyé sur Internet. Lancé en administrateur ; rien n'est modifié sans votre confirmation.", null));
    }

    /// <summary>Ligne de réglage façon Paramètres Windows : icône, titre, explication, contrôle à droite.</summary>
    private static UIElement Reglage(string glyphe, string titre, string description, FrameworkElement? controle)
    {
        var g = new Grid { ColumnSpacing = 16 };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icone = Ui.Icone(glyphe, 20);
        icone.VerticalAlignment = VerticalAlignment.Center;
        g.Children.Add(icone);

        var texte = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        texte.Children.Add(Ui.Texte(titre));
        texte.Children.Add(Ui.Legende(description));
        Grid.SetColumn(texte, 1);
        g.Children.Add(texte);

        if (controle is not null)
        {
            controle.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(controle, 2);
            g.Children.Add(controle);
        }

        return Ui.Carte(g, new Thickness(16, 14, 16, 14));
    }

    private void Charger()
    {
        _chargement = true;
        var p = StockagePreferences.Charger(FichierPreferences);
        _theme.SelectedItem = _theme.Items.OfType<ComboBoxItem>().FirstOrDefault(i => i.Tag as string == p.Theme) ?? _theme.Items[0];
        _diagnostic.IsOn = p.ModeDiagnostic;
        _notifications.IsOn = p.NotificationsActives;
        _planifiee.IsOn = AnalysePlanifiee.Existe();
        _chargement = false;
    }

    private void Theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_chargement || _theme.SelectedItem is not ComboBoxItem { Tag: string theme })
        {
            return;
        }

        StockagePreferences.Enregistrer(FichierPreferences, StockagePreferences.Charger(FichierPreferences) with { Theme = theme });
        App.AppliquerTheme(theme);
        Statut("Thème enregistré.");
    }

    private void Diagnostic_Toggled(object sender, RoutedEventArgs e)
    {
        if (_chargement)
        {
            return;
        }

        StockagePreferences.Enregistrer(FichierPreferences, StockagePreferences.Charger(FichierPreferences) with { ModeDiagnostic = _diagnostic.IsOn });
        JournalVitalis.Information(_diagnostic.IsOn ? "Mode diagnostic activé." : "Mode diagnostic désactivé.");
        Statut("Enregistré. Redémarrez Vitalis pour appliquer le journal détaillé.");
    }

    private async Task VerifierVersionAsync(Button bouton)
    {
        bouton.IsEnabled = false;
        try
        {
            var v = await MiseAJourVitalis.VerifierAsync();
            if (v.PlusRecente)
            {
                Statut($"Vitalis {v.Version} est disponible (vous avez {AppInfo.Version}). La page de téléchargement s'ouvre.", InfoBarSeverity.Informational);
                await Windows.System.Launcher.LaunchUriAsync(new Uri(v.Page));
            }
            else
            {
                Statut($"Vitalis est à jour ({AppInfo.Version}).");
            }
        }
        catch (Exception ex)
        {
            Statut($"Vérification impossible : {ex.Message}", InfoBarSeverity.Warning);
        }
        finally
        {
            bouton.IsEnabled = true;
        }
    }

    private void Planifiee_Toggled(object sender, RoutedEventArgs e)
    {
        if (_chargement)
        {
            return;
        }

        try
        {
            if (_planifiee.IsOn)
            {
                AnalysePlanifiee.Creer(Environment.ProcessPath!);
                Statut("Analyse planifiée créée : chaque lundi à 12 h.");
            }
            else
            {
                AnalysePlanifiee.Supprimer();
                Statut("Analyse planifiée supprimée.");
            }

            JournalVitalis.Information(_planifiee.IsOn ? "Analyse planifiée créée." : "Analyse planifiée supprimée.");
        }
        catch (Exception ex)
        {
            JournalVitalis.Erreur("Planificateur de tâches", ex);
            _chargement = true;
            _planifiee.IsOn = AnalysePlanifiee.Existe();
            _chargement = false;
            Statut($"Planificateur de tâches : {ex.Message}", InfoBarSeverity.Error);
        }
    }

    private void Notifications_Toggled(object sender, RoutedEventArgs e)
    {
        if (_chargement)
        {
            return;
        }

        StockagePreferences.Enregistrer(FichierPreferences, StockagePreferences.Charger(FichierPreferences) with { NotificationsActives = _notifications.IsOn });
        Statut(_notifications.IsOn ? "Notifications activées." : "Notifications désactivées.");
    }

    private void Statut(string message, InfoBarSeverity gravite = InfoBarSeverity.Success)
    {
        _statut.Severity = gravite;
        _statut.Title = message;
        _statut.IsOpen = true;
    }

    private static void OuvrirDossier(string dossier)
    {
        Directory.CreateDirectory(dossier);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dossier}\"") { UseShellExecute = false });
    }
}
