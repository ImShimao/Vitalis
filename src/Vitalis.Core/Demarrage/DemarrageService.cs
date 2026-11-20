using System.Text.Json;

namespace Vitalis.Core.Demarrage;

/// <summary>Une entrée de démarrage : valeur d'une clé Run (Microsoft : « Run and RunOnce registry keys »).</summary>
public sealed record EntreeDemarrage(string Nom, string Commande, string Emplacement);

/// <summary>Accès aux clés Run. Implémentation réelle : <see cref="RegistreDemarrageWindows"/>.</summary>
public interface IRegistreDemarrage
{
    IReadOnlyList<EntreeDemarrage> Lister();
    void Ecrire(EntreeDemarrage entree);
    void Supprimer(EntreeDemarrage entree);
}

/// <summary>
/// Désactivation réversible : l'entrée est sauvegardée (fichier JSON) puis retirée de la clé Run.
/// Restaurer réécrit exactement la commande sauvegardée. Aucune désactivation sans confirmation explicite.
/// </summary>
public sealed class DemarrageService
{
    private readonly IRegistreDemarrage _registre;
    private readonly string _fichierSauvegarde;

    public DemarrageService(IRegistreDemarrage registre, string fichierSauvegarde)
    {
        _registre = registre;
        _fichierSauvegarde = fichierSauvegarde;
    }

    public IReadOnlyList<EntreeDemarrage> Lister() => _registre.Lister();

    public IReadOnlyList<EntreeDemarrage> Desactivees() =>
        File.Exists(_fichierSauvegarde)
            ? JsonSerializer.Deserialize<List<EntreeDemarrage>>(File.ReadAllText(_fichierSauvegarde)) ?? []
            : [];

    /// <exception cref="InvalidOperationException">Confirmation absente.</exception>
    public void Desactiver(EntreeDemarrage entree, bool confirme)
    {
        if (!confirme)
        {
            throw new InvalidOperationException("Désactivation sans confirmation refusée.");
        }

        var sauvegarde = Desactivees().Where(e => !(e.Nom == entree.Nom && e.Emplacement == entree.Emplacement)).ToList();
        sauvegarde.Add(entree);
        Enregistrer(sauvegarde);
        _registre.Supprimer(entree);
    }

    public void Restaurer(EntreeDemarrage entree)
    {
        var restantes = Desactivees().Where(e => !(e.Nom == entree.Nom && e.Emplacement == entree.Emplacement)).ToList();
        _registre.Ecrire(entree);
        Enregistrer(restantes);
    }

    private void Enregistrer(IReadOnlyList<EntreeDemarrage> entrees)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_fichierSauvegarde)!);
        File.WriteAllText(_fichierSauvegarde, JsonSerializer.Serialize(entrees));
    }
}
