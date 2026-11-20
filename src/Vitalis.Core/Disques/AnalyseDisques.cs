using System.Text.RegularExpressions;
using Vitalis.Core.Materiel;
using static Vitalis.Core.Materiel.Conversions;

namespace Vitalis.Core.Disques;

/// <summary>
/// Santé des disques et espace libre. Sources (vérifiées dans la doc Microsoft, 2026-11-20) :
/// MSFT_PhysicalDisk, MSFT_StorageReliabilityCounter, MSFT_PhysicalDiskToStorageReliabilityCounter
/// (root\Microsoft\Windows\Storage), Win32_LogicalDisk (root\CIMV2).
/// Jointure des compteurs : d'abord par l'association MSFT_PhysicalDiskToStorageReliabilityCounter
/// (GUID d'ObjectId), puis, à défaut, par DeviceId. Le journal optionnel trace les valeurs brutes.
/// </summary>
public sealed class AnalyseDisques
{
    private const string Stockage = @"root\Microsoft\Windows\Storage";
    private const string CimV2 = @"root\CIMV2";

    private static readonly Regex GuidRegex = new(
        @"\{[0-9a-fA-F]{8}-(?:[0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}\}", RegexOptions.Compiled);

    /// <summary>Seuils d'espace libre : sous 20 % = attention, sous 10 % = critique.</summary>
    public const double SeuilAttention = 20.0;
    public const double SeuilCritique = 10.0;

    private readonly ISourceWmi _source;
    private readonly Action<string>? _journal;

    /// <param name="source">Source WMI.</param>
    /// <param name="journal">Reçoit des lignes de diagnostic (valeurs brutes). Null = aucun journal.</param>
    public AnalyseDisques(ISourceWmi source, Action<string>? journal = null)
    {
        _source = source;
        _journal = journal;
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
                ["DeviceId", "ObjectId", "FriendlyName", "MediaType", "HealthStatus", "Size"]);

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

            _journal?.Invoke($"MSFT_PhysicalDisk : {elements.Count} disque(s)");
            foreach (var (ligne, d) in lignes.Zip(elements))
            {
                _journal?.Invoke($"  disque DeviceId={d.Identifiant} GUID={DernierGuid(Texte(ligne, "ObjectId")) ?? "absent"} nom={d.Nom}");
            }

            section = elements.Count == 0
                ? new Section<DisqueSante>(Disponibilite.Indisponible, [], "Aucun disque physique renvoyé par MSFT_PhysicalDisk.")
                : elements.All(d => d.Nom is not null)
                    ? new Section<DisqueSante>(Disponibilite.Disponible, elements)
                    : new Section<DisqueSante>(Disponibilite.Partielle, elements, "Certains noms de disque manquent.");

