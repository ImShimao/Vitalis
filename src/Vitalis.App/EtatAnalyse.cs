using Vitalis.Core.Disques;
using Vitalis.Core.Materiel;
using Vitalis.Core.MisesAJour;
using Vitalis.Core.Pilotes;
using Vitalis.Core.TableauDeBord;

namespace Vitalis.App;

/// <summary>
/// Un résultat d'analyse partagé par la session : le dernier résultat, et l'analyse en cours s'il y en a une.
/// Quitter une page ne coupe pas l'analyse : en y revenant, la page retrouve l'analyse en cours ou son résultat.
/// </summary>
public sealed class Chargeur<T> where T : class
{
    private Task<T>? _enCours;
    private CancellationTokenSource? _annulation;

    /// <summary>Vrai si l'analyse du tableau de bord fournit aussi ce résultat.</summary>
    public bool RempliParTableau { get; init; }

    public T? Valeur { get; private set; }

    public DateTime? Date { get; private set; }

    public bool EnCours => _enCours is { IsCompleted: false };

    /// <summary>L'analyse en cours ou la dernière lancée (null si aucune).</summary>
    public Task<T>? Tache => _enCours;

    /// <summary>Dernier pourcentage reçu (0 à 100) pendant l'analyse en cours.</summary>
    public int Progression { get; private set; }

    public event Action<int>? ProgressionChangee;

    /// <summary>Renvoie l'analyse en cours, ou en lance une. Le résultat est gardé pour la session.</summary>
    public Task<T> Obtenir(Func<IProgress<int>, CancellationToken, Task<T>> analyse)
    {
        if (_enCours is { IsCompleted: false })
        {
            return _enCours;
        }

        _annulation = new CancellationTokenSource();
        Progression = 0;
        // Progress<T> capture le contexte du thread d'interface : les abonnés sont appelés sur ce thread.
        var progres = new Progress<int>(v =>
        {
            Progression = v;
            ProgressionChangee?.Invoke(v);
        });

        _enCours = Executer(analyse, progres, _annulation.Token);
        return _enCours;
    }

    private async Task<T> Executer(Func<IProgress<int>, CancellationToken, Task<T>> analyse, IProgress<int> progres, CancellationToken ct)
    {
        var r = await analyse(progres, ct);
        Definir(r, DateTime.Now);
        return r;
    }

    public void Annuler() => _annulation?.Cancel();

    public void Definir(T valeur, DateTime date)
    {
        Valeur = valeur;
        Date = date;
    }
}

/// <summary>Résultats de la session, en mémoire seulement (rien n'est écrit sur le disque).</summary>
public static class EtatAnalyse
{
    public static Chargeur<ResultatTableauDeBord> Tableau { get; } = new();

    public static Chargeur<RapportMateriel> Materiel { get; } = new() { RempliParTableau = true };

    public static Chargeur<RapportDisques> Disques { get; } = new() { RempliParTableau = true };

    public static Chargeur<RapportMisesAJour> MisesAJour { get; } = new() { RempliParTableau = true };

    public static Chargeur<RapportPilotes> Pilotes { get; } = new();

    /// <summary>L'analyse du tableau de bord remplit aussi les pages Matériel, Disques et Mises à jour.</summary>
    public static void Repartir(ResultatTableauDeBord r)
    {
        if (r.Materiel is not null)
        {
            Materiel.Definir(r.Materiel, r.Date);
        }

        if (r.Disques is not null)
        {
            Disques.Definir(r.Disques, r.Date);
        }

        if (r.MisesAJour is not null)
        {
            MisesAJour.Definir(r.MisesAJour, r.Date);
        }
    }
}
