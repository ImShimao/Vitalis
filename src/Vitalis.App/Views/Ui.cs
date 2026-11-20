using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Vitalis.App.Views;

/// <summary>Niveau d'un état affiché. La couleur est toujours accompagnée d'une icône et d'un mot.</summary>
public enum Niveau
{
    Succes,
    Attention,
    Critique,
    Neutre,
}

/// <summary>Briques d'interface partagées par toutes les pages : cartes, badges, textes, états de chargement.</summary>
public static class Ui
{
    public static Brush Pinceau(string cle) => (Brush)Application.Current.Resources[cle];

    public static Style Style(string cle) => (Style)Application.Current.Resources[cle];

    public static TextBlock Texte(string texte, bool secondaire = false, bool gras = false, double? taille = null)
    {
        var t = new TextBlock
        {
            Text = texte,
            TextWrapping = TextWrapping.Wrap,
            FontWeight = gras ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = Pinceau(secondaire ? "VitalisTexteSecondaire" : "VitalisTexte"),
        };
        if (taille is { } s)
        {
            t.FontSize = s;
        }

        return t;
    }

    public static TextBlock Legende(string texte) => new()
    {
        Text = texte,
        TextWrapping = TextWrapping.Wrap,
        Style = Style("CaptionTextBlockStyle"),
        Foreground = Pinceau("VitalisTexteSecondaire"),
    };

    public static TextBlock TitreSection(string texte) => new() { Text = texte, Style = Style("VitalisTitreSection") };

    public static FontIcon Icone(string glyphe, double taille = 16, Brush? couleur = null)
    {
        var i = new FontIcon { Glyph = glyphe, FontSize = taille };
        if (couleur is not null)
        {
            i.Foreground = couleur;
        }

        return i;
    }

    public static Border Carte(UIElement contenu, Thickness? marge = null) => new()
    {
        Style = Style("VitalisCarte"),
        Child = contenu,
        Padding = marge ?? new Thickness(20),
    };

    /// <summary>En-tête de carte : icône, titre, et un élément à droite (badge, bouton).</summary>
    public static Grid EnTeteCarte(string glyphe, string titre, UIElement? droite = null)
    {
        var g = new Grid { ColumnSpacing = 12 };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icone = Icone(glyphe, 20, Pinceau("AccentTextFillColorPrimaryBrush"));
        icone.VerticalAlignment = VerticalAlignment.Center;
        var texte = Texte(titre, gras: true);
        texte.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(texte, 1);
        g.Children.Add(icone);
        g.Children.Add(texte);

        if (droite is FrameworkElement fe)
        {
            fe.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(fe, 2);
            g.Children.Add(fe);
        }

        return g;
    }

