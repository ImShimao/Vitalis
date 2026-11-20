using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vitalis.Core.Disques;
using Vitalis.Core.Materiel;
using Vitalis.Core.MisesAJour;
using Vitalis.Core.TableauDeBord;

namespace Vitalis.App.Views;

/// <summary>Page « Tableau de bord » : score global, détail du score, cartes de synthèse cliquables.</summary>
public sealed partial class TableauDeBordPage : Page
{
    private static Chargeur<ResultatTableauDeBord> Etat => EtatAnalyse.Tableau;

    private ProgressBar? _barre;
    private TextBlock? _texteProgression;
    private string? _erreur;

    public TableauDeBordPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            Etat.ProgressionChangee += SurProgression;
            Afficher();
            if (Etat.EnCours)
            {
                await AttendreAsync();
            }
        };
        Unloaded += (_, _) => Etat.ProgressionChangee -= SurProgression;
    }

    private void SurProgression(int v)
    {
        if (_barre is not null && _texteProgression is not null)
        {
            _barre.Value = v;
            _texteProgression.Text = TexteProgression(v);
        }
    }

    private static string TexteProgression(int v) =>
        $"Analyse en cours… {v} %  ·  la recherche Windows Update prend environ 30 secondes. Rien n'est modifié.";

    private async void BoutonAnalyser_Click(object sender, RoutedEventArgs e)
    {
        _erreur = null;
        _ = Etat.Obtenir(async (progres, ct) =>
        {
            var r = await new TableauDeBordService(
                    new InventaireMateriel(new WmiSource()),
                    new AnalyseDisques(new WmiSource()),
                    new AnalyseMisesAJour(new WuaCom(), new WingetCli()))
                .AnalyserAsync(progres, ct);
            EtatAnalyse.Repartir(r);
            EnregistrerDerniereAnalyse(r.Date);
            return r;
        });
        await AttendreAsync();
    }

    private async Task AttendreAsync()
    {
        Afficher();
        try
        {
            if (Etat.Tache is { } tache)
            {
                await tache;
            }
        }
        catch (OperationCanceledException)
        {
            _erreur = "Analyse annulée.";
        }
        catch (Exception ex)
        {
            _erreur = $"Analyse impossible : {ex.Message}";
        }

        Afficher();
    }

    private void BoutonAnnuler_Click(object sender, RoutedEventArgs e)
    {
        // Les appels COM de Windows Update ne s'interrompent pas en cours d'appel : l'annulation attend la fin de l'appel.
        Etat.Annuler();
        BoutonAnnulerTexte.Text = "Annulation…";
        BoutonAnnuler.IsEnabled = false;
    }

    private void Afficher()
    {
        var enCours = Etat.EnCours;
        BoutonAnalyser.IsEnabled = !enCours;
        BoutonAnalyserTexte.Text = Etat.Valeur is null ? "Analyser maintenant" : "Analyser à nouveau";
        BoutonAnnuler.Visibility = enCours ? Visibility.Visible : Visibility.Collapsed;
        if (enCours)
        {
            BoutonAnnuler.IsEnabled = true;
            BoutonAnnulerTexte.Text = "Annuler";
        }

        Corps.Children.Clear();
        Corps.Children.Add(Hero(Etat.Valeur, enCours));

        if (_erreur is not null && !enCours)
        {
            Corps.Children.Add(Ui.Message(InfoBarSeverity.Warning, _erreur));
        }

        if (Etat.Valeur is not { } r)
        {
            return;
        }

        Corps.Children.Add(Ui.TitreSection("Synthèse"));
        var grille = new GrilleAdaptative { LargeurMinimale = 300 };
        foreach (var c in r.Cartes)
        {
            grille.Children.Add(Carte(c, r));
        }

        Corps.Children.Add(grille);
        Corps.Children.Add(Ui.TitreSection("Détail du score"));
        Corps.Children.Add(DetailScore(r));
    }

    private UIElement Hero(ResultatTableauDeBord? r, bool enCours)
    {
        var score = r?.Score;
        var niveau = score switch
        {
            null => Niveau.Neutre,
            >= 80 => Niveau.Succes,
            >= 50 => Niveau.Attention,
            _ => Niveau.Critique,
        };

        var anneau = new Grid { Width = 128, Height = 128 };
        anneau.Children.Add(new ProgressRing
        {
            IsIndeterminate = enCours && score is null,
            IsActive = true,
            Value = score ?? 0,
            Width = 128,
            Height = 128,
            Foreground = Ui.Pinceau(niveau switch
            {
                Niveau.Succes => "VitalisSucces",
                Niveau.Attention => "VitalisAttention",
                Niveau.Critique => "VitalisCritique",
                _ => "AccentFillColorDefaultBrush",
            }),
            Background = Ui.Pinceau("VitalisNeutreFond"),
        });
        var chiffre = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        chiffre.Children.Add(new TextBlock
        {
            Text = score?.ToString(CultureInfo.CurrentCulture) ?? "—",
            Style = Ui.Style("TitleLargeTextBlockStyle"),
            Foreground = Ui.Pinceau("VitalisTexte"),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        chiffre.Children.Add(new TextBlock { Text = "sur 100", Style = Ui.Style("CaptionTextBlockStyle"), Foreground = Ui.Pinceau("VitalisTexteSecondaire"), HorizontalAlignment = HorizontalAlignment.Center });
        anneau.Children.Add(chiffre);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(anneau, score is null ? "Score indisponible" : $"Score {score} sur 100");

        var texte = new StackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        var (titre, message) = (r, score) switch
        {
            (null, _) when enCours => ("Analyse en cours", "Matériel, disques et mises à jour sont lus en parallèle."),
            (null, _) => ("Aucune analyse pour l'instant", "Lancez une analyse : environ 30 secondes. Vitalis lit l'état du PC et ne modifie rien."),
            (_, null) => ("Score indisponible", "Une partie de l'analyse n'a pas abouti : voyez les cartes ci-dessous."),
            (_, >= 80) => ("Votre PC est en bon état", "Rien d'urgent. Consultez les cartes pour le détail."),
            (_, >= 50) => ("Quelques points à surveiller", "Certains éléments méritent votre attention : voyez les cartes ci-dessous."),
            _ => ("Des problèmes demandent votre attention", "Commencez par les cartes marquées « Critique »."),
        };

        texte.Children.Add(new TextBlock { Text = titre, Style = Ui.Style("SubtitleTextBlockStyle"), Foreground = Ui.Pinceau("VitalisTexte"), TextWrapping = TextWrapping.Wrap });
        texte.Children.Add(Ui.Texte(message, secondaire: true));

        if (enCours)
        {
            _barre = new ProgressBar { Minimum = 0, Maximum = 100, Value = Etat.Progression, Margin = new Thickness(0, 8, 0, 0) };
            _texteProgression = Ui.Legende(TexteProgression(Etat.Progression));
            texte.Children.Add(_barre);
            texte.Children.Add(_texteProgression);
        }
        else
        {
            _barre = null;
            _texteProgression = null;
            var date = r?.Date ?? LireDerniereAnalyse();
            if (date is not null)
            {
                texte.Children.Add(Ui.Legende(r is null
                    ? $"Dernière analyse (session précédente) : {date.Value.ToString("g", CultureInfo.CurrentCulture)}"
                    : $"Analysé {Ui.Quand(date.Value)}"));
            }

            if (r is not null)
            {
                var rapport = new HyperlinkButton { Content = "Exporter un rapport HTML ou PDF", Padding = new Thickness(0, 4, 0, 0) };
                rapport.Click += (_, _) => App.Naviguer("rapport");
                texte.Children.Add(rapport);
            }
        }

        var g = new Grid { ColumnSpacing = 32 };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(texte, 1);
        g.Children.Add(anneau);
        g.Children.Add(texte);
        return Ui.Carte(g, new Thickness(28));
    }

    private static readonly Dictionary<string, (string Page, string Glyphe)> Destinations = new()
    {
        ["Matériel"] = ("materiel", "\uE950"),
        ["Disques"] = ("disques", "\uEDA2"),
        ["Espace libre"] = ("disques", "\uE7B8"),
        ["Mises à jour"] = ("mises-a-jour", "\uE895"),
    };

    private static UIElement Carte(CarteSynthese c, ResultatTableauDeBord r)
    {
        var (page, glyphe) = Destinations.GetValueOrDefault(c.Titre, ("tableau-de-bord", "\uE9D9"));
        var p = new StackPanel { Spacing = 10 };
        p.Children.Add(Ui.EnTeteCarte(glyphe, c.Titre, BadgeCarte(c, r)));

        if (c.Etat == Disponibilite.Indisponible)
        {
            p.Children.Add(Ui.Texte(c.Raison ?? "Indisponible.", secondaire: true));
        }
        else
        {
            if (c.Valeur is not null)
            {
                p.Children.Add(new TextBlock { Text = c.Valeur, Style = Ui.Style("BodyStrongTextBlockStyle"), Foreground = Ui.Pinceau("VitalisTexte"), TextWrapping = TextWrapping.Wrap });
            }

            if (c.Detail is not null)
            {
                p.Children.Add(Ui.Texte(c.Detail, secondaire: true));
            }

            if (c.Etat == Disponibilite.Partielle && c.Raison is not null)
            {
                p.Children.Add(Ui.Legende(c.Raison));
            }
        }

        var lien = new HyperlinkButton { Content = "Voir le détail", Padding = new Thickness(0) };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(lien, $"Voir le détail : {c.Titre}");
        lien.Click += (_, _) => App.Naviguer(page);
        p.Children.Add(lien);
        return Ui.Carte(p);
    }

    private static Border BadgeCarte(CarteSynthese c, ResultatTableauDeBord r)
    {
        if (c.Etat == Disponibilite.Indisponible)
        {
            return Ui.Badge(Niveau.Neutre, "Indisponible");
        }

        switch (c.Titre)
        {
            case "Disques" when r.Disques is { } d && d.Disques.Elements.Count > 0:
                var pire = d.Disques.Elements.Select(x => x.Sante).Min(x => x switch
                {
                    StatutSante.Critique => 0,
                    StatutSante.Attention => 1,
                    StatutSante.Sain => 2,
                    _ => 3,
                });
                return pire switch
                {
                    0 => Ui.Badge(Niveau.Critique, "Critique"),
                    1 => Ui.Badge(Niveau.Attention, "Attention"),
                    2 => Ui.Badge(Niveau.Succes, "Sains"),
                    _ => Ui.Badge(Niveau.Neutre, "État inconnu"),
                };
            case "Espace libre" when r.Disques is { } d && d.Lecteurs.Elements.Count > 0:
                var statut = d.Lecteurs.Elements.Min(l => l.Statut switch
                {
                    StatutEspace.Critique => 0,
                    StatutEspace.Attention => 1,
                    _ => 2,
                });
                return statut switch
                {
                    0 => Ui.Badge(Niveau.Critique, "Presque plein"),
                    1 => Ui.Badge(Niveau.Attention, "À surveiller"),
                    _ => Ui.Badge(Niveau.Succes, "Suffisant"),
                };
            case "Mises à jour" when r.MisesAJour is { } m:
                return m.EnAttente.Elements.Count == 0
                    ? Ui.Badge(Niveau.Succes, "Windows à jour")
                    : Ui.Badge(Niveau.Attention, "En attente");
        }

        return c.Etat == Disponibilite.Partielle ? Ui.Badge(Niveau.Attention, "Partiel") : Ui.Badge(Niveau.Succes, "Lu");
    }

    private static UIElement DetailScore(ResultatTableauDeBord r)
    {
        var lignes = new List<UIElement>();
        foreach (var c in r.Composants)
        {
            var droite = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            if (c.Valeur is { } v)
            {
                droite.Children.Add(new ProgressBar { Minimum = 0, Maximum = 100, Value = v, Width = 160, VerticalAlignment = VerticalAlignment.Center });
                droite.Children.Add(new TextBlock { Text = $"{v} / 100", Width = 64, TextAlignment = TextAlignment.Right, Foreground = Ui.Pinceau("VitalisTexte") });
            }
            else
            {
                droite.Children.Add(Ui.Badge(Niveau.Neutre, "Non évalué"));
            }

            lignes.Add(Ui.Ligne(c.Nom, $"Poids dans le score : {c.Poids} %", droite));
        }

        lignes.Add(Ui.Legende("Le matériel n'entre pas dans le score : il n'indique pas un problème à corriger. Formule : voir ADR-008."));
        return Ui.ListeCarte(lignes);
    }

    private static DateTime? LireDerniereAnalyse()
    {
        try
        {
            var fichier = FichierDerniereAnalyse();
            return File.Exists(fichier) && DateTime.TryParse(File.ReadAllText(fichier), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d)
                ? d
                : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static void EnregistrerDerniereAnalyse(DateTime date)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FichierDerniereAnalyse())!);
            File.WriteAllText(FichierDerniereAnalyse(), date.ToString("o", CultureInfo.InvariantCulture));
        }
        catch (IOException)
        {
            // La date de dernière analyse est un confort : son échec ne doit pas bloquer l'affichage.
        }
    }

    private static string FichierDerniereAnalyse() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vitalis", "derniere-analyse.txt");
}
