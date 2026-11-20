using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Vitalis.Core.MisesAJour;

/// <summary>Résultat d'une installation : réussite, et message lisible (code d'erreur ou dernière ligne utile).</summary>
public sealed record ResultatInstallation(bool Reussi, string Message, bool RedemarrageRequis = false);

/// <summary>
/// Met à jour un logiciel par « winget upgrade --id &lt;id&gt; --exact ». Lancé seulement après confirmation de l'utilisateur.
/// L'identifiant vient de la sortie de winget : il est revalidé avant d'être placé sur la ligne de commande.
/// </summary>
public static class InstallateurWinget
{
    // Identifiant winget : « Editeur.Produit », lettres, chiffres, point, tiret, plus, soulignement.
    private static readonly Regex IdValide = new(@"^[A-Za-z0-9][A-Za-z0-9._+\-]{0,127}$", RegexOptions.Compiled);
    private static readonly TimeSpan Delai = TimeSpan.FromMinutes(20);

    // Codes winget documentés (github.com/microsoft/winget-cli, doc/windows/package-manager/winget/returnCodes.md).
    private const int AucuneMiseAJourApplicable = unchecked((int)0x8A15002B);
    private const int RedemarrageNecessaire = unchecked((int)0x8A150109);
    private const int RedemarrerPuisReessayer = unchecked((int)0x8A15010A);
    private const int RedemarrageInitie = unchecked((int)0x8A15010B);

    public static bool EstIdValide(string id) => IdValide.IsMatch(id);

    /// <param name="installer">Vrai : « winget install » (logiciel absent, ex. PawnIO), faux : « winget upgrade ».</param>
    public static string Arguments(string id, bool installer = false)
    {
        if (!EstIdValide(id))
        {
            throw new ArgumentException("Identifiant winget invalide.", nameof(id));
        }

        return $"{(installer ? "install" : "upgrade")} --id \"{id}\" --exact --source winget --silent --accept-package-agreements --accept-source-agreements --disable-interactivity";
    }

    public static Task<ResultatInstallation> MettreAJourAsync(string id, CancellationToken ct = default) => ExecuterAsync(Arguments(id), ct);

    public static Task<ResultatInstallation> InstallerAsync(string id, CancellationToken ct = default) => ExecuterAsync(Arguments(id, installer: true), ct);

    private static async Task<ResultatInstallation> ExecuterAsync(string arguments, CancellationToken ct)
    {
        var psi = new ProcessStartInfo("winget", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
        };

        using var processus = Process.Start(psi) ?? throw new InvalidOperationException("winget n'a pas pu être lancé.");
        var sortie = processus.StandardOutput.ReadToEndAsync(ct);
        _ = processus.StandardError.ReadToEndAsync(ct);

        using var delai = CancellationTokenSource.CreateLinkedTokenSource(ct);
        delai.CancelAfter(Delai);
        try
        {
            await processus.WaitForExitAsync(delai.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            processus.Kill(entireProcessTree: true);
            return new ResultatInstallation(false, ct.IsCancellationRequested ? "Annulé." : "winget n'a pas terminé dans le délai prévu (20 minutes).");
        }

        return Interpreter(processus.ExitCode, await sortie.ConfigureAwait(false));
    }

    public static ResultatInstallation Interpreter(int code, string sortie) => code switch
    {
        0 => new ResultatInstallation(true, "Installation terminée."),
        RedemarrageNecessaire or RedemarrageInitie => new ResultatInstallation(true, "Mis à jour : redémarrez le PC pour terminer.", RedemarrageRequis: true),
        RedemarrerPuisReessayer => new ResultatInstallation(false, "Installation impossible pour l'instant : redémarrez le PC puis réessayez.", RedemarrageRequis: true),
        AucuneMiseAJourApplicable => new ResultatInstallation(false, "winget ne trouve plus de mise à jour applicable (déjà à jour, ou installée autrement)."),
        _ => new ResultatInstallation(false, $"Échec (code 0x{code:X8}). {DerniereLigne(sortie)}".Trim()),
    };

    /// <summary>Dernière ligne lisible de la sortie (sans barres de progression ni caractères de contrôle).</summary>
    private static string DerniereLigne(string sortie) =>
        sortie.Split('\n', '\r')
            .Select(l => Regex.Replace(l, @"[\u0000-\u001F]", string.Empty).Trim())
            .LastOrDefault(l => l.Length > 3 && l.Any(char.IsLetter) && !l.Contains('█') && !l.Contains('▒')) ?? string.Empty;
}