    /// <summary>Pastille d'état : icône + mot, sur un fond teinté.</summary>
    public static Border Badge(Niveau niveau, string texte)
    {
        var (glyphe, couleur, fond) = niveau switch
        {
            Niveau.Succes => ("\uE73E", "VitalisSucces", "VitalisSuccesFond"),
            Niveau.Attention => ("\uE7BA", "VitalisAttention", "VitalisAttentionFond"),
            Niveau.Critique => ("\uEA39", "VitalisCritique", "VitalisCritiqueFond"),
            _ => ("\uE946", "VitalisTexte", "VitalisNeutreFond"),
        };

        var contenu = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        contenu.Children.Add(Icone(glyphe, 12, Pinceau(couleur)));
        contenu.Children.Add(new TextBlock
        {
            Text = texte,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = Pinceau(couleur),
            VerticalAlignment = VerticalAlignment.Center,
        });

        return new Border
        {
            Background = Pinceau(fond),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 2, 10, 3),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = contenu,
        };
    }

    /// <summary>Grand chiffre avec son libellé, pour les cartes de synthèse.</summary>
    public static StackPanel Statistique(string valeur, string libelle, string? glyphe = null)
    {
        var p = new StackPanel { Spacing = 2 };
        var ligne = new Grid { ColumnSpacing = 10 };
        ligne.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        ligne.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        if (glyphe is not null)
        {
            var i = Icone(glyphe, 20, Pinceau("AccentTextFillColorPrimaryBrush"));
            i.VerticalAlignment = VerticalAlignment.Top;
            i.Margin = new Thickness(0, 6, 0, 0);
            ligne.Children.Add(i);
        }

        ligne.Children.Add(new TextBlock { Text = valeur, Style = Style("SubtitleTextBlockStyle"), Foreground = Pinceau("VitalisTexte"), TextWrapping = TextWrapping.Wrap, MaxLines = 2 });
        Grid.SetColumn(ligne.Children[^1] as FrameworkElement, 1);
        p.Children.Add(ligne);
        p.Children.Add(Legende(libelle));
        return p;
    }

    /// <summary>Paire « libellé : valeur » sur deux colonnes, pour les fiches matériel.</summary>
    public static Grid Proprietes(IEnumerable<(string Cle, string? Valeur)> lignes)
    {
        var g = new Grid { ColumnSpacing = 24, RowSpacing = 8 };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var i = 0;
        foreach (var (cle, valeur) in lignes)
        {
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var c = Texte(cle, secondaire: true);
            var v = Texte(valeur ?? "non disponible", secondaire: valeur is null);
            v.IsTextSelectionEnabled = true;
            Grid.SetRow(c, i);
            Grid.SetRow(v, i);
            Grid.SetColumn(v, 1);
            g.Children.Add(c);
            g.Children.Add(v);
            i++;
        }

        return g;
    }

    /// <summary>Liste de lignes séparées par un trait fin, dans une seule carte.</summary>
    public static Border ListeCarte(IEnumerable<UIElement> lignes)
    {
        var p = new StackPanel();
        var premiere = true;
        foreach (var l in lignes)
        {
            if (!premiere)
            {
                p.Children.Add(new Border { Height = 1, Background = Pinceau("DividerStrokeColorDefaultBrush"), Margin = new Thickness(0, 12, 0, 12) });
            }

            p.Children.Add(l);
            premiere = false;
        }

        return Carte(p, new Thickness(20, 16, 20, 16));
    }

    /// <summary>Ligne de liste : texte principal, texte secondaire, et un élément à droite.</summary>
    public static Grid Ligne(string titre, string? detail, UIElement? droite = null, UIElement? gauche = null)
    {
        var g = new Grid { ColumnSpacing = 16 };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        if (gauche is FrameworkElement fg)
        {
            fg.VerticalAlignment = VerticalAlignment.Center;
            g.Children.Add(fg);
        }

        var texte = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        texte.Children.Add(Texte(titre));
        if (!string.IsNullOrWhiteSpace(detail))
        {
            texte.Children.Add(Legende(detail));
        }

        Grid.SetColumn(texte, 1);
        g.Children.Add(texte);

        if (droite is FrameworkElement fd)
        {
            fd.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(fd, 2);
            g.Children.Add(fd);
        }

        return g;
    }

    /// <summary>Indicateur de chargement avec une phrase qui dit ce qui se passe et combien de temps cela prend.</summary>
    public static Grid Chargement(string message)
    {
        // Grid et non StackPanel horizontal : le message revient à la ligne dans une fenêtre étroite.
        var p = new Grid { ColumnSpacing = 12, Margin = new Thickness(0, 8, 0, 8) };
        p.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        p.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        p.Children.Add(new ProgressRing { IsActive = true, Width = 20, Height = 20 });
        var t = Texte(message, secondaire: true);
        t.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(t, 1);
        p.Children.Add(t);
        return p;
    }

    /// <summary>État vide ou à venir : icône, titre, explication, bouton optionnel.</summary>
    public static Border Vide(string glyphe, string titre, string message, UIElement? action = null)
    {
        var p = new StackPanel { Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center, MaxWidth = 520, Margin = new Thickness(0, 24, 0, 24) };
        var i = Icone(glyphe, 40, Pinceau("AccentTextFillColorPrimaryBrush"));
        i.HorizontalAlignment = HorizontalAlignment.Center;
        p.Children.Add(i);
        var t = new TextBlock { Text = titre, Style = Style("SubtitleTextBlockStyle"), Foreground = Pinceau("VitalisTexte"), HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center };
        p.Children.Add(t);
        var m = Texte(message, secondaire: true);
        m.TextAlignment = TextAlignment.Center;
        p.Children.Add(m);
        if (action is FrameworkElement fa)
        {
            fa.HorizontalAlignment = HorizontalAlignment.Center;
            fa.Margin = new Thickness(0, 8, 0, 0);
            p.Children.Add(fa);
        }

        return Carte(p);
    }

    /// <summary>Message d'information ou d'erreur, non fermable, toujours lisible.</summary>
    public static InfoBar Message(InfoBarSeverity gravite, string titre, string? message = null, UIElement? action = null) => new()
    {
        Severity = gravite,
        Title = titre,
        Message = message ?? string.Empty,
        IsOpen = true,
        IsClosable = false,
        ActionButton = action as ButtonBase,
    };

    /// <summary>
    /// Chargement standard d'une page : résultat de la session s'il existe (sauf « Actualiser »),
    /// sinon indicateur de chargement, analyse, puis affichage ; une erreur devient un message lisible.
    /// </summary>
    public static async Task ChargerAsync<T>(
        Chargeur<T> etat,
        bool forcer,
        StackPanel corps,
        Control? bouton,
        string messageChargement,
        Func<CancellationToken, Task<T>> analyse,
        Action<T> afficher) where T : class
    {
        if (!forcer && !etat.EnCours && etat.Valeur is { } existant)
        {
            AfficherAvecDate(corps, existant, etat.Date, afficher);
            return;
        }

        if (bouton is not null)
        {
            bouton.IsEnabled = false;
        }

        corps.Children.Clear();
        corps.Children.Add(Chargement(messageChargement));
        try
        {
            // Le tableau de bord est déjà en train de lire ces données : on attend son résultat au lieu de relancer.
            if (!forcer && etat.RempliParTableau && EtatAnalyse.Tableau.Tache is { IsCompleted: false } tableau)
            {
                try
                {
                    await tableau;
                }
                catch (Exception)
                {
                    // Échec du tableau de bord : la page lance sa propre analyse ci-dessous.
                }

                if (etat.Valeur is { } fourni)
                {
                    AfficherAvecDate(corps, fourni, etat.Date, afficher);
                    return;
                }
            }

            var r = await etat.Obtenir((_, ct) => analyse(ct));
            AfficherAvecDate(corps, r, etat.Date, afficher);
        }
        catch (Exception e)
        {
            corps.Children.Clear();
            corps.Children.Add(Message(InfoBarSeverity.Error, "Analyse impossible", e.Message));
        }
        finally
        {
            if (bouton is not null)
            {
                bouton.IsEnabled = true;
            }
        }
    }

    private static void AfficherAvecDate<T>(StackPanel corps, T valeur, DateTime? date, Action<T> afficher)
    {
        corps.Children.Clear();
        afficher(valeur);
        if (date is { } d)
        {
            corps.Children.Insert(0, Legende($"Analysé {Quand(d)}. « Actualiser » relance la lecture."));
        }
    }

    public static Button BoutonAccent(string texte, string? glyphe = null)
    {
        var b = Bouton(texte, glyphe);
        b.Style = Style("AccentButtonStyle");
        return b;
    }

    public static Button Bouton(string texte, string? glyphe = null)
    {
        if (glyphe is null)
        {
            return new Button { Content = texte };
        }

        var p = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        p.Children.Add(Icone(glyphe, 14));
        p.Children.Add(new TextBlock { Text = texte });
        var b = new Button { Content = p };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(b, texte);
        return b;
    }

    /// <summary>« 0 programme », « 1 programme », « 3 programmes ».</summary>
    public static string Pluriel(int n, string singulier, string? pluriel = null) =>
        $"{n} {(n > 1 ? pluriel ?? singulier + "s" : singulier)}";

    public static string Gio(long? octets) => octets is null
        ? "n/d"
        : octets.Value >= 1024L * 1024 * 1024 * 1024
            ? (octets.Value / 1024.0 / 1024 / 1024 / 1024).ToString("0.0#", System.Globalization.CultureInfo.CurrentCulture) + " Tio"
            : (octets.Value / 1024.0 / 1024 / 1024).ToString("0.0", System.Globalization.CultureInfo.CurrentCulture) + " Gio";

    /// <summary>« il y a 5 min », « aujourd'hui à 14:02 », « le 3 novembre à 09:10 ».</summary>
    public static string Quand(DateTime date)
    {
        var ecart = DateTime.Now - date;
        if (ecart < TimeSpan.FromMinutes(1))
        {
            return "à l'instant";
        }

        if (ecart < TimeSpan.FromHours(1))
        {
            return $"il y a {(int)ecart.TotalMinutes} min";
        }

        return date.Date == DateTime.Today
            ? $"aujourd'hui à {date:HH:mm}"
            : $"le {date.ToString("d MMMM", System.Globalization.CultureInfo.CurrentCulture)} à {date:HH:mm}";
    }
}

