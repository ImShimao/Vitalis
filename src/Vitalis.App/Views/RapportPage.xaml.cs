using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vitalis.Core.Rapport;
using Vitalis.Core.TableauDeBord;

namespace Vitalis.App.Views;

/// <summary>Page « Rapport » : export HTML ou PDF de la dernière analyse du tableau de bord.</summary>
public sealed partial class RapportPage : Page
{
    private static readonly string DossierRapports = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Vitalis");

    private UIElement? _message;

    public RapportPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Afficher();
    }

    private void Afficher()
    {
        Corps.Children.Clear();
        if (_message is not null)
        {
            Corps.Children.Add(_message);
        }

        var analyse = EtatAnalyse.Tableau.Valeur;
        if (analyse is null)
        {
            var aller = Ui.BoutonAccent("Aller au tableau de bord", "\uE80F");
            aller.Click += (_, _) => App.Naviguer("tableau-de-bord");
            Corps.Children.Add(Ui.Vide("\uE9F9", "Aucune analyse à exporter",
                EtatAnalyse.Tableau.EnCours
                    ? "Une analyse est en cours sur le tableau de bord. Revenez ici quand elle sera terminée."
                    : "Le rapport reprend la dernière analyse du tableau de bord. Lancez-en une d'abord (environ 30 secondes).",
                aller));
            Corps.Children.Add(Contenu());
            return;
        }

        var p = new StackPanel { Spacing = 14 };
        p.Children.Add(Ui.EnTeteCarte("\uE9F9", "Exporter l'analyse", Ui.Badge(Niveau.Succes, $"Analyse {Ui.Quand(analyse.Date)}")));
        p.Children.Add(Ui.Texte($"Score : {(analyse.Score is null ? "indisponible" : $"{analyse.Score} / 100")}  ·  {analyse.Cartes.Count} rubriques", secondaire: true));

        var boutons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var html = Ui.BoutonAccent("Exporter en HTML", "\uE774");
        var pdf = Ui.Bouton("Exporter en PDF", "\uEA90");
        var dossier = Ui.Bouton("Ouvrir le dossier", "\uE838");
        html.Click += async (_, _) => await ExporterAsync(analyse, pdf: false, html, pdf);
        pdf.Click += async (_, _) => await ExporterAsync(analyse, pdf: true, html, pdf);
        dossier.Click += (_, _) => OuvrirDossier();
        boutons.Children.Add(html);
        boutons.Children.Add(pdf);
        boutons.Children.Add(dossier);
        p.Children.Add(boutons);
        p.Children.Add(Ui.Legende($"Enregistré dans {DossierRapports.Replace(Environment.UserName, "[utilisateur]", StringComparison.OrdinalIgnoreCase)}. Le PDF est produit par Microsoft Edge, déjà installé avec Windows."));
        Corps.Children.Add(Ui.Carte(p));
        Corps.Children.Add(Contenu());
    }

    private static UIElement Contenu()
    {
        var p = new StackPanel { Spacing = 10 };
        p.Children.Add(Ui.EnTeteCarte("\uE72E", "Ce que contient le rapport"));
        p.Children.Add(Ui.Texte("Score et son détail, matériel, santé des disques, espace libre, mises à jour en attente.", secondaire: true));
        p.Children.Add(Ui.Texte("Votre nom d'utilisateur et le nom du PC sont masqués. Aucun numéro de série n'est inclus. Le fichier reste sur votre PC : rien n'est envoyé.", secondaire: true));
        return Ui.Carte(p);
    }

    private async Task ExporterAsync(ResultatTableauDeBord analyse, bool pdf, Button boutonHtml, Button boutonPdf)
    {
        boutonHtml.IsEnabled = boutonPdf.IsEnabled = false;
        string? fichier = null;
        string? erreur = null;
        var pilotes = EtatAnalyse.Pilotes.Valeur;
        try
        {
            (fichier, erreur) = await Task.Run(() =>
            {
                IReadOnlyList<Vitalis.Core.SanteLive.ComposantLive>? capteurs = null;
                try
                {
                    using var lecteur = new Vitalis.Core.SanteLive.CapteursLive();
                    capteurs = lecteur.Lire();
                }
                catch (Exception)
                {
                    // Capteurs illisibles : le rapport l'indique, sans bloquer l'export.
                }

                Directory.CreateDirectory(DossierRapports);
                var nom = Path.Combine(DossierRapports, $"rapport-{DateTime.Now:yyyyMMdd-HHmmss}");
                File.WriteAllText(nom + ".html", new RapportHtml(Environment.UserName, Environment.MachineName).Generer(analyse, pilotes?.Installes, pilotes?.Disponibles.Etat == Vitalis.Core.Materiel.Disponibilite.Disponible ? pilotes.Disponibles.Elements : null, capteurs), System.Text.Encoding.UTF8);
                if (!pdf)
                {
                    return (nom + ".html", (string?)null);
                }

                // Edge headless : quelques secondes, hors du thread d'interface.
                var e = ExportateurPdf.Exporter(nom + ".html", nom + ".pdf");
                return (e is null ? nom + ".pdf" : null, e);
            });
        }
        catch (Exception ex)
        {
            erreur = ex.Message;
        }

        if (fichier is not null)
        {
            var ouvrir = new Button { Content = "Ouvrir" };
            var chemin = fichier;
            ouvrir.Click += (_, _) => Process.Start(new ProcessStartInfo(chemin) { UseShellExecute = true });
            _message = Ui.Message(InfoBarSeverity.Success, pdf ? "Rapport PDF enregistré" : "Rapport HTML enregistré", Path.GetFileName(fichier), ouvrir);
        }
        else
        {
            _message = Ui.Message(InfoBarSeverity.Error, "Export impossible", erreur);
        }

        Afficher();
    }

    private static void OuvrirDossier()
    {
        Directory.CreateDirectory(DossierRapports);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{DossierRapports}\"") { UseShellExecute = false });
    }
}
