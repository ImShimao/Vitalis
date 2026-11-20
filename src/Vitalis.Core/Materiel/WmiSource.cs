using System.Management;

namespace Vitalis.Core.Materiel;

/// <summary>Lecture réelle via System.Management (WMI). Une requête par classe, propriétés explicites.</summary>
public sealed class WmiSource : ISourceWmi
{
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Requeter(
        string espaceNoms,
        string classe,
        IReadOnlyList<string> proprietes,
        string? filtre = null)
    {
        var requete = $"SELECT {string.Join(", ", proprietes)} FROM {classe}";
        if (!string.IsNullOrWhiteSpace(filtre))
        {
            requete += $" WHERE {filtre}";
        }

        var lignes = new List<IReadOnlyDictionary<string, object?>>();
        using var recherche = new ManagementObjectSearcher(espaceNoms, requete);
        using var resultats = recherche.Get();

        foreach (ManagementObject objet in resultats)
        {
            using (objet)
            {
                var ligne = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var propriete in proprietes)
                {
                    ligne[propriete] = objet[propriete];
                }

                lignes.Add(ligne);
            }
        }

        return lignes;
    }
}
