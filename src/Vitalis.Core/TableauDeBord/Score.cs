using Vitalis.Core.Disques;

namespace Vitalis.Core.TableauDeBord;

/// <summary>Un composant du score : sa valeur sur 100 (null si indisponible) et son poids.</summary>
public sealed record ComposantScore(string Nom, int Poids, int? Valeur);

/// <summary>
/// Formules du score global sur 100. Choix de conception (ADR-008), à valider :
/// Disques 40 %, Mises à jour 35 %, Espace libre 25 %. Un composant indisponible n'est pas compté :
/// son poids est réparti sur les autres (le score reste sur 100).
/// </summary>
public static class Score
{
    public const int PoidsDisques = 40;
    public const int PoidsMisesAJour = 35;
    public const int PoidsEspace = 25;

    /// <summary>Mises à jour : 100 sans mise en attente, −10 par mise en attente, −20 si aucune installation depuis plus de 30 jours.</summary>
    public static ComposantScore? MisesAJour(int? nombreEnAttente, DateTime? derniereInstallation, DateTime maintenant)
    {
        if (nombreEnAttente is null)
        {
            return new ComposantScore("Mises à jour", PoidsMisesAJour, null);
        }

        var valeur = 100 - 10 * nombreEnAttente.Value;
        if (derniereInstallation is { } date && (maintenant - date).TotalDays > 30)
        {
            valeur -= 20;
        }

        return new ComposantScore("Mises à jour", PoidsMisesAJour, Math.Clamp(valeur, 0, 100));
    }

    /// <summary>Disques : moyenne par disque (Sain 100, Attention 50, Critique 0). Les disques de santé inconnue sont exclus.</summary>
    public static ComposantScore Disques(IReadOnlyList<DisqueSante> disques)
    {
        var connus = disques.Where(d => d.Sante != StatutSante.Inconnu).Select(d => d.Sante switch
        {
            StatutSante.Sain => 100,
            StatutSante.Attention => 50,
            _ => 0,
        }).ToList();

        return new ComposantScore("Disques", PoidsDisques, connus.Count == 0 ? null : (int)Math.Round(connus.Average()));
    }

    /// <summary>Espace libre : moyenne par lecteur (Sain 100, Attention 60, Critique 20).</summary>
    public static ComposantScore Espace(IReadOnlyList<LecteurEspace> lecteurs)
    {
        if (lecteurs.Count == 0)
        {
            return new ComposantScore("Espace libre", PoidsEspace, null);
        }

        var valeurs = lecteurs.Select(l => l.Statut switch
        {
            StatutEspace.Sain => 100,
            StatutEspace.Attention => 60,
            _ => 20,
        }).ToList();

        return new ComposantScore("Espace libre", PoidsEspace, (int)Math.Round(valeurs.Average()));
    }

    /// <summary>Moyenne pondérée des composants connus ; null si aucun composant n'est connu.</summary>
    public static int? Global(IReadOnlyList<ComposantScore> composants)
    {
        var connus = composants.Where(c => c.Valeur is not null).ToList();
        var poids = connus.Sum(c => c.Poids);
        if (poids == 0)
        {
            return null;
        }

        var somme = connus.Sum(c => c.Valeur!.Value * c.Poids);
        return (int)Math.Round((double)somme / poids);
    }
}
