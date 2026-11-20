using System.Globalization;
using Vitalis.Core.Materiel;

namespace Vitalis.Core.Pilotes;

/// <summary>Un pilote installé (Win32_PnPSignedDriver).</summary>
public sealed record Pilote(
    string Appareil,
    string? Fabricant,
    string? Version,
    DateTime? Date,
    string? Classe,
    bool Ancien,
    LienFabricant? Lien);

public sealed record RapportPilotes(Section<Pilote> Pilotes, int NombreAnciens);

/// <summary>
/// Liste des pilotes et règle des deux ans : un pilote daté de plus de deux ans est « ancien » (à vérifier).
/// Lecture seule : aucun pilote n'est installé ni téléchargé.
/// Source : Win32_PnPSignedDriver (root\CIMV2), doc Microsoft vérifiée le 2026-11-20.
/// </summary>
public sealed class AnalysePilotes
{
    private const string CimV2 = @"root\CIMV2";

    private readonly ISourceWmi _source;

    public AnalysePilotes(ISourceWmi source)
    {
        _source = source;
    }

    public RapportPilotes Analyser(DateTime maintenant)
    {
        try
        {
            var lignes = _source.Requeter(CimV2, "Win32_PnPSignedDriver",
                ["DeviceName", "Manufacturer", "DriverVersion", "DriverDate", "DeviceClass"]);

            var pilotes = lignes
                .Select(l => Convertir(l, maintenant))
                .Where(p => p is not null)
                .Select(p => p!)
                .OrderBy(p => p.Appareil, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            var anciens = pilotes.Count(p => p.Ancien);
            return pilotes.Count == 0
                ? new RapportPilotes(new Section<Pilote>(Disponibilite.Indisponible, [], "Aucun pilote renvoyé par Win32_PnPSignedDriver."), 0)
                : new RapportPilotes(new Section<Pilote>(Disponibilite.Disponible, pilotes), anciens);
        }
        catch (Exception e)
        {
            return new RapportPilotes(
                new Section<Pilote>(Disponibilite.Indisponible, [], $"Pilotes : lecture impossible ({e.GetType().Name} : {e.Message.Trim().TrimEnd('.', ' ')})"),
                0);
        }
    }

    private static Pilote? Convertir(IReadOnlyDictionary<string, object?> l, DateTime maintenant)
    {
        var appareil = Conversions.Texte(l, "DeviceName");
        if (appareil is null)
        {
            return null;
        }

        var fabricant = Conversions.Texte(l, "Manufacturer");
        var date = DateDriver(Conversions.Texte(l, "DriverDate"));
        return new Pilote(
            appareil,
            fabricant,
            Conversions.Texte(l, "DriverVersion"),
            date,
            Conversions.Texte(l, "DeviceClass"),
            EstAncien(date, maintenant),
            LiensFabricants.Trouver(fabricant));
    }

    /// <summary>Accepte le format CIM (AAAAMMJJ…) et le format « AAAA-MM-JJ HH:MM:SS » renvoyé par certains systèmes.</summary>
    public static DateTime? DateDriver(string? valeur)
    {
        if (string.IsNullOrWhiteSpace(valeur))
        {
            return null;
        }

        var cim = Conversions.DateCim(valeur);
        if (cim is not null)
        {
            return cim;
        }

        return DateTime.TryParse(valeur, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d.Date : null;
    }

    /// <summary>Règle des deux ans : ancien si la date est antérieure à maintenant moins deux ans. Sans date : non déterminé (faux).</summary>
    public static bool EstAncien(DateTime? date, DateTime maintenant) =>
        date is not null && date.Value < maintenant.AddYears(-2);
}
