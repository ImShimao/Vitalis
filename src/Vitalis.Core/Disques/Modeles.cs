using Vitalis.Core.Materiel;

namespace Vitalis.Core.Disques;

/// <summary>Statut de santé d'un disque physique, d'après MSFT_PhysicalDisk.HealthStatus.</summary>
public enum StatutSante
{
    Sain,
    Attention,
    Critique,
    Inconnu,
}

public enum TypeMedia
{
    Inconnu,
    Hdd,
    Ssd,
    Scm,
}

/// <summary>Statut d'un lecteur d'après son espace libre.</summary>
public enum StatutEspace
{
    Sain,
    Attention,
    Critique,
}

/// <summary>
/// Un disque physique. Les champs SMART (température, usure, heures, erreurs) viennent de
/// MSFT_StorageReliabilityCounter et restent null si ces compteurs ne sont pas lisibles (droits administrateur).
/// </summary>
public sealed record DisqueSante(
    string? Identifiant,
    string? Nom,
    TypeMedia Type,
    long? TailleOctets,
    StatutSante Sante,
    int? TemperatureC,
    int? TemperatureMaxC,
    int? UsurePourcent,
    int? HeuresMarche,
    long? ErreursLectureTotal);

/// <summary>Un lecteur logique fixe (DriveType = 3).</summary>
public sealed record LecteurEspace(
    string Lettre,
    string? Nom,
    long? TailleOctets,
    long? EspaceLibreOctets,
    double? PourcentLibre,
    StatutEspace Statut);

/// <summary>
/// Résultat complet. <see cref="SmartComplet"/> est false si les compteurs de fiabilité n'ont pas pu être lus :
/// l'application propose alors de relancer en administrateur.
/// </summary>
public sealed record RapportDisques(
    Section<DisqueSante> Disques,
    Section<LecteurEspace> Lecteurs,
    bool SmartComplet,
    string? RaisonSmart);
