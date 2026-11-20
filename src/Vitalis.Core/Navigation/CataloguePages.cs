namespace Vitalis.Core.Navigation;

/// <summary>Une page de l'interface : clé stable (nom de la note dans 04-Fonctions), titre français, étape de remplissage.</summary>
public sealed record PageDefinition(string Cle, string Titre, int EtapeDisponible);

/// <summary>Les 10 pages de la v1, dans l'ordre du menu. Voir ADR-002 pour le regroupement.</summary>
public static class CataloguePages
{
    public static IReadOnlyList<PageDefinition> Toutes { get; } =
    [
        new("tableau-de-bord", "Tableau de bord", 5),
        new("materiel", "Matériel", 2),
        new("disques", "Disques", 3),
        new("mises-a-jour", "Mises à jour", 4),
        new("pilotes", "Pilotes", 7),
        new("demarrage", "Démarrage", 8),
        new("performances", "Performances", 9),
        new("sante-live", "Santé live", 10),
        new("rapport", "Rapport", 6),
        new("parametres", "Paramètres", 11),
    ];
}
