namespace Vitalis.Core.Pilotes;

/// <summary>Un pilote plus récent proposé par Windows Update pour un matériel de ce PC.</summary>
public sealed record PiloteDisponible(
    string Titre,
    string? Modele,
    string? Fournisseur,
    string? Classe,
    DateTime? Date,
    string? HardwareId,
    string UpdateId,
    bool Masque);

/// <summary>Pilote installé mis en face du pilote proposé (la comparaison façon « version installée → disponible »).</summary>
public sealed record ComparaisonPilote(PiloteDisponible Disponible, Pilote? Installe);

/// <summary>
/// Catalogue de pilotes de Windows Update : Microsoft y publie les pilotes que les fabricants ont signés et validés
/// pour le matériel détecté. C'est la seule source officielle qui dit, matériel par matériel, qu'un pilote plus récent existe.
/// Source : IUpdateSearcher::Search avec Type='Driver', IWindowsDriverUpdate (DriverHardwareID, DriverModel,
/// DriverProvider, DriverClass, DriverVerDate). Doc Microsoft vérifiée le 2026-11-20.
/// </summary>
public static class CataloguePilotes
{
    public static IReadOnlyList<PiloteDisponible> Rechercher()
    {
        var type = Type.GetTypeFromProgID("Microsoft.Update.Session")
            ?? throw new InvalidOperationException("Windows Update Agent n'est pas disponible.");
        dynamic session = Activator.CreateInstance(type)!;
        session.ClientApplicationID = "Vitalis";
        dynamic resultat = session.CreateUpdateSearcher().Search("IsInstalled=0 and Type='Driver'");
        dynamic updates = resultat.Updates;

        int n = updates.Count;
        var liste = new List<PiloteDisponible>(n);
        for (var i = 0; i < n; i++)
        {
            dynamic u = updates.Item(i);
            liste.Add(new PiloteDisponible(
                (string?)u.Title ?? "Pilote",
                Vide((string?)u.DriverModel),
                Vide((string?)u.DriverProvider),
                Vide((string?)u.DriverClass),
                (DateTime?)u.DriverVerDate,
                Vide((string?)u.DriverHardwareID),
                (string)u.Identity.UpdateID,
                (bool)u.IsHidden));
        }

        return liste;
    }

    /// <summary>
    /// Associe chaque pilote proposé au pilote installé du même matériel. Win32_PnPSignedDriver ne donne que l'identifiant
    /// matériel principal, plus précis que celui ciblé par Windows Update : on accepte donc qu'il commence par celui-ci.
    /// </summary>
    public static IReadOnlyList<ComparaisonPilote> Comparer(IReadOnlyList<PiloteDisponible> disponibles, IReadOnlyList<Pilote> installes) =>
        disponibles.Select(d => new ComparaisonPilote(d, d.HardwareId is null ? null : installes
            .Where(p => p.HardwareId is not null && p.HardwareId.StartsWith(d.HardwareId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.HardwareId!.Length)
            .FirstOrDefault()))
        .ToList();

    private static string? Vide(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
