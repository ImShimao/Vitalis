using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Vitalis.Core.Disques;
using Vitalis.Core.Materiel;

namespace Vitalis.App.Views;

/// <summary>Page « Disques » : santé des disques physiques et espace libre des lecteurs.</summary>
public sealed partial class DisquesPage : Page
{
    private CancellationTokenSource? _annulation;

    public DisquesPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await ChargerAsync();
        Unloaded += (_, _) => _annulation?.Cancel();
    }

    private async Task ChargerAsync()
    {
        _annulation?.Cancel();
        _annulation = new CancellationTokenSource();

        TexteStatut.Text = "Analyse en cours…";
        ListeDisques.Children.Clear();
        ListeLecteurs.Children.Clear();

        try
        {
            // Diagnostic : seulement en administrateur (les compteurs SMART ne sont lisibles qu'alors).
            Action<string>? journal = Droits.EstAdministrateur() ? EcrireDiagnostic : null;
            var rapport = await new AnalyseDisques(new WmiSource(), journal).AnalyserAsync(_annulation.Token);
            Afficher(rapport);
        }
        catch (OperationCanceledException)
        {
            // Page quittée pendant l'analyse : rien à afficher.
        }
        catch (Exception e)
        {
            TexteStatut.Text = $"Analyse impossible : {e.Message}";
        }
    }

    /// <summary>
    /// Ajoute une ligne au fichier %LOCALAPPDATA%\Vitalis\diagnostic-smart.txt.
    /// Contient des identifiants internes de disque (DeviceId, GUID) et les noms de modèle ; aucun numéro de série.
    /// </summary>
    private static void EcrireDiagnostic(string ligne)
    {
        try
        {
            var dossier = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vitalis");
            Directory.CreateDirectory(dossier);
            File.AppendAllText(Path.Combine(dossier, "diagnostic-smart.txt"),
                $"{DateTime.Now:HH:mm:ss} {ligne}{Environment.NewLine}");
        }
        catch (IOException)
        {
            // Le diagnostic ne doit jamais bloquer l'affichage.
        }
    }

    private void Afficher(RapportDisques rapport)
    {
        var bouton = !Droits.EstAdministrateur();
        BoutonAdmin.Visibility = bouton ? Visibility.Visible : Visibility.Collapsed;

        TexteStatut.Text = rapport.SmartComplet
            ? "Compteurs SMART lus pour tous les disques."
            : rapport.RaisonSmart ?? "Compteurs SMART incomplets.";

        AfficherSection(rapport.Disques, ListeDisques, "Aucun disque lisible", d => new[]
        {
            $"{d.Nom ?? "Disque sans nom"} · {TypeTexte(d.Type)} · {Gio(d.TailleOctets)} · {SanteTexte(d.Sante)}",
            d.TemperatureC is null && d.UsurePourcent is null
                ? "SMART non lu (droits administrateur requis)"
                : $"Température {d.TemperatureC?.ToString(CultureInfo.InvariantCulture) ?? "n/d"} °C · " +
                  $"Usure {d.UsurePourcent?.ToString(CultureInfo.InvariantCulture) ?? "n/d"} % · " +
                  $"Heures de marche {d.HeuresMarche?.ToString(CultureInfo.InvariantCulture) ?? "n/d"} · " +
                  $"Erreurs de lecture {d.ErreursLectureTotal?.ToString(CultureInfo.InvariantCulture) ?? "n/d"}",
        });

        AfficherSection(rapport.Lecteurs, ListeLecteurs, "Aucun lecteur fixe lisible", l => new[]
        {
            $"{l.Lettre} {l.Nom ?? string.Empty} · {Gio(l.EspaceLibreOctets)} libres sur {Gio(l.TailleOctets)} · " +
            $"{(l.PourcentLibre is null ? "n/d" : l.PourcentLibre.Value.ToString("0", CultureInfo.InvariantCulture))} % · {EspaceTexte(l.Statut)}",
        });
    }

    private static void AfficherSection<T>(Section<T> section, StackPanel conteneur, string vide, Func<T, string[]> lignes)
    {
        if (section.Etat == Disponibilite.Indisponible || section.Elements.Count == 0)
        {
            conteneur.Children.Add(Texte($"{vide}. {section.Raison}", secondaire: true));
            return;
        }

        if (section.Etat == Disponibilite.Partielle && section.Raison is not null)
        {
            conteneur.Children.Add(Texte(section.Raison, secondaire: true));
        }

        foreach (var element in section.Elements)
        {
            foreach (var ligne in lignes(element))
            {
                conteneur.Children.Add(Texte(ligne, secondaire: false));
            }
        }
    }

    private static TextBlock Texte(string texte, bool secondaire) => new()
    {
        Text = texte,
        TextWrapping = TextWrapping.Wrap,
        Foreground = (Brush)Application.Current.Resources[secondaire ? "VitalisTexteSecondaire" : "VitalisTexte"],
    };

    /// <summary>Le statut est écrit en toutes lettres avec un symbole : la couleur n'est jamais le seul moyen de le lire.</summary>
    private static string SanteTexte(StatutSante sante) => sante switch
    {
        StatutSante.Sain => "✔ Sain",
        StatutSante.Attention => "⚠ Attention",
        StatutSante.Critique => "✖ Critique",
        _ => "? Inconnu",
    };

    private static string EspaceTexte(StatutEspace statut) => statut switch
    {
        StatutEspace.Sain => "✔ Sain",
        StatutEspace.Attention => "⚠ Attention",
        _ => "✖ Critique",
    };

    private static string TypeTexte(TypeMedia type) => type switch
    {
        TypeMedia.Hdd => "HDD",
        TypeMedia.Ssd => "SSD",
        TypeMedia.Scm => "SCM",
        _ => "type inconnu",
    };

    private static string Gio(long? octets) => octets is null
        ? "n/d"
        : (octets.Value / 1024.0 / 1024.0 / 1024.0).ToString("0.0", CultureInfo.CurrentCulture) + " Gio";

    /// <summary>Demande une confirmation avant toute relance : Windows demandera ensuite l'élévation (UAC).</summary>
    private async void BoutonAdmin_Click(object sender, RoutedEventArgs e)
    {
        var dialogue = new ContentDialog
        {
            Title = "Relancer en administrateur ?",
            Content = Droits.MessageElevation,
            PrimaryButtonText = "Relancer",
            CloseButtonText = "Annuler",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot,
        };

        if (await dialogue.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = true, Verb = "runas" });
            Application.Current.Exit();
        }
        catch (Win32Exception)
        {
            TexteStatut.Text = "Relance annulée : Windows n'a pas accordé les droits administrateur.";
        }
    }
}
