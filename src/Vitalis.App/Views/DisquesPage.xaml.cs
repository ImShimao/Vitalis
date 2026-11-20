using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vitalis.Core.Disques;
using Vitalis.Core.Materiel;

namespace Vitalis.App.Views;

/// <summary>Page « Disques » : santé des disques physiques et espace libre des lecteurs.</summary>
public sealed partial class DisquesPage : Page
{
    public DisquesPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await ChargerAsync(forcer: false);
    }

    private async void BoutonActualiser_Click(object sender, RoutedEventArgs e) => await ChargerAsync(forcer: true);

    private Task ChargerAsync(bool forcer)
    {
        // Diagnostic : seulement en administrateur (les compteurs SMART ne sont lisibles qu'alors).
        Action<string>? journal = Droits.EstAdministrateur() ? EcrireDiagnostic : null;
        return Ui.ChargerAsync(
            EtatAnalyse.Disques, forcer, Corps, BoutonActualiser,
            "Lecture des disques et des lecteurs… (quelques secondes)",
            ct => new AnalyseDisques(new WmiSource(), journal).AnalyserAsync(ct),
            Afficher);
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
        // SMART : message, et relance administrateur seulement si elle peut aider.
        if (!rapport.SmartComplet)
        {
            Button? action = null;
            if (!Droits.EstAdministrateur())
            {
                action = new Button { Content = "Relancer en administrateur" };
                action.Click += BoutonAdmin_Click;
            }

            Corps.Children.Add(Ui.Message(InfoBarSeverity.Informational, "Compteurs SMART incomplets",
                rapport.RaisonSmart ?? "Température, usure et heures de marche ne sont pas lisibles.", action));
        }

        Corps.Children.Add(Ui.TitreSection("Disques physiques"));
        if (rapport.Disques.Elements.Count == 0)
        {
            Corps.Children.Add(Ui.Vide("\uEDA2", "Aucun disque lisible", rapport.Disques.Raison ?? "Windows n'a renvoyé aucun disque."));
        }
        else
        {
            var grille = new GrilleAdaptative { LargeurMinimale = 340 };
            foreach (var d in rapport.Disques.Elements)
            {
                grille.Children.Add(CarteDisque(d));
            }

            Corps.Children.Add(grille);
        }

        Corps.Children.Add(Ui.TitreSection("Espace libre par lecteur"));
        if (rapport.Lecteurs.Elements.Count == 0)
        {
            Corps.Children.Add(Ui.Vide("\uE7B8", "Aucun lecteur fixe lisible", rapport.Lecteurs.Raison ?? "Windows n'a renvoyé aucun lecteur."));
        }
        else
        {
            var grille = new GrilleAdaptative { LargeurMinimale = 300 };
            foreach (var l in rapport.Lecteurs.Elements)
            {
                grille.Children.Add(CarteLecteur(l));
            }

            Corps.Children.Add(grille);
        }
    }

    private static UIElement CarteDisque(DisqueSante d)
    {
        var p = new StackPanel { Spacing = 12 };
        p.Children.Add(Ui.EnTeteCarte("\uEDA2", d.Nom ?? "Disque sans nom", d.Sante switch
        {
            StatutSante.Sain => Ui.Badge(Niveau.Succes, "Sain"),
            StatutSante.Attention => Ui.Badge(Niveau.Attention, "Attention"),
            StatutSante.Critique => Ui.Badge(Niveau.Critique, "Critique"),
            _ => Ui.Badge(Niveau.Neutre, "Inconnu"),
        }));

        var smartLu = d.TemperatureC is not null || d.UsurePourcent is not null || d.HeuresMarche is not null;
        var lignes = new List<(string, string?)>
        {
            ("Type", d.Type switch { TypeMedia.Hdd => "Disque dur (HDD)", TypeMedia.Ssd => "SSD", TypeMedia.Scm => "Mémoire de stockage (SCM)", _ => "inconnu" }),
            ("Capacité", Ui.Gio(d.TailleOctets)),
        };

        // Sans compteurs SMART, quatre lignes « non disponible » n'apprennent rien : une seule phrase les remplace.
        if (smartLu)
        {
            lignes.AddRange(
            [
            ("Température", d.TemperatureC is null ? null : $"{d.TemperatureC} °C" + (d.TemperatureMaxC is null ? string.Empty : $" (max. {d.TemperatureMaxC} °C)")),
            ("Usure", d.UsurePourcent is null ? null : $"{d.UsurePourcent} %"),
            ("Heures de marche", d.HeuresMarche?.ToString("N0", CultureInfo.CurrentCulture)),
            ("Erreurs de lecture", d.ErreursLectureTotal?.ToString("N0", CultureInfo.CurrentCulture)),
            ]);
        }

        p.Children.Add(Ui.Proprietes(lignes));

        if (!smartLu)
        {
            p.Children.Add(Ui.Legende("Température, usure et heures de marche : non fournies par Windows pour ce disque."));
        }

        return Ui.Carte(p);
    }

    private static UIElement CarteLecteur(LecteurEspace l)
    {
        var p = new StackPanel { Spacing = 10 };
        var titre = string.IsNullOrWhiteSpace(l.Nom) ? $"Lecteur {l.Lettre}" : $"{l.Nom} ({l.Lettre})";
        p.Children.Add(Ui.EnTeteCarte("\uE7B8", titre, l.Statut switch
        {
            StatutEspace.Sain => Ui.Badge(Niveau.Succes, "Suffisant"),
            StatutEspace.Attention => Ui.Badge(Niveau.Attention, "À surveiller"),
            _ => Ui.Badge(Niveau.Critique, "Presque plein"),
        }));

        if (l.PourcentLibre is { } libre)
        {
            var barre = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 100 - libre,
                Height = 6,
                Foreground = Ui.Pinceau(l.Statut switch
                {
                    StatutEspace.Sain => "AccentFillColorDefaultBrush",
                    StatutEspace.Attention => "VitalisAttention",
                    _ => "VitalisCritique",
                }),
            };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(barre, $"{100 - libre:0} % utilisés");
            p.Children.Add(barre);
        }

        p.Children.Add(Ui.Texte($"{Ui.Gio(l.EspaceLibreOctets)} libres sur {Ui.Gio(l.TailleOctets)}"
            + (l.PourcentLibre is null ? string.Empty : $"  ·  {l.PourcentLibre.Value.ToString("0", CultureInfo.CurrentCulture)} % libre"), secondaire: true));
        return Ui.Carte(p);
    }

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
            Corps.Children.Insert(0, Ui.Message(InfoBarSeverity.Warning, "Relance annulée", "Windows n'a pas accordé les droits administrateur."));
        }
    }
}
