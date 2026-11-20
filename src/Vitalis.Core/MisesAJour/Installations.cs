using Vitalis.Core.Journal;

namespace Vitalis.Core.MisesAJour;

public enum EtatInstallation
{
    EnCours,
    Reussie,
    Echouee,
}

/// <summary>État d'un élément installé pendant la session (logiciel winget, pilote, ou mises à jour Windows).</summary>
public sealed record SuiviInstallation(EtatInstallation Etat, string Message, bool Redemarrage = false);

/// <summary>Bilan affiché en haut d'une page à la fin d'une série d'installations.</summary>
public sealed record BilanInstallation(bool Reussi, string Titre, string Message, bool Redemarrage);

/// <summary>
/// Installations de la session, indépendantes des pages : changer de page ne les arrête pas, et en revenant
/// la page retrouve l'élément en cours, les résultats et le bilan. Une seule installation à la fois dans toute
/// l'application (winget et Windows Update ne s'enchaînent pas bien en parallèle).
/// Appelé depuis le thread d'interface : les suites des await et l'événement <see cref="Change"/> y restent.
/// </summary>
public static class Installations
{
    public const string GroupeLogiciels = "logiciels";
    public const string GroupeWindows = "windows";
    public const string GroupePilotes = "pilotes";
    public const string CleWindows = "windows-update";

    private static readonly Dictionary<string, SuiviInstallation> Suivis = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, BilanInstallation> Bilans = new();

    public static bool EnCours { get; private set; }

    /// <summary>Un élément a changé d'état, ou une série a commencé ou fini.</summary>
    public static event Action? Change;

    public static SuiviInstallation? Suivi(string cle) => Suivis.GetValueOrDefault(cle);

    public static BilanInstallation? Bilan(string groupe) => Bilans.GetValueOrDefault(groupe);

    /// <summary>
    /// Installe les éléments l'un après l'autre. Faux si une installation est déjà en cours (rien n'est lancé).
    /// <paramref name="installer"/> reçoit la clé et un suivi d'étape ; une exception devient un échec.
    /// </summary>
    public static async Task<bool> LancerAsync(
        string groupe,
        IReadOnlyList<string> cles,
        Func<string, IProgress<string>, Task<ResultatInstallation>> installer,
        Func<IReadOnlyList<ResultatInstallation>, BilanInstallation> bilan)
    {
        if (EnCours || cles.Count == 0)
        {
            return false;
        }

        EnCours = true;
        Bilans.Remove(groupe);
        var resultats = new List<ResultatInstallation>();
        try
        {
            foreach (var cle in cles)
            {
                Definir(cle, new SuiviInstallation(EtatInstallation.EnCours, "Installation…"));
                var etape = new Progress<string>(t => Definir(cle, new SuiviInstallation(EtatInstallation.EnCours, t)));
                ResultatInstallation r;
                try
                {
                    r = await installer(cle, etape);
                }
                catch (Exception e)
                {
                    r = new ResultatInstallation(false, $"{e.GetType().Name} : {e.Message}");
                }

                JournalVitalis.Information($"Installation {groupe} {cle} : {(r.Reussi ? "réussie" : "échec")}.");
                resultats.Add(r);
                Definir(cle, new SuiviInstallation(r.Reussi ? EtatInstallation.Reussie : EtatInstallation.Echouee, r.Message, r.RedemarrageRequis));
            }
        }
        finally
        {
            EnCours = false;
            Bilans[groupe] = bilan(resultats);
            Change?.Invoke();
        }

        return true;
    }

    /// <summary>Un seul appel pour plusieurs clés (pilotes : une seule recherche Windows Update pour tous).</summary>
    public static Task<bool> LancerEnBlocAsync(
        string groupe,
        IReadOnlyList<string> cles,
        Func<IProgress<string>, Task<ResultatInstallation>> installer,
        Func<ResultatInstallation, BilanInstallation> bilan)
    {
        var bloc = string.Join("|", cles);
        return LancerAsync(groupe, [bloc], async (_, etape) =>
        {
            foreach (var cle in cles)
            {
                Definir(cle, new SuiviInstallation(EtatInstallation.EnCours, "Installation…"));
            }

            ResultatInstallation r;
            try
            {
                r = await installer(etape);
            }
            catch (Exception e)
            {
                r = new ResultatInstallation(false, $"{e.GetType().Name} : {e.Message}");
            }

            foreach (var cle in cles)
            {
                Definir(cle, new SuiviInstallation(r.Reussi ? EtatInstallation.Reussie : EtatInstallation.Echouee, r.Message, r.RedemarrageRequis));
            }

            return r;
        }, rs => bilan(rs[0]));
    }

    private static void Definir(string cle, SuiviInstallation suivi)
    {
        Suivis[cle] = suivi;
        Change?.Invoke();
    }
}
