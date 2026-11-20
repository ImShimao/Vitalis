namespace Vitalis.Core.MisesAJour;

/// <summary>
/// Télécharge et installe les mises à jour Windows en attente (Windows Update Agent, COM), après confirmation de l'utilisateur.
/// Demande les droits administrateur (Vitalis est lancé en administrateur, voir ADR-015).
/// Sources (doc Microsoft, vérifiée le 2026-11-20) : IUpdateSession::CreateUpdateDownloader / CreateUpdateInstaller,
/// IUpdate::AcceptEula, IUpdate::InstallationBehavior.CanRequestUserInput, IInstallationResult (ResultCode, RebootRequired).
/// </summary>
public static class InstallateurWindowsUpdate
{
    private const string Critere = "IsInstalled=0 and IsHidden=0";

    /// <param name="etape">Reçoit une phrase à afficher (recherche, téléchargement, installation).</param>
    /// <param name="identifiants">UpdateID à installer (un pilote précis, par exemple). Null : toutes les mises à jour en attente.</param>
    public static ResultatInstallation Installer(IProgress<string>? etape = null, CancellationToken ct = default, IReadOnlyCollection<string>? identifiants = null)
    {
        var type = Type.GetTypeFromProgID("Microsoft.Update.Session")
            ?? throw new InvalidOperationException("Windows Update Agent n'est pas disponible.");
        var typeCollection = Type.GetTypeFromProgID("Microsoft.Update.UpdateColl")
            ?? throw new InvalidOperationException("Windows Update Agent n'est pas disponible.");

        dynamic session = Activator.CreateInstance(type)!;
        session.ClientApplicationID = "Vitalis";

        etape?.Report("Recherche des mises à jour…");
        dynamic resultat = session.CreateUpdateSearcher().Search(Critere);
        dynamic trouvees = resultat.Updates;
        dynamic aInstaller = Activator.CreateInstance(typeCollection)!;
        var ignorees = 0;

        int n = trouvees.Count;
        for (var i = 0; i < n; i++)
        {
            dynamic u = trouvees.Item(i);
            if (identifiants is not null && !identifiants.Contains((string)u.Identity.UpdateID, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            // Une mise à jour qui ouvre sa propre fenêtre ne peut pas s'installer sans surveillance : laissée à Windows Update.
            if ((bool)u.InstallationBehavior.CanRequestUserInput)
            {
                ignorees++;
                continue;
            }

            if (!(bool)u.EulaAccepted)
            {
                u.AcceptEula();
            }

            aInstaller.Add(u);
        }

        int total = aInstaller.Count;
        if (total == 0)
        {
            return new ResultatInstallation(ignorees == 0, ignorees == 0
                ? "Aucune mise à jour à installer."
                : "Les mises à jour restantes demandent une action : ouvrez Windows Update.");
        }

        ct.ThrowIfCancellationRequested();
        etape?.Report($"Téléchargement de {total} mise(s) à jour… (peut prendre plusieurs minutes)");
        dynamic telechargeur = session.CreateUpdateDownloader();
        telechargeur.Updates = aInstaller;
        telechargeur.Download();

        ct.ThrowIfCancellationRequested();
        etape?.Report($"Installation de {total} mise(s) à jour… Ne fermez pas Vitalis.");
        dynamic installateur = session.CreateUpdateInstaller();
        installateur.Updates = aInstaller;
        dynamic installation = installateur.Install();

        int code = installation.ResultCode;
        bool redemarrage = installation.RebootRequired;
        var suffixe = ignorees > 0 ? $" {ignorees} mise(s) à jour à faire depuis Windows Update." : string.Empty;
        return code switch
        {
            2 => new ResultatInstallation(true, (redemarrage ? $"{total} installée(s). Redémarrez le PC pour terminer." : $"{total} installée(s).") + suffixe, redemarrage),
            3 => new ResultatInstallation(true, $"Installées avec des erreurs : consultez l'historique Windows Update.{suffixe}", redemarrage),
            4 => new ResultatInstallation(false, "Échec de l'installation : consultez l'historique Windows Update.", redemarrage),
            5 => new ResultatInstallation(false, "Installation interrompue."),
            _ => new ResultatInstallation(false, $"Résultat inattendu (code {code})."),
        };
    }
}
