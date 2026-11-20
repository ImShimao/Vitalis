using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Vitalis.Core.MisesAJour;

/// <summary>Une application pour laquelle winget signale une version plus récente.</summary>
public sealed record MiseAJourLogiciel(string Nom, string Id, string VersionActuelle, string VersionDisponible);

/// <summary>
/// Mises à jour de logiciels via la commande winget (lecture seule : « winget upgrade » sans --all ni identifiant).
/// Choix de l'étape 4 (ADR-007) : l'API COM WinGet n'a pas pu être vérifiée dans la documentation Microsoft.
/// </summary>
public interface IServiceWinget
{
    IReadOnlyList<MiseAJourLogiciel> RechercherMisesAJour();
}

public sealed class WingetCli : IServiceWinget
{
    private const string Arguments = "upgrade --source winget --accept-source-agreements --disable-interactivity";
    private static readonly TimeSpan Delai = TimeSpan.FromMinutes(3);

    public IReadOnlyList<MiseAJourLogiciel> RechercherMisesAJour()
    {
        var psi = new ProcessStartInfo("winget", Arguments)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
        };

        using var processus = Process.Start(psi)
            ?? throw new InvalidOperationException("winget n'a pas pu être lancé.");

        // Lecture avant l'attente : évite le blocage si la sortie dépasse la mémoire tampon.
        var sortie = processus.StandardOutput.ReadToEnd();
        if (!processus.WaitForExit(Delai))
        {
            processus.Kill(entireProcessTree: true);
            throw new TimeoutException("winget n'a pas répondu dans le délai prévu.");
        }

        return AnalyseurWinget.Lire(sortie);
    }
}

/// <summary>
/// Lit le tableau texte de « winget upgrade ». Le format est fixe en colonnes, mais les en-têtes sont localisés :
/// on ne lit donc que les lignes après la ligne de tirets, et seules les lignes à 4 colonnes sont retenues.
/// </summary>
public static class AnalyseurWinget
{
    private static readonly Regex CaracteresDeCommande = new(@"[\u0000-\u0008\u000B-\u001F]", RegexOptions.Compiled);

    /// <summary>
    /// Lecture à partir de la droite : la version disponible, la version actuelle et l'identifiant sont les trois derniers
    /// jetons (le nom peut contenir des espaces, et l'identifiant est parfois collé au nom par un seul espace).
    /// </summary>
    public static IReadOnlyList<MiseAJourLogiciel> Lire(string sortie)
    {
        var propre = CaracteresDeCommande.Replace(sortie, string.Empty);
        var resultats = new List<MiseAJourLogiciel>();
        var apresSeparateur = false;

        foreach (var ligne in propre.Split('\n'))
        {
            var texte = ligne.Trim();
            if (!apresSeparateur)
            {
                apresSeparateur = texte.StartsWith("---", StringComparison.Ordinal);
                continue;
            }

            var mise = LireLigne(texte);
            if (mise is not null)
            {
                resultats.Add(mise);
            }
        }

        return resultats;
    }

    private static MiseAJourLogiciel? LireLigne(string texte)
    {
        var jetons = texte.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var n = jetons.Length;
        if (n < 4)
        {
            return null;
        }

        var dispo = jetons[n - 1];
        // Version actuelle écrite « < 1.2.3 » : le « < » occupe un jeton à part.
        var indexActuelle = n - 2;
        var indexId = n - 3;
        if (indexId >= 1 && jetons[indexId] == "<")
        {
            indexId = n - 4;
        }

        if (indexId < 1)
        {
            return null;
        }

        var id = jetons[indexId];
        var actuelle = jetons[indexActuelle];
        // Un identifiant winget contient un point ; une version contient un chiffre. Sinon : ligne de pied de page.
        if (!id.Contains('.') || !actuelle.Any(char.IsDigit) || !dispo.Any(char.IsDigit))
        {
            return null;
        }

        var nom = string.Join(' ', jetons.Take(indexId));
        return new MiseAJourLogiciel(nom, id, actuelle, dispo);
    }
}
