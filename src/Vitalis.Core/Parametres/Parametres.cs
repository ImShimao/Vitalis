using System.Text.Json;

namespace Vitalis.Core.Parametres;

/// <summary>Préférences de l'utilisateur, enregistrées localement (JSON).</summary>
public sealed record Preferences(string Theme = "Systeme", bool ModeDiagnostic = false, bool NotificationsActives = true)
{
    public static readonly string[] ThemesPossibles = ["Systeme", "Clair", "Sombre"];

    /// <summary>Thème invalide ou vide : revient à « Systeme ». Aucun thème inconnu n'est accepté.</summary>
    public Preferences Normalisees() => this with
    {
        Theme = ThemesPossibles.Contains(Theme) ? Theme : "Systeme",
    };
}

public static class StockagePreferences
{
    public static Preferences Charger(string fichier)
    {
        try
        {
            if (!File.Exists(fichier))
            {
                return new Preferences();
            }

            return (JsonSerializer.Deserialize<Preferences>(File.ReadAllText(fichier)) ?? new Preferences()).Normalisees();
        }
        catch (Exception e) when (e is JsonException or IOException)
        {
            // Fichier corrompu : on repart des valeurs par défaut sans bloquer l'application.
            return new Preferences();
        }
    }

    public static void Enregistrer(string fichier, Preferences preferences)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(fichier)!);
        File.WriteAllText(fichier, JsonSerializer.Serialize(preferences.Normalisees()));
    }
}
