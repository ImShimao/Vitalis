using System.Globalization;

namespace Vitalis.Core.Materiel;

/// <summary>Conversions des valeurs brutes WMI. Toutes renvoient null si la valeur est absente ou invalide.</summary>
public static class Conversions
{
    public static string? Texte(IReadOnlyDictionary<string, object?> ligne, string cle)
    {
        if (!ligne.TryGetValue(cle, out var valeur) || valeur is null)
        {
            return null;
        }

        var texte = Convert.ToString(valeur, CultureInfo.InvariantCulture)?.Trim();
        return string.IsNullOrEmpty(texte) ? null : texte;
    }

    public static long? Entier(IReadOnlyDictionary<string, object?> ligne, string cle)
    {
        if (!ligne.TryGetValue(cle, out var valeur) || valeur is null)
        {
            return null;
        }

        return valeur switch
        {
            string s => long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var r) ? r : null,
            IConvertible c => Convert.ToInt64(c, CultureInfo.InvariantCulture),
            _ => null,
        };
    }

    /// <summary>
    /// Partie date d'une date CIM_DATETIME (ex. 20240115000000.000000+000) : AAAAMMJJ, sans heure ni fuseau.
    /// Les dates de BIOS et de pilote n'ont pas d'heure significative. Null si mal formée.
    /// </summary>
    public static DateTime? DateCim(string? valeur)
    {
        if (string.IsNullOrWhiteSpace(valeur) || valeur.Length < 8)
        {
            return null;
        }

        return DateTime.TryParseExact(valeur[..8], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    /// <summary>Décode un tableau de codes UTF-16 (propriétés WmiMonitorID) jusqu'au premier zéro.</summary>
    public static string? DecoderUtf16(object? valeur)
    {
        IEnumerable<int>? codes = valeur switch
        {
            ushort[] tableau => tableau.Select(x => (int)x),
            Array tableau => tableau.Cast<object?>().Select(x => x is null ? 0 : Convert.ToInt32(x, CultureInfo.InvariantCulture)),
            _ => null,
        };

        if (codes is null)
        {
            return null;
        }

        var texte = new string(codes.TakeWhile(c => c != 0).Select(c => (char)c).ToArray()).Trim();
        return texte.Length == 0 ? null : texte;
    }

    /// <summary>Somme des capacités des barrettes connues ; null si aucune capacité n'est connue.</summary>
    public static long? MemoireTotaleOctets(IEnumerable<Memoire> modules)
    {
        var capacites = modules.Where(m => m.CapaciteOctets is not null).Select(m => m.CapaciteOctets!.Value).ToList();
        return capacites.Count == 0 ? null : capacites.Sum();
    }
}
