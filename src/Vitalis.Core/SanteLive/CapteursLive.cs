using LibreHardwareMonitor.Hardware;

namespace Vitalis.Core.SanteLive;

public enum TypeMesure
{
    Temperature,
    Charge,
    Frequence,
    Ventilateur,
    Puissance,
}

public enum Categorie
{
    Processeur,
    CarteGraphique,
    Memoire,
    CarteMere,
    Stockage,
    Autre,
}

public enum NiveauMesure
{
    Normal,
    Attention,
    Critique,
}

/// <summary>Une valeur de capteur. Valeur null : capteur présent mais non lu (jamais de valeur inventée).</summary>
public sealed record Mesure(string Capteur, TypeMesure Type, float? Valeur);

public sealed record ComposantLive(string Nom, Categorie Categorie, IReadOnlyList<Mesure> Mesures);

/// <summary>
/// Lecture des capteurs en temps réel par LibreHardwareMonitorLib 0.9.6 (licence MPL-2.0, utilisée sans modification).
/// Les températures du processeur passent par le pilote PawnIO (signé, installé à part) : sans lui, elles restent
/// indisponibles et le reste fonctionne (cartes graphiques NVIDIA/AMD/Intel, disques, mémoire). Voir ADR-016.
/// </summary>
public sealed class CapteursLive : IDisposable
{
    private readonly Computer _ordinateur = new()
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMemoryEnabled = true,
        IsMotherboardEnabled = true,
        IsStorageEnabled = true,
    };

    private bool _ouvert;

    public void Ouvrir()
    {
        _ordinateur.Open();
        _ouvert = true;
    }

    public IReadOnlyList<ComposantLive> Lire()
    {
        if (!_ouvert)
        {
            Ouvrir();
        }

        var resultat = new List<ComposantLive>();
        foreach (var materiel in _ordinateur.Hardware)
        {
            Ajouter(materiel, resultat);
        }

        return resultat;
    }

    private static void Ajouter(IHardware materiel, List<ComposantLive> resultat)
    {
        materiel.Update();
        var mesures = materiel.Sensors.Select(Convertir).Where(m => m is not null).Select(m => m!).ToList();
        foreach (var sous in materiel.SubHardware)
        {
            // Puces de la carte mère (Super I/O) : leurs capteurs sont rattachés à la carte mère.
            sous.Update();
            mesures.AddRange(sous.Sensors.Select(Convertir).Where(m => m is not null).Select(m => m!));
        }

        if (mesures.Count > 0)
        {
            resultat.Add(new ComposantLive(materiel.Name, CategorieDe(materiel.HardwareType), mesures));
        }
    }

    private static Mesure? Convertir(ISensor s)
    {
        TypeMesure? type = s.SensorType switch
        {
            SensorType.Temperature => TypeMesure.Temperature,
            SensorType.Load => TypeMesure.Charge,
            SensorType.Clock => TypeMesure.Frequence,
            SensorType.Fan => TypeMesure.Ventilateur,
            SensorType.Power => TypeMesure.Puissance,
            _ => null,
        };

        // Valeur NaN ou 0 °C : capteur non lu (LibreHardwareMonitor renvoie ces valeurs sans pilote).
        var valeur = s.Value is { } v && !float.IsNaN(v) && !(type == TypeMesure.Temperature && v <= 0) ? v : (float?)null;
        return type is null ? null : new Mesure(s.Name, type.Value, valeur);
    }

    private static Categorie CategorieDe(HardwareType t) => t switch
    {
        HardwareType.Cpu => Categorie.Processeur,
        HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel => Categorie.CarteGraphique,
        HardwareType.Memory => Categorie.Memoire,
        HardwareType.Motherboard or HardwareType.SuperIO => Categorie.CarteMere,
        HardwareType.Storage => Categorie.Stockage,
        _ => Categorie.Autre,
    };

    public void Dispose()
    {
        if (_ouvert)
        {
            _ordinateur.Close();
            _ouvert = false;
        }
    }
}

/// <summary>Règles de lecture : seuils de température par composant et valeurs clés. Sans dépendance au matériel (testable).</summary>
public static class AnalyseCapteurs
{
    /// <summary>
    /// Seuils (°C) attention / critique. Processeurs : la plupart limitent leur fréquence vers 95-100 °C.
    /// Cartes graphiques : vers 83-90 °C. SSD et disques : au-delà de 60-70 °C la durée de vie baisse.
    /// </summary>
    public static (float Attention, float Critique) Seuils(Categorie c) => c switch
    {
        Categorie.Processeur => (85, 95),
        Categorie.CarteGraphique => (80, 90),
        Categorie.Stockage => (55, 65),
        _ => (70, 85),
    };

    public static NiveauMesure Niveau(Categorie c, float? temperature)
    {
        if (temperature is not { } t)
        {
            return NiveauMesure.Normal;
        }

        var (attention, critique) = Seuils(c);
        return t >= critique ? NiveauMesure.Critique : t >= attention ? NiveauMesure.Attention : NiveauMesure.Normal;
    }

    /// <summary>Température la plus représentative : « Package » / « Core Max » / « GPU Core », sinon la plus haute lue.</summary>
    public static float? TemperaturePrincipale(ComposantLive c)
    {
        var temps = c.Mesures.Where(m => m.Type == TypeMesure.Temperature && m.Valeur is not null).ToList();
        if (temps.Count == 0)
        {
            return null;
        }

        string[] preferes = ["Package", "Core Max", "Tctl/Tdie", "GPU Core", "Composite", "Temperature"];
        foreach (var nom in preferes)
        {
            var m = temps.FirstOrDefault(t => t.Capteur.Contains(nom, StringComparison.OrdinalIgnoreCase));
            if (m is not null)
            {
                return m.Valeur;
            }
        }

        return temps.Max(t => t.Valeur);
    }

    /// <summary>Charge globale : « Total » (processeur), « Memory » (mémoire), « GPU Core » (carte graphique), sinon la plus haute.</summary>
    public static float? ChargePrincipale(ComposantLive c)
    {
        var charges = c.Mesures.Where(m => m.Type == TypeMesure.Charge && m.Valeur is not null).ToList();
        if (charges.Count == 0)
        {
            return null;
        }

        string[] preferes = ["CPU Total", "Total", "Memory", "GPU Core", "D3D 3D"];
        foreach (var nom in preferes)
        {
            var m = charges.FirstOrDefault(t => t.Capteur.Equals(nom, StringComparison.OrdinalIgnoreCase))
                ?? charges.FirstOrDefault(t => t.Capteur.Contains(nom, StringComparison.OrdinalIgnoreCase));
            if (m is not null)
            {
                return m.Valeur;
            }
        }

        return charges.Max(t => t.Valeur);
    }

    /// <summary>Vrai si un processeur est détecté sans aucune température lue : cas typique du pilote PawnIO absent ou bloqué.</summary>
    public static bool TemperaturesProcesseurManquantes(IReadOnlyList<ComposantLive> composants) =>
        composants.Where(c => c.Categorie == Categorie.Processeur)
            .All(c => !c.Mesures.Any(m => m.Type == TypeMesure.Temperature && m.Valeur is not null));
}
