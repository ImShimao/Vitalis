using Serilog;
using Serilog.Events;

namespace Vitalis.Core.Journal;

/// <summary>
/// Journal local (Serilog) : fichier par jour, 14 jours conservés, niveaux Information / Avertissement / Erreur.
/// Règle : aucune donnée personnelle n'est écrite (ni nom d'utilisateur, ni nom de machine, ni chemin de profil) :
/// seuls les types d'erreur et les messages techniques sont journalisés.
/// </summary>
public static class JournalVitalis
{
    public const int JoursConserves = 14;

    private static ILogger _logger = Serilog.Core.Logger.None;

    public static string DossierJournal(string dossierLocal) => Path.Combine(dossierLocal, "journaux");

    /// <param name="dossierLocal">Ex. %LOCALAPPDATA%\Vitalis.</param>
    /// <param name="modeDiagnostic">Si vrai, le niveau Débogage est aussi écrit.</param>
    public static void Initialiser(string dossierLocal, bool modeDiagnostic = false)
    {
        var dossier = DossierJournal(dossierLocal);
        Directory.CreateDirectory(dossier);

        _logger = new LoggerConfiguration()
            .MinimumLevel.Is(modeDiagnostic ? LogEventLevel.Debug : LogEventLevel.Information)
            .WriteTo.File(
                Path.Combine(dossier, "vitalis-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: JoursConserves,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    public static void Information(string message) => _logger.Information("{Message}", Nettoyer(message));

    public static void Avertissement(string message) => _logger.Warning("{Message}", Nettoyer(message));

    public static void Erreur(string message, Exception? exception = null)
    {
        // On journalise le type d'exception, pas son message complet : il peut contenir un chemin de profil.
        var texte = exception is null ? Nettoyer(message) : $"{Nettoyer(message)} ({exception.GetType().Name})";
        _logger.Error("{Message}", texte);
    }

    public static void Debug(string message) => _logger.Debug("{Message}", Nettoyer(message));

    /// <summary>Masque les chemins de profil utilisateur (C:\Users\NomUtilisateur\...).</summary>
    public static string Nettoyer(string? texte)
    {
        if (string.IsNullOrEmpty(texte))
        {
            return string.Empty;
        }

        return System.Text.RegularExpressions.Regex.Replace(texte, @"[A-Za-z]:\\Users\\[^\\]+", @"C:\Users\[utilisateur]");
    }

    public static void Fermer()
    {
        if (_logger is IDisposable d)
        {
            d.Dispose();
        }

        _logger = Serilog.Core.Logger.None;
        Log.CloseAndFlush();
    }
}
