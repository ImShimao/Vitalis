namespace Vitalis.Core.MisesAJour;

/// <summary>
/// Accès à Windows Update Agent (API COM Microsoft.Update.Session). Lecture seule.
/// L'implémentation réelle est <see cref="WuaCom"/> ; les tests utilisent une fausse implémentation.
/// </summary>
public interface IServiceWua
{
    /// <summary>Mises à jour non installées et non masquées (logiciels et pilotes).</summary>
    IReadOnlyList<MiseAJour> RechercherEnAttente();

    /// <summary>Entrées de l'historique, de la plus récente à la plus ancienne.</summary>
    IReadOnlyList<EntreeHistorique> Historique(int nombreMax);
}
