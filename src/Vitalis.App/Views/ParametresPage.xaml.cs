using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vitalis.Core.Journal;
using Vitalis.Core.Parametres;

namespace Vitalis.App.Views;

/// <summary>Page « Paramètres » : thème, mode diagnostic, accès aux journaux.</summary>
public sealed partial class ParametresPage : Page
{
    private bool _chargement;

    public ParametresPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Charger();
    }

    private static string FichierPreferences => Path.Combine(DossierLocal, "preferences.json");

    public static string DossierLocal => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vitalis");

    private void Charger()
    {
        _chargement = true;
        var p = StockagePreferences.Charger(FichierPreferences);
        foreach (ComboBoxItem item in ChoixTheme.Items)
        {
            if (item.Tag as string == p.Theme)
            {
                ChoixTheme.SelectedItem = item;
            }
        }

        ModeDiagnostic.IsOn = p.ModeDiagnostic;
        _chargement = false;
    }

    private void ChoixTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_chargement || ChoixTheme.SelectedItem is not ComboBoxItem item || item.Tag is not string theme)
        {
            return;
        }

        var p = StockagePreferences.Charger(FichierPreferences) with { Theme = theme };
        StockagePreferences.Enregistrer(FichierPreferences, p);
        App.AppliquerTheme(theme);
        TexteStatut.Text = "Thème enregistré.";
    }

    private void ModeDiagnostic_Toggled(object sender, RoutedEventArgs e)
    {
        if (_chargement)
        {
            return;
        }

        var p = StockagePreferences.Charger(FichierPreferences) with { ModeDiagnostic = ModeDiagnostic.IsOn };
        StockagePreferences.Enregistrer(FichierPreferences, p);
        JournalVitalis.Information(ModeDiagnostic.IsOn ? "Mode diagnostic activé." : "Mode diagnostic désactivé.");
        TexteStatut.Text = "Enregistré. Redémarrez Vitalis pour appliquer le journal détaillé.";
    }

    private void BoutonJournaux_Click(object sender, RoutedEventArgs e)
    {
        var dossier = JournalVitalis.DossierJournal(DossierLocal);
        Directory.CreateDirectory(dossier);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dossier}\"") { UseShellExecute = false });
    }
}
