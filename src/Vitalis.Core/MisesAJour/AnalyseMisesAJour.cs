using Vitalis.Core.Materiel;

namespace Vitalis.Core.MisesAJour;

/// <summary>
/// Mises à jour Windows en attente et date de la dernière installation réussie.
/// Lecture seule : rien n'est téléchargé ni installé. Chaque lecture est isolée et ne peut pas faire échouer l'autre.
/// </summary>
public sealed class AnalyseMisesAJour
{
    /// <summary>Nombre d'entrées d'historique lues (suffisant pour trouver la dernière installation réussie).</summary>
    public const int NombreHistorique = 100;

    private readonly IServiceWua _wua;
    private readonly IServiceWinget _winget;

    public AnalyseMisesAJour(IServiceWua wua, IServiceWinget winget)
    {
        _wua = wua;
        _winget = winget;
    }

    public async Task<RapportMisesAJour> AnalyserAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var enAttente = Task.Run(() => LireEnAttente(), ct);
        var historique = Task.Run(() => LireHistorique(), ct);
        var logiciels = Task.Run(() => LireLogiciels(), ct);

        await Task.WhenAll(enAttente, historique, logiciels).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();

        return new RapportMisesAJour(
            enAttente.Result,
            historique.Result.Date,
            historique.Result.Raison,
            logiciels.Result);
    }

    private Section<MiseAJourLogiciel> LireLogiciels()
    {
        try
        {
            var elements = _winget.RechercherMisesAJour();
            return elements.Count == 0
                ? new Section<MiseAJourLogiciel>(Disponibilite.Disponible, [], "Aucune mise à jour de logiciel signalée par winget.")
                : new Section<MiseAJourLogiciel>(Disponibilite.Disponible, elements);
        }
        catch (Exception e)
        {
            return new Section<MiseAJourLogiciel>(Disponibilite.Indisponible, [],
                $"winget indisponible ({e.GetType().Name} : {Nettoyer(e)}). Vérifiez que winget est installé.");
        }
    }

    private Section<MiseAJour> LireEnAttente()
    {
        try
        {
            var elements = _wua.RechercherEnAttente();
            return elements.Count == 0
                ? new Section<MiseAJour>(Disponibilite.Disponible, [], "Aucune mise à jour en attente.")
                : new Section<MiseAJour>(Disponibilite.Disponible, elements);
        }
        catch (Exception e)
        {
            return new Section<MiseAJour>(Disponibilite.Indisponible, [], $"Recherche Windows Update impossible ({e.GetType().Name} : {Nettoyer(e)})");
        }
    }

    private (DateTime? Date, string? Raison) LireHistorique()
    {
        try
        {
            var entrees = _wua.Historique(NombreHistorique);
            var derniere = entrees.Where(e => e.Reussie && e.Date is not null)
                .OrderByDescending(e => e.Date)
                .FirstOrDefault();

            return derniere is null
                ? (null, "Aucune installation réussie dans l'historique récent.")
                : (derniere.Date, null);
        }
        catch (Exception e)
        {
            return (null, $"Historique Windows Update illisible ({e.GetType().Name} : {Nettoyer(e)})");
        }
    }

    private static string Nettoyer(Exception e) => e.Message.Trim().TrimEnd('.', ' ');
}
