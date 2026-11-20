using Vitalis.Core.Disques;
using Vitalis.Core.Materiel;
using Vitalis.Core.MisesAJour;

namespace Vitalis.Core.TableauDeBord;

/// <summary>Une carte de synthèse. Une carte en erreur porte sa raison et n'empêche jamais les autres.</summary>
public sealed record CarteSynthese(string Titre, string? Valeur, string? Detail, Disponibilite Etat, string? Raison);

public sealed record ResultatTableauDeBord(
    int? Score,
    IReadOnlyList<ComposantScore> Composants,
    IReadOnlyList<CarteSynthese> Cartes,
    DateTime Date,
    RapportMateriel? Materiel = null,
    RapportDisques? Disques = null,
    RapportMisesAJour? MisesAJour = null);

/// <summary>
/// Lance les quatre analyses en parallèle, construit les cartes et le score.
/// Chaque analyse est isolée : une exception devient une carte « indisponible ».
/// </summary>
public sealed class TableauDeBordService
{
    private readonly InventaireMateriel _materiel;
    private readonly AnalyseDisques _disques;
    private readonly AnalyseMisesAJour _misesAJour;
    private readonly Func<DateTime> _horloge;

    public TableauDeBordService(InventaireMateriel materiel, AnalyseDisques disques, AnalyseMisesAJour misesAJour, Func<DateTime>? horloge = null)
    {
        _materiel = materiel;
        _disques = disques;
        _misesAJour = misesAJour;
        _horloge = horloge ?? (() => DateTime.Now);
    }

    /// <param name="progression">Reçoit le pourcentage d'analyses terminées (0 à 100).</param>
    public async Task<ResultatTableauDeBord> AnalyserAsync(IProgress<int>? progression = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var termines = 0;
        async Task<T?> Suivre<T>(Func<Task<T>> analyse) where T : class
        {
            try
            {
                return await analyse().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                var n = Interlocked.Increment(ref termines);
                progression?.Report(n * 100 / 3);
            }
        }

        var tMateriel = Suivre(() => _materiel.AnalyserAsync(ct));
        var tDisques = Suivre(() => _disques.AnalyserAsync(ct));
        var tMaj = Suivre(() => _misesAJour.AnalyserAsync(ct));

        var materiel = await tMateriel.ConfigureAwait(false);
        var disques = await tDisques.ConfigureAwait(false);
        var maj = await tMaj.ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();

        var maintenant = _horloge();
        var cartes = new List<CarteSynthese>
        {
            CarteMateriel(materiel),
            CarteDisques(disques),
            CarteEspace(disques),
            CarteMisesAJour(maj),
        };

        var composants = new List<ComposantScore>
        {
            disques is null ? new ComposantScore("Disques", Score.PoidsDisques, null) : Score.Disques(disques.Disques.Elements),
            // Mises à jour en erreur : composant indéterminé (et non « 0 mise en attente »).
            Score.MisesAJour(
                maj is null || maj.EnAttente.Etat == Disponibilite.Indisponible ? null : maj.EnAttente.Elements.Count,
                maj?.DerniereInstallationReussie,
                maintenant)!,
            disques is null ? new ComposantScore("Espace libre", Score.PoidsEspace, null) : Score.Espace(disques.Lecteurs.Elements),
        };

        return new ResultatTableauDeBord(Score.Global(composants), composants, cartes, maintenant, materiel, disques, maj);
    }

    private static CarteSynthese CarteMateriel(RapportMateriel? r)
    {
        if (r is null)
        {
            return Indisponible("Matériel", "Analyse matériel en erreur.");
        }

        var cpu = r.Processeur.Elements.FirstOrDefault()?.Nom;
        var memoire = Conversions.MemoireTotaleOctets(r.Memoire.Elements);
        var detail = memoire is null ? null : $"{(memoire.Value / 1024.0 / 1024 / 1024).ToString("0", System.Globalization.CultureInfo.CurrentCulture)} Gio de mémoire";
        return new CarteSynthese("Matériel", cpu ?? "Processeur inconnu", detail, r.Processeur.Etat, r.Processeur.Raison);
    }

    private static CarteSynthese CarteDisques(RapportDisques? r)
    {
        if (r is null)
        {
            return Indisponible("Disques", "Analyse des disques en erreur.");
        }

        var disques = r.Disques.Elements;
        StatutSante? pire = disques.Count == 0 ? null : disques.Select(d => d.Sante).OrderBy(s => s switch
        {
            StatutSante.Critique => 0,
            StatutSante.Attention => 1,
            StatutSante.Sain => 2,
            _ => 3,
        }).First();

        return new CarteSynthese("Disques", $"{disques.Count} disque(s)", pire is null ? null : $"Pire état : {pire}",
            r.Disques.Etat, r.Disques.Raison);
    }

    private static CarteSynthese CarteEspace(RapportDisques? r)
    {
        if (r is null)
        {
            return Indisponible("Espace libre", "Analyse des lecteurs en erreur.");
        }

        var lecteurs = r.Lecteurs.Elements.Where(l => l.PourcentLibre is not null).ToList();
        if (lecteurs.Count == 0)
        {
            return new CarteSynthese("Espace libre", null, null, r.Lecteurs.Etat, r.Lecteurs.Raison ?? "Aucun lecteur lisible.");
        }

        var plusFaible = lecteurs.MinBy(l => l.PourcentLibre)!;
        return new CarteSynthese("Espace libre", $"{plusFaible.PourcentLibre!.Value:0} % libre",
            $"Lecteur le plus chargé : {plusFaible.Lettre}", r.Lecteurs.Etat, r.Lecteurs.Raison);
    }

    private static CarteSynthese CarteMisesAJour(RapportMisesAJour? r)
    {
        if (r is null)
        {
            return Indisponible("Mises à jour", "Analyse des mises à jour en erreur.");
        }

        // Le score ne compte que les mises Windows ; la carte montre séparément les logiciels (winget).
        var windows = r.EnAttente.Elements.Count;
        var logiciels = r.Logiciels.Elements.Count;
        var date = r.DerniereInstallationReussie is { } d ? d.ToString("d MMMM yyyy", System.Globalization.CultureInfo.CurrentCulture) : "inconnue";
        var etat = r.EnAttente.Etat == Disponibilite.Indisponible ? Disponibilite.Indisponible : Disponibilite.Disponible;
        return new CarteSynthese("Mises à jour", $"{windows} Windows · {logiciels} logiciel(s)",
            $"Dernière installation Windows : {date}", etat, r.EnAttente.Raison);
    }

    private static CarteSynthese Indisponible(string titre, string raison) =>
        new(titre, null, null, Disponibilite.Indisponible, raison);
}
