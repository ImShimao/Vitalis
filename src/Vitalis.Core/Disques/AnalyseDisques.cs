using Vitalis.Core.Materiel;
using static Vitalis.Core.Materiel.Conversions;

namespace Vitalis.Core.Disques;

/// <summary>
/// Santé des disques et espace libre. Sources (vérifiées dans la doc Microsoft, 2026-11-20) :
/// MSFT_PhysicalDisk, MSFT_StorageReliabilityCounter (root\Microsoft\Windows\Storage), Win32_LogicalDisk (root\CIMV2).
/// Les compteurs sont joints aux disques par DeviceId.
/// </summary>
public sealed class AnalyseDisques
{
    private const string Stockage = @"root\Microsoft\Windows\Storage";
    private const string CimV2 = @"root\CIMV2";

    /// <summary>Seuils d'espace libre : sous 20 % = attention, sous 10 % = critique.</summary>
    public const double SeuilAttention = 20.0;
    public const double SeuilCritique = 10.0;

    private readonly ISourceWmi _source;

    public AnalyseDisques(ISourceWmi source)
    {
        _source = source;
    }

    public async Task<RapportDisques> AnalyserAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var disques = Task.Run(() => LireDisques(), ct);
        var lecteurs = Task.Run(() => LireLecteurs(), ct);

        await Task.WhenAll(disques, lecteurs).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();

