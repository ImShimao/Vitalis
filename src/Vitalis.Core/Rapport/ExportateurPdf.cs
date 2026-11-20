using System.Diagnostics;

namespace Vitalis.Core.Rapport;

/// <summary>
/// Export PDF d'un fichier HTML via Microsoft Edge en mode sans affichage (--print-to-pdf).
/// Edge est fourni avec Windows 10 et 11. Aucune bibliothèque tierce.
/// </summary>
public static class ExportateurPdf
{
    private static readonly string[] Chemins =
    [
        @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
    ];

    public static string? TrouverEdge() => Chemins.FirstOrDefault(File.Exists);

    /// <returns>Null si l'export a réussi, sinon la raison de l'échec.</returns>
    public static string? Exporter(string fichierHtml, string fichierPdf)
    {
        var edge = TrouverEdge();
        if (edge is null)
        {
            return "Microsoft Edge introuvable : export PDF impossible (le fichier HTML reste disponible).";
        }

        if (File.Exists(fichierPdf))
        {
            File.Delete(fichierPdf);
        }

        var uri = new Uri(Path.GetFullPath(fichierHtml)).AbsoluteUri;
        var psi = new ProcessStartInfo(edge)
        {
            ArgumentList = { "--headless", "--disable-gpu", "--no-pdf-header-footer", $"--print-to-pdf={Path.GetFullPath(fichierPdf)}", uri },
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var processus = Process.Start(psi) ?? throw new InvalidOperationException("Edge n'a pas pu être lancé.");
        if (!processus.WaitForExit(TimeSpan.FromSeconds(60)))
        {
            processus.Kill(entireProcessTree: true);
            return "L'export PDF a dépassé 60 secondes.";
        }

        return File.Exists(fichierPdf) ? null : "Edge n'a pas créé le fichier PDF.";
    }
}