/// <summary>
/// Grille adaptative : autant de colonnes que la largeur le permet (largeur minimale par colonne),
/// chaque ligne prend la hauteur de son élément le plus haut. Remplace UniformGrid, absent de WinUI 3.
/// </summary>
public sealed class GrilleAdaptative : Panel
{
    public double LargeurMinimale { get; set; } = 280;

    public double Espacement { get; set; } = 12;

    private int Colonnes(double largeur) =>
        Math.Max(1, Math.Min(Math.Max(1, Children.Count),
            double.IsInfinity(largeur) ? 2 : (int)((largeur + Espacement) / (LargeurMinimale + Espacement))));

    private double LargeurColonne(double largeur, int colonnes) =>
        Math.Max(0, (largeur - Espacement * (colonnes - 1)) / colonnes);

    protected override Size MeasureOverride(Size disponible)
    {
        var largeur = double.IsInfinity(disponible.Width) ? LargeurMinimale * 2 + Espacement : disponible.Width;
        var colonnes = Colonnes(largeur);
        var lc = LargeurColonne(largeur, colonnes);
        double hauteur = 0, hauteurLigne = 0;

        for (var i = 0; i < Children.Count; i++)
        {
            Children[i].Measure(new Size(lc, double.PositiveInfinity));
            hauteurLigne = Math.Max(hauteurLigne, Children[i].DesiredSize.Height);
            if (i % colonnes == colonnes - 1 || i == Children.Count - 1)
            {
                hauteur += hauteurLigne + (i == Children.Count - 1 ? 0 : Espacement);
                hauteurLigne = 0;
            }
        }

        return new Size(largeur, hauteur);
    }

    protected override Size ArrangeOverride(Size finale)
    {
        var colonnes = Colonnes(finale.Width);
        var lc = LargeurColonne(finale.Width, colonnes);
        double y = 0;

        for (var debut = 0; debut < Children.Count; debut += colonnes)
        {
            var fin = Math.Min(debut + colonnes, Children.Count);
            double h = 0;
            for (var i = debut; i < fin; i++)
            {
                h = Math.Max(h, Children[i].DesiredSize.Height);
            }

            for (var i = debut; i < fin; i++)
            {
                Children[i].Arrange(new Rect((i - debut) * (lc + Espacement), y, lc, h));
            }

            y += h + Espacement;
        }

        return finale;
    }
}
