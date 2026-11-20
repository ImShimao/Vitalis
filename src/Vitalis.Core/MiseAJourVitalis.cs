using System.Net.Http.Headers;
using System.Text.Json;

namespace Vitalis.Core;

/// <summary>Résultat de la vérification : version publiée la plus récente et page où la télécharger.</summary>
public sealed record VersionPubliee(string Version, string Page, bool PlusRecente);

/// <summary>
/// Vérifie, à la demande de l'utilisateur seulement, si une version plus récente de Vitalis est publiée sur GitHub
/// (API « releases/latest », sans compte ni donnée envoyée hormis l'adresse IP inhérente à toute connexion).
/// Rien n'est téléchargé : la page de la version s'ouvre dans le navigateur.
/// </summary>
public static class MiseAJourVitalis
{
    public const string Depot = "ImShimao/Vitalis";
    public const string PageVersions = "https://github.com/" + Depot + "/releases";

    public static async Task<VersionPubliee> VerifierAsync(CancellationToken ct = default)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(AppInfo.Nom, AppInfo.Version));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using var reponse = await client.GetAsync($"https://api.github.com/repos/{Depot}/releases/latest", ct).ConfigureAwait(false);
        if (reponse.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException("Aucune version publiée pour l'instant.");
        }

        reponse.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await reponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
        var tag = json.RootElement.GetProperty("tag_name").GetString() ?? throw new InvalidDataException("Réponse GitHub sans version.");
        var page = json.RootElement.TryGetProperty("html_url", out var u) && u.GetString() is { } url
            && url.StartsWith("https://github.com/" + Depot + "/", StringComparison.OrdinalIgnoreCase) ? url : PageVersions;
        return new VersionPubliee(tag.TrimStart('v', 'V'), page, EstPlusRecente(tag, AppInfo.Version));
    }

    /// <summary>« v0.3.0 » est plus récente que « 0.2.0 ». Une étiquette illisible n'est jamais considérée plus récente.</summary>
    public static bool EstPlusRecente(string etiquette, string actuelle)
    {
        static Version? Lire(string s)
        {
            var propre = s.Trim().TrimStart('v', 'V').Split('-', '+')[0];
            return Version.TryParse(propre.Contains('.') ? propre : propre + ".0", out var v) ? v : null;
        }

        return Lire(etiquette) is { } publiee && Lire(actuelle) is { } locale && publiee > locale;
    }
}
