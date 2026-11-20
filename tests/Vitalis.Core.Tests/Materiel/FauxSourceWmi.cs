using Vitalis.Core.Materiel;

namespace Vitalis.Core.Tests.Materiel;

/// <summary>Source WMI simulée : données par classe, classes absentes qui lèvent une erreur comme WMI.</summary>
public sealed class FauxSourceWmi : ISourceWmi
{
    private readonly Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> _donnees = new();
    private readonly HashSet<string> _absentes = new();

    /// <summary>Appels reçus : espace de noms, classe, filtre.</summary>
    public List<(string Espace, string Classe, string? Filtre)> Appels { get; } = new();

    public FauxSourceWmi Avec(string classe, params IReadOnlyDictionary<string, object?>[] lignes)
    {
        _donnees[classe] = lignes;
        return this;
    }

    public FauxSourceWmi SansClasse(string classe)
    {
        _absentes.Add(classe);
        return this;
    }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Requeter(
        string espaceNoms,
        string classe,
        IReadOnlyList<string> proprietes,
        string? filtre = null)
    {
        Appels.Add((espaceNoms, classe, filtre));

        if (_absentes.Contains(classe))
        {
            throw new InvalidOperationException($"Invalid class {classe}");
        }

        return _donnees.TryGetValue(classe, out var lignes) ? lignes : [];
    }

    public static Dictionary<string, object?> Ligne(params (string Cle, object? Valeur)[] champs)
    {
        return champs.ToDictionary(c => c.Cle, c => c.Valeur, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Codes UTF-16 comme les renvoie WmiMonitorID (tableau de ushort terminé par des zéros).</summary>
    public static ushort[] Codes(string texte, int longueur = 16)
    {
        var codes = new ushort[longueur];
        for (var i = 0; i < texte.Length && i < longueur; i++)
        {
            codes[i] = texte[i];
        }

        return codes;
    }

    /// <summary>Une machine complète et plausible, utilisée par plusieurs tests.</summary>
    public static FauxSourceWmi MachineComplete()
    {
        return new FauxSourceWmi()
            .Avec("Win32_Processor", Ligne(
                ("Name", "Intel(R) Core(TM) i7-12700"), ("Manufacturer", "GenuineIntel"),
                ("NumberOfCores", (uint)12), ("NumberOfLogicalProcessors", (uint)20),
                ("MaxClockSpeed", (uint)2100), ("L2CacheSize", (uint)12288), ("L3CacheSize", (uint)25600)))
            .Avec("Win32_BaseBoard", Ligne(
                ("Manufacturer", "ASUSTeK COMPUTER INC."), ("Product", "PRIME B660-PLUS"), ("Version", "Rev 1.xx")))
            .Avec("Win32_BIOS", Ligne(
                ("Manufacturer", "American Megatrends Inc."), ("SMBIOSBIOSVersion", "1.20"),
                ("ReleaseDate", "20230115000000.000000+000")))
            .Avec("Win32_PhysicalMemory",
                Ligne(("DeviceLocator", "DIMM 1"), ("Capacity", (ulong)8589934592), ("Speed", (uint)3200), ("Manufacturer", "Kingston")),
                Ligne(("DeviceLocator", "DIMM 2"), ("Capacity", (ulong)8589934592), ("Speed", (uint)3200), ("Manufacturer", "Kingston")))
            .Avec("Win32_VideoController", Ligne(
                ("Name", "NVIDIA GeForce RTX 3060"), ("AdapterRAM", (uint)4293918720),
                ("DriverVersion", "31.0.15.3623"), ("DriverDate", "20240510000000.000000+000"),
                ("CurrentHorizontalResolution", (uint)1920), ("CurrentVerticalResolution", (uint)1080),
                ("CurrentRefreshRate", (uint)144)))
            .Avec("WmiMonitorID", Ligne(
                ("ManufacturerName", Codes("DEL")), ("UserFriendlyName", Codes("DELL U2720Q"))))
            .Avec("MSFT_NetAdapter", Ligne(
                ("Name", "Wi-Fi"), ("InterfaceDescription", "Intel(R) Wi-Fi 6 AX201 160MHz")))
            .Avec("Win32_SoundDevice", Ligne(
                ("Name", "Realtek High Definition Audio"), ("Manufacturer", "Realtek"), ("Status", "OK")));
    }
}
