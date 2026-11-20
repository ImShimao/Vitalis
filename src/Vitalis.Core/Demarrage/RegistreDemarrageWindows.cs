using Microsoft.Win32;

namespace Vitalis.Core.Demarrage;

/// <summary>
/// Clés Run réelles. Lecture : HKCU et HKLM. Écriture : HKCU seulement (HKLM demande les droits administrateur
/// et n'est jamais modifiée par Vitalis).
/// Chemin (Microsoft, « Run and RunOnce registry keys ») : SOFTWARE\Microsoft\Windows\CurrentVersion\Run.
/// </summary>
public sealed class RegistreDemarrageWindows : IRegistreDemarrage
{
    private const string Chemin = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public IReadOnlyList<EntreeDemarrage> Lister()
    {
        var liste = new List<EntreeDemarrage>();
        LireCle(Registry.CurrentUser, "HKCU", liste);
        LireCle(Registry.LocalMachine, "HKLM", liste);
        return liste;
    }

    private static void LireCle(RegistryKey racine, string nomRacine, List<EntreeDemarrage> liste)
    {
        using var cle = racine.OpenSubKey(Chemin, writable: false);
        if (cle is null)
        {
            return;
        }

        foreach (var nom in cle.GetValueNames())
        {
            var commande = cle.GetValue(nom) as string ?? string.Empty;
            liste.Add(new EntreeDemarrage(nom, commande, $"{nomRacine}\\{Chemin}"));
        }
    }

    public void Ecrire(EntreeDemarrage entree)
    {
        if (!entree.Emplacement.StartsWith("HKCU", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Seul HKCU peut être modifié.");
        }

        using var cle = Registry.CurrentUser.CreateSubKey(Chemin, writable: true);
        cle.SetValue(entree.Nom, entree.Commande, RegistryValueKind.String);
    }

    public void Supprimer(EntreeDemarrage entree)
    {
        if (!entree.Emplacement.StartsWith("HKCU", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Seul HKCU peut être modifié.");
        }

        using var cle = Registry.CurrentUser.OpenSubKey(Chemin, writable: true);
        cle?.DeleteValue(entree.Nom, throwOnMissingValue: false);
    }
}
