using Vitalis.Core.Materiel;

namespace Vitalis.Core.MisesAJour;

/// <summary>Une mise à jour Windows en attente (logiciel ou pilote). Lecture seule : rien n'est installé.</summary>
public sealed record MiseAJour(
    string Titre,
    IReadOnlyList<string> Kb,
    string? Gravite,
    bool Obligatoire,
    bool Pilote);

/// <summary>Une entrée de l'historique Windows Update.</summary>
public sealed record EntreeHistorique(DateTime? Date, string? Titre, bool Reussie);

public sealed record RapportMisesAJour(
    Section<MiseAJour> EnAttente,
    DateTime? DerniereInstallationReussie,
    string? RaisonHistorique);
