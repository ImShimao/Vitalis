using System.Globalization;

namespace Vitalis.Core.Accessibilite;

/// <summary>Calcul du rapport de contraste WCAG 2.x entre deux couleurs hexadécimales.</summary>
public static class Contraste
{
    /// <summary>Seuil WCAG AA pour le texte normal.</summary>
    public const double SeuilAA = 4.5;

    /// <summary>Rapport de contraste entre 1 (identique) et 21 (noir sur blanc).</summary>
    /// <param name="couleurA">Couleur au format #RRGGBB ou #AARRGGBB (l'alpha est ignoré).</param>
    /// <param name="couleurB">Idem.</param>
    public static double Rapport(string couleurA, string couleurB)
    {
        var la = Luminance(couleurA);
        var lb = Luminance(couleurB);
        var clair = Math.Max(la, lb);
        var sombre = Math.Min(la, lb);
        return (clair + 0.05) / (sombre + 0.05);
    }

    /// <summary>Luminance relative selon la formule WCAG 2.x.</summary>
    public static double Luminance(string couleur)
    {
        var (r, g, b) = Rgb(couleur);
        return 0.2126 * Lineariser(r) + 0.7152 * Lineariser(g) + 0.0722 * Lineariser(b);
    }

    private static double Lineariser(double canal)
    {
        return canal <= 0.03928 ? canal / 12.92 : Math.Pow((canal + 0.055) / 1.055, 2.4);
    }

    private static (double R, double G, double B) Rgb(string couleur)
    {
        var hex = couleur.TrimStart('#');
        if (hex.Length == 8)
        {
            hex = hex[2..];
        }

        if (hex.Length != 6 || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            throw new ArgumentException($"Couleur invalide : « {couleur} ». Format attendu : #RRGGBB ou #AARRGGBB.", nameof(couleur));
        }

        double Canal(int debut) => int.Parse(hex.Substring(debut, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
        return (Canal(0), Canal(2), Canal(4));
    }
}