            // Garde les ObjectId pour la jointure (non exposés dans le modèle).
            _objetsDisques = lignes.Select(l => Texte(l, "ObjectId")).ToList();
        }
        catch (Exception e)
        {
            return new LectureDisques(
                new Section<DisqueSante>(Disponibilite.Indisponible, [], $"Disques : lecture impossible ({e.GetType().Name} : {e.Message.Trim().TrimEnd('.', ' ')})"),
                SmartComplet: false,
                RaisonSmart: "Disques indisponibles, donc aucun compteur SMART.");
        }

        // Compteurs SMART : refusés sans droits administrateur. En cas d'échec, les disques restent affichés.
        IReadOnlyList<IReadOnlyDictionary<string, object?>> compteurs;
        try
        {
            compteurs = _source.Requeter(Stockage, "MSFT_StorageReliabilityCounter",
                ["DeviceId", "ObjectId", "Temperature", "TemperatureMax", "Wear", "PowerOnHours", "ReadErrorsTotal"]);
        }
        catch (Exception e)
        {
            return new LectureDisques(section, SmartComplet: false,
                RaisonSmart: $"Compteurs SMART non lisibles ({e.GetType().Name} : {e.Message.Trim().TrimEnd('.', ' ')}). Droits administrateur requis.");
        }

        _journal?.Invoke($"MSFT_StorageReliabilityCounter : {compteurs.Count} compteur(s)");
        if (compteurs.Count == 0)
        {
            // Cas réel observé : Windows ne renvoie aucun compteur, même en administrateur. Ce n'est pas une erreur de jointure.
            return new LectureDisques(section, SmartComplet: false,
                RaisonSmart: "Windows ne fournit aucun compteur de fiabilité pour ces disques (pilote ou matériel non compatible).");
        }
        foreach (var c in compteurs)
        {
            _journal?.Invoke($"  compteur DeviceId={Texte(c, "DeviceId") ?? "absent"} GUID={DernierGuid(Texte(c, "ObjectId")) ?? "absent"} temp={Texte(c, "Temperature") ?? "absente"}");
        }

        // Association disque ↔ compteur (la voie officielle). Échec : on bascule sur DeviceId.
        var liens = LireAssociations();

        var parId = compteurs
            .Where(c => Texte(c, "DeviceId") is not null)
            .GroupBy(c => Texte(c, "DeviceId")!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var enrichis = new List<DisqueSante>();
        for (var i = 0; i < section.Elements.Count; i++)
        {
            var d = section.Elements[i];
            var compteur = TrouverCompteur(i, d, compteurs, liens, parId, out var methode);
            _journal?.Invoke($"  jointure disque {d.Identifiant} -> {methode}");

            enrichis.Add(compteur is null ? d : d with
            {
                TemperatureC = (int?)Entier(compteur, "Temperature"),
                TemperatureMaxC = (int?)Entier(compteur, "TemperatureMax"),
                UsurePourcent = (int?)Entier(compteur, "Wear"),
                HeuresMarche = (int?)Entier(compteur, "PowerOnHours"),
                ErreursLectureTotal = Entier(compteur, "ReadErrorsTotal"),
            });
        }

        var sansCompteur = enrichis.Count(d => d.TemperatureC is null && d.UsurePourcent is null);
        var raison = sansCompteur == 0 ? null : $"{sansCompteur} disque(s) sans compteurs SMART correspondants.";

        return new LectureDisques(section with { Elements = enrichis }, SmartComplet: sansCompteur == 0, RaisonSmart: raison);
    }

    private IReadOnlyList<string?> _objetsDisques = [];

    /// <summary>Liens (référence disque, référence compteur) de l'association, ou liste vide si la lecture échoue.</summary>
    private IReadOnlyList<(string? Disque, string? Compteur)> LireAssociations()
    {
        try
        {
            var lignes = _source.Requeter(Stockage, "MSFT_PhysicalDiskToStorageReliabilityCounter",
                ["PhysicalDisk", "StorageReliabilityCounter"]);

            var liens = lignes.Select(l => (Disque: Texte(l, "PhysicalDisk"), Compteur: Texte(l, "StorageReliabilityCounter"))).ToList();
            _journal?.Invoke($"MSFT_PhysicalDiskToStorageReliabilityCounter : {liens.Count} lien(s)");
            foreach (var lien in liens.Take(10))
            {
                _journal?.Invoke($"  lien disque={lien.Disque ?? "absent"} compteur={lien.Compteur ?? "absent"}");
            }

            return liens;
        }
        catch (Exception e)
        {
            _journal?.Invoke($"Association illisible : {e.GetType().Name} : {e.Message}");
            return [];
        }
    }

    private IReadOnlyDictionary<string, object?>? TrouverCompteur(
        int indexDisque,
        DisqueSante disque,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> compteurs,
        IReadOnlyList<(string? Disque, string? Compteur)> liens,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> parId,
        out string methode)
    {
        var guidDisque = indexDisque < _objetsDisques.Count ? DernierGuid(_objetsDisques[indexDisque]) : null;

        if (guidDisque is not null)
        {
            var lien = liens.FirstOrDefault(l => l.Disque is not null && l.Disque.Contains(guidDisque, StringComparison.OrdinalIgnoreCase));
            if (lien.Compteur is not null)
            {
                var compteur = compteurs.FirstOrDefault(c =>
                    DernierGuid(Texte(c, "ObjectId")) is { } g && lien.Compteur.Contains(g, StringComparison.OrdinalIgnoreCase));
                if (compteur is not null)
                {
                    methode = "association";
                    return compteur;
                }
            }
        }

        if (disque.Identifiant is not null && parId.TryGetValue(disque.Identifiant, out var parIdentifiant))
        {
            methode = "DeviceId";
            return parIdentifiant;
        }

        methode = "aucune";
        return null;
    }

    /// <summary>Dernier GUID entre accolades d'une chaîne (ObjectId, référence WMI).</summary>
    public static string? DernierGuid(string? texte)
    {
        if (texte is null)
        {
            return null;
        }

        var correspondances = GuidRegex.Matches(texte);
        return correspondances.Count == 0 ? null : correspondances[^1].Value;
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
