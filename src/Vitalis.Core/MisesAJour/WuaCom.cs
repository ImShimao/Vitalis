namespace Vitalis.Core.MisesAJour;

/// <summary>
/// Implémentation COM de Windows Update Agent (ProgID Microsoft.Update.Session), appelée en liaison tardive (dynamic).
/// Sources (vérifiées dans la doc Microsoft, 2026-11-20) : IUpdateSearcher::Search (critères IsInstalled, IsHidden, Type),
/// IUpdateSearcher::QueryHistory, IUpdate (Title, KBArticleIDs, MsrcSeverity, IsMandatory), IUpdateHistoryEntry (Date, ResultCode).
/// ResultCode : 2 = réussi, 3 = réussi avec erreurs (doc OperationResultCode).
/// </summary>
public sealed class WuaCom : IServiceWua
{
    private const string CritereEnAttente = "IsInstalled=0 and IsHidden=0";
    private const string CritereLogiciel = CritereEnAttente + " and Type='Software'";
    private const string CritereAutoPilote = CritereEnAttente + " and Type='Driver'";

    public IReadOnlyList<MiseAJour> RechercherEnAttente()
    {
        var type = Type.GetTypeFromProgID("Microsoft.Update.Session")
            ?? throw new InvalidOperationException("Windows Update Agent (Microsoft.Update.Session) n'est pas disponible.");

        dynamic session = Activator.CreateInstance(type)!;
        object recherche = session.CreateUpdateSearcher();

        // Une recherche par type : la séparation logiciel / pilote se fait par le critère, pas par le contenu.
        var pilotes = ListerMises(recherche, CritereAutoPilote, pilote: true);
        var logiciels = ListerMises(recherche, CritereLogiciel, pilote: false);

        return logiciels.Concat(pilotes).ToList();
    }

    /// <remarks>Le paramètre est typé object : un argument dynamic rendrait le résultat dynamic (Concat échoue alors).</remarks>
    private static IReadOnlyList<MiseAJour> ListerMises(object recherche, string critere, bool pilote)
    {
        dynamic resultat = ((dynamic)recherche).Search(critere);
        dynamic updates = resultat.Updates;
        int nombre = updates.Count;

        var liste = new List<MiseAJour>(nombre);
        for (var i = 0; i < nombre; i++)
        {
            dynamic u = updates.Item(i);
            liste.Add(new MiseAJour(
                Titre: (string?)u.Title ?? "Sans titre",
                Kb: LireKb(u.KBArticleIDs),
                Gravite: string.IsNullOrWhiteSpace((string?)u.MsrcSeverity) ? null : (string)u.MsrcSeverity,
                Obligatoire: (bool)u.IsMandatory,
                Pilote: pilote));
        }

        return liste;
    }

    private static IReadOnlyList<string> LireKb(dynamic? collection)
    {
        if (collection is null)
        {
            return [];
        }

        int nombre = collection.Count;
        var kb = new List<string>(nombre);
        for (var i = 0; i < nombre; i++)
        {
            kb.Add("KB" + (string)collection.Item(i));
        }

        return kb;
    }

    public IReadOnlyList<EntreeHistorique> Historique(int nombreMax)
    {
        var type = Type.GetTypeFromProgID("Microsoft.Update.Session")
            ?? throw new InvalidOperationException("Windows Update Agent (Microsoft.Update.Session) n'est pas disponible.");

        dynamic session = Activator.CreateInstance(type)!;
        dynamic recherche = session.CreateUpdateSearcher();

        int total = recherche.GetTotalHistoryCount();
        int nombre = Math.Min(total, nombreMax);
        if (nombre <= 0)
        {
            return [];
        }

        dynamic entrees = recherche.QueryHistory(0, nombre);
        int lues = entrees.Count;

        var liste = new List<EntreeHistorique>(lues);
        for (var i = 0; i < lues; i++)
        {
            dynamic e = entrees.Item(i);
            int code = (int)e.ResultCode;
            liste.Add(new EntreeHistorique(
                Date: (DateTime?)e.Date,
                Titre: (string?)e.Title,
                Reussie: code == 2 || code == 3));
        }

        return liste;
    }
}