        return new RapportDisques(disques.Result.Section, lecteurs.Result, disques.Result.SmartComplet, disques.Result.RaisonSmart);
    }

    /// <summary>Résultat intermédiaire : la section des disques et l'état des compteurs SMART.</summary>
    private sealed record LectureDisques(Section<DisqueSante> Section, bool SmartComplet, string? RaisonSmart);

    private LectureDisques LireDisques()
    {
        Section<DisqueSante> section;
        try
        {
            var lignes = _source.Requeter(Stockage, "MSFT_PhysicalDisk",
                ["DeviceId", "FriendlyName", "MediaType", "HealthStatus", "Size"]);

            var elements = lignes.Select(l => new DisqueSante(
                Identifiant: Texte(l, "DeviceId"),
                Nom: Texte(l, "FriendlyName"),
                Type: TypeDe(Entier(l, "MediaType")),
                TailleOctets: Entier(l, "Size"),
                Sante: StatutDe(Entier(l, "HealthStatus")),
                TemperatureC: null,
                TemperatureMaxC: null,
                UsurePourcent: null,
                HeuresMarche: null,
                ErreursLectureTotal: null)).ToList();

            section = elements.Count == 0
                ? new Section<DisqueSante>(Disponibilite.Indisponible, [], "Aucun disque physique renvoyé par MSFT_PhysicalDisk.")
                : elements.All(d => d.Nom is not null)
                    ? new Section<DisqueSante>(Disponibilite.Disponible, elements)
                    : new Section<DisqueSante>(Disponibilite.Partielle, elements, "Certains noms de disque manquent.");
        }
        catch (Exception e)
        {
            return new LectureDisques(
                new Section<DisqueSante>(Disponibilite.Indisponible, [], $"Disques : lecture impossible ({e.GetType().Name} : {e.Message.Trim().TrimEnd('.', ' ')})"),
                SmartComplet: false,
                RaisonSmart: "Disques indisponibles, donc aucun compteur SMART.");
        }

        // Compteurs SMART : souvent refusés sans droits administrateur. En cas d'échec, les disques restent affichés.
        IReadOnlyList<IReadOnlyDictionary<string, object?>> compteurs;
        try
        {
            compteurs = _source.Requeter(Stockage, "MSFT_StorageReliabilityCounter",
                ["DeviceId", "Temperature", "TemperatureMax", "Wear", "PowerOnHours", "ReadErrorsTotal"]);
        }
        catch (Exception e)
        {
            return new LectureDisques(section, SmartComplet: false,
                RaisonSmart: $"Compteurs SMART non lisibles ({e.GetType().Name} : {e.Message.Trim().TrimEnd('.', ' ')}). Droits administrateur requis.");
        }

        var parId = compteurs
            .Where(c => Texte(c, "DeviceId") is not null)
            .GroupBy(c => Texte(c, "DeviceId")!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var enrichis = section.Elements.Select(d =>
        {
            if (d.Identifiant is null || !parId.TryGetValue(d.Identifiant, out var c))
            {
                return d;
            }

            return d with
            {
                TemperatureC = (int?)Entier(c, "Temperature"),
                TemperatureMaxC = (int?)Entier(c, "TemperatureMax"),
                UsurePourcent = (int?)Entier(c, "Wear"),
                HeuresMarche = (int?)Entier(c, "PowerOnHours"),
                ErreursLectureTotal = Entier(c, "ReadErrorsTotal"),
            };
        }).ToList();

        var sansCompteur = enrichis.Count(d => d.TemperatureC is null && d.UsurePourcent is null);
        var raison = sansCompteur == 0 ? null : $"{sansCompteur} disque(s) sans compteurs SMART correspondants (DeviceId).";

        return new LectureDisques(section with { Elements = enrichis }, SmartComplet: sansCompteur == 0, RaisonSmart: raison);
    }

    private Section<LecteurEspace> LireLecteurs()
    {
        try
        {
            var lignes = _source.Requeter(CimV2, "Win32_LogicalDisk", ["DeviceID", "VolumeName", "Size", "FreeSpace"], "DriveType = 3");

            var elements = lignes.Select(l =>
            {
                var taille = Entier(l, "Size");
                var libre = Entier(l, "FreeSpace");
                var pourcent = taille is > 0 && libre is not null ? 100.0 * libre.Value / taille.Value : (double?)null;
                var lettre = Texte(l, "DeviceID") ?? string.Empty;
                return new LecteurEspace(lettre, Texte(l, "VolumeName"), taille, libre, pourcent, StatutEspaceDe(pourcent));
            }).ToList();

            if (elements.Count == 0)
            {
                return new Section<LecteurEspace>(Disponibilite.Indisponible, [], "Aucun lecteur fixe renvoyé par Win32_LogicalDisk.");
            }

            return elements.All(e => e.PourcentLibre is not null)
                ? new Section<LecteurEspace>(Disponibilite.Disponible, elements)
                : new Section<LecteurEspace>(Disponibilite.Partielle, elements, "Taille ou espace libre manquant pour certains lecteurs.");
        }
        catch (Exception e)
        {
            return new Section<LecteurEspace>(Disponibilite.Indisponible, [], $"Lecteurs : lecture impossible ({e.GetType().Name} : {e.Message.Trim().TrimEnd('.', ' ')})");
        }
    }

    /// <summary>HealthStatus : 0 = sain, 1 = attention, 2 = critique, 5 = inconnu (doc MSFT_PhysicalDisk).</summary>
    public static StatutSante StatutDe(long? codeSante) => codeSante switch
    {
        0 => StatutSante.Sain,
        1 => StatutSante.Attention,
        2 => StatutSante.Critique,
        _ => StatutSante.Inconnu,
    };

    /// <summary>MediaType : 3 = HDD, 4 = SSD, 5 = SCM ; 0 ou autre = inconnu (doc MSFT_PhysicalDisk).</summary>
    public static TypeMedia TypeDe(long? codeMedia) => codeMedia switch
    {
        3 => TypeMedia.Hdd,
        4 => TypeMedia.Ssd,
        5 => TypeMedia.Scm,
        _ => TypeMedia.Inconnu,
    };

    public static StatutEspace StatutEspaceDe(double? pourcentLibre)
    {
        if (pourcentLibre is null)
        {
            return StatutEspace.Critique;
        }

        return pourcentLibre < SeuilCritique ? StatutEspace.Critique
            : pourcentLibre < SeuilAttention ? StatutEspace.Attention
            : StatutEspace.Sain;
    }
}
