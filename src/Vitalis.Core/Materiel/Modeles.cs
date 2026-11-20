namespace Vitalis.Core.Materiel;

/// <summary>État d'une famille de matériel. Une valeur absente reste null : jamais de donnée inventée.</summary>
public enum Disponibilite
{
    /// <summary>Toutes les valeurs essentielles sont présentes.</summary>
    Disponible,

    /// <summary>Des éléments existent, mais une valeur essentielle manque.</summary>
    Partielle,

    /// <summary>Rien à afficher : classe absente, erreur de lecture ou aucun élément.</summary>
    Indisponible,
}

/// <summary>Résultat d'une famille. Une erreur dans une famille ne touche jamais les autres.</summary>
public sealed record Section<T>(Disponibilite Etat, IReadOnlyList<T> Elements, string? Raison = null);

public sealed record Processeur(string? Nom, string? Fabricant, int? Coeurs, int? ThreadsLogiques, int? FrequenceMaxMHz, int? CacheL2Ko, int? CacheL3Ko);

public sealed record CarteMere(string? Fabricant, string? Modele, string? Version);

public sealed record Bios(string? Fabricant, string? Version, DateTime? DateSortie);

/// <summary>Un module de mémoire vive (un élément par barrette).</summary>
public sealed record Memoire(string? Emplacement, long? CapaciteOctets, int? VitesseMHz, string? Fabricant);

/// <summary>Carte graphique. <see cref="MemoireOctets"/> est plafonnée à 4 Gio par WMI (voir fonction inventaire-materiel).</summary>
public sealed record CarteGraphique(string? Nom, long? MemoireOctets, string? VersionPilote, DateTime? DatePilote, int? LargeurPx, int? HauteurPx, int? FrequenceHz);

public sealed record Ecran(string? Fabricant, string? Nom);

/// <summary>Adaptateur réseau matériel (filtré sur HardwareInterface = TRUE).</summary>
public sealed record AdaptateurReseau(string? Nom, string? Description);

public sealed record PeripheriqueAudio(string? Nom, string? Fabricant, string? Statut);

public sealed record RapportMateriel(
    Section<Processeur> Processeur,
    Section<CarteMere> CarteMere,
    Section<Bios> Bios,
    Section<Memoire> Memoire,
    Section<CarteGraphique> CartesGraphiques,
    Section<Ecran> Ecrans,
    Section<AdaptateurReseau> Reseau,
    Section<PeripheriqueAudio> Audio);
