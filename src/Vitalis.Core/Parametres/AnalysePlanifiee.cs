using Vitalis.Core.TableauDeBord;

namespace Vitalis.Core.Parametres;

/// <summary>
/// Analyse planifiée : une tâche du Planificateur de tâches lance « Vitalis.exe --analyse-planifiee » chaque semaine,
/// avec les droits les plus élevés (Vitalis demande l'administrateur) et sans fenêtre. Elle ne fait que lire, puis notifie.
/// API COM Schedule.Service (doc Microsoft « Task Scheduler Scripting Objects », vérifiée le 2026-11-20) :
/// déclencheur hebdomadaire (type 3), action exécutable (type 0), RunLevel 1 = le plus élevé,
/// StartWhenAvailable : rattrapée si le PC était éteint. Enregistrement : TASK_CREATE_OR_UPDATE (6), jeton interactif (3).
/// </summary>
public static class AnalysePlanifiee
{
    public const string Argument = "--analyse-planifiee";
    public const string NomTache = "Vitalis - analyse hebdomadaire";

    private static dynamic Service()
    {
        var type = Type.GetTypeFromProgID("Schedule.Service")
            ?? throw new InvalidOperationException("Le Planificateur de tâches n'est pas disponible.");
        dynamic service = Activator.CreateInstance(type)!;
        service.Connect();
        return service;
    }

    public static bool Existe()
    {
        try
        {
            Service().GetFolder("\\").GetTask(NomTache);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Crée ou remplace la tâche : chaque lundi à 12 h 00, rattrapée au prochain démarrage si le PC était éteint.</summary>
    public static void Creer(string executable)
    {
        var service = Service();
        dynamic def = service.NewTask(0);
        def.RegistrationInfo.Description = "Analyse Vitalis en lecture seule, puis notification du résultat. Créée depuis les paramètres de Vitalis.";
        def.Principal.RunLevel = 1;
        def.Principal.LogonType = 3;
        def.Settings.StartWhenAvailable = true;
        def.Settings.DisallowStartIfOnBatteries = false;
        def.Settings.StopIfGoingOnBatteries = false;
        def.Settings.ExecutionTimeLimit = "PT30M";

        dynamic declencheur = def.Triggers.Create(3);
        declencheur.StartBoundary = ProchainLundi(DateTime.Now).ToString("yyyy-MM-dd'T'12:00:00");
        declencheur.DaysOfWeek = 2; // lundi (bit 2, doc WeeklyTrigger.DaysOfWeek)
        declencheur.WeeksInterval = 1;

        dynamic action = def.Actions.Create(0);
        action.Path = executable;
        action.Arguments = Argument;
        action.WorkingDirectory = Path.GetDirectoryName(executable);

        service.GetFolder("\\").RegisterTaskDefinition(NomTache, def, 6, null, null, 3);
    }

    public static void Supprimer()
    {
        if (Existe())
        {
            Service().GetFolder("\\").DeleteTask(NomTache, 0);
        }
    }

    public static DateTime ProchainLundi(DateTime depuis)
    {
        var jours = ((int)DayOfWeek.Monday - (int)depuis.DayOfWeek + 7) % 7;
        return depuis.Date.AddDays(jours == 0 ? 7 : jours);
    }

    /// <summary>Texte de la notification : score et ce qui demande une action. Jamais de donnée personnelle.</summary>
    public static (string Titre, string Texte) Notification(ResultatTableauDeBord r, int? pilotesDisponibles)
    {
        var titre = r.Score is { } s ? $"Vitalis : score {s}/100" : "Vitalis : analyse terminée";
        var points = new List<string>();
        var windows = r.MisesAJour?.EnAttente.Elements.Count ?? 0;
        var logiciels = r.MisesAJour?.Logiciels.Elements.Count ?? 0;
        if (windows > 0)
        {
            points.Add($"{windows} mise{(windows > 1 ? "s" : string.Empty)} à jour Windows");
        }

        if (logiciels > 0)
        {
            points.Add($"{logiciels} logiciel{(logiciels > 1 ? "s" : string.Empty)} à mettre à jour");
        }

        if (pilotesDisponibles is > 0 and var p)
        {
            points.Add($"{p} pilote{(p > 1 ? "s" : string.Empty)} plus récent{(p > 1 ? "s" : string.Empty)}");
        }

        var texte = points.Count == 0
            ? "Tout est à jour. Rien à faire."
            : string.Join(" · ", points) + ". Ouvrez Vitalis pour les installer.";
        return (titre, texte);
    }
}
