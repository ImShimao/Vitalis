using System.Security.Principal;

namespace Vitalis.Core.Disques;

/// <summary>Droits de l'utilisateur courant. Lecture seule : rien n'est élevé ici.</summary>
public static class Droits
{
    /// <summary>Vrai si le processus s'exécute avec les droits administrateur (jeton élevé).</summary>
    public static bool EstAdministrateur()
    {
        using var identite = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identite).IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>Message affiché avant toute demande d'élévation : ce qui change, et ce qui ne change pas.</summary>
    public const string MessageElevation =
        "Les compteurs SMART (température, usure, heures de marche, erreurs) ne sont lisibles qu'en administrateur. " +
        "Relancer Vitalis en administrateur demande la confirmation de Windows (UAC). " +
        "Vitalis ne modifie rien sur le disque : il lit seulement ces compteurs.";
}
