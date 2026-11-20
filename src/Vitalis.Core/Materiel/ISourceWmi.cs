namespace Vitalis.Core.Materiel;

/// <summary>
/// Source de lecture WMI. L'implémentation réelle est <see cref="WmiSource"/> ; les tests utilisent des données simulées.
/// Chaque ligne est un dictionnaire nom de propriété → valeur brute (null si absente).
/// </summary>
public interface ISourceWmi
{
    /// <param name="espaceNoms">Ex. root\CIMV2.</param>
    /// <param name="classe">Ex. Win32_Processor.</param>
    /// <param name="proprietes">Seules ces propriétés sont lues (moins de données transférées).</param>
    /// <param name="filtre">Clause WHERE optionnelle, sans le mot-clé WHERE.</param>
    /// <exception cref="Exception">Classe absente, espace de noms inconnu ou erreur WMI.</exception>
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Requeter(
        string espaceNoms,
        string classe,
        IReadOnlyList<string> proprietes,
        string? filtre = null);
}
