using static Vitalis.Core.Materiel.Conversions;

namespace Vitalis.Core.Materiel;

/// <summary>
/// Inventaire du matériel. Chaque famille est lue en parallèle, dans son propre bloc :
/// une erreur ou une classe absente rend seulement cette famille « indisponible ».
/// Sources (vérifiées dans la documentation Microsoft, voir fonction inventaire-materiel) :
/// Win32_Processor, Win32_BaseBoard, Win32_BIOS, Win32_PhysicalMemory, Win32_VideoController,
/// WmiMonitorID (root\wmi), MSFT_NetAdapter (root\StandardCimv2), Win32_SoundDevice.
/// </summary>
public sealed class InventaireMateriel
{
    private const string CimV2 = @"root\CIMV2";
    private const string Wmi = @"root\wmi";
    private const string StandardCimv2 = @"root\StandardCimv2";

    private readonly ISourceWmi _source;

    public InventaireMateriel(ISourceWmi source)
    {
        _source = source;
    }

    public async Task<RapportMateriel> AnalyserAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var processeur = Task.Run(() => Securiser(LireProcesseur, "Processeur"), ct);
        var carteMere = Task.Run(() => Securiser(LireCarteMere, "Carte mère"), ct);
        var bios = Task.Run(() => Securiser(LireBios, "BIOS"), ct);
        var memoire = Task.Run(() => Securiser(LireMemoire, "Mémoire"), ct);
        var gpu = Task.Run(() => Securiser(LireCartesGraphiques, "Cartes graphiques"), ct);
        var ecrans = Task.Run(() => Securiser(LireEcrans, "Écrans"), ct);
        var reseau = Task.Run(() => Securiser(LireReseau, "Réseau"), ct);
        var audio = Task.Run(() => Securiser(LireAudio, "Audio"), ct);

        await Task.WhenAll(processeur, carteMere, bios, memoire, gpu, ecrans, reseau, audio).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();

        return new RapportMateriel(
            processeur.Result, carteMere.Result, bios.Result, memoire.Result,
            gpu.Result, ecrans.Result, reseau.Result, audio.Result);
    }

    /// <summary>Filet de sécurité : une exception de lecture devient une section « indisponible » avec sa raison.</summary>
    private static Section<T> Securiser<T>(Func<Section<T>> lire, string famille)
    {
        try
        {
            return lire();
        }
        catch (Exception e)
        {
            return new Section<T>(Disponibilite.Indisponible, [], $"{famille} : lecture impossible ({e.GetType().Name} : {e.Message})");
        }
    }

    /// <summary>Calcule l'état d'une famille à partir de ses éléments et de ses champs essentiels.</summary>
    private static Section<T> Evaluer<T>(IReadOnlyList<T> elements, Func<T, bool> complet, string classe)
    {
        if (elements.Count == 0)
        {
            return new Section<T>(Disponibilite.Indisponible, [], $"Aucun élément renvoyé par {classe}.");
        }

        if (elements.All(complet))
        {
            return new Section<T>(Disponibilite.Disponible, elements);
        }

        return new Section<T>(Disponibilite.Partielle, elements, $"Certaines valeurs manquent ({classe}).");
    }

    private Section<Processeur> LireProcesseur()
    {
        var lignes = _source.Requeter(CimV2, "Win32_Processor",
            ["Name", "Manufacturer", "NumberOfCores", "NumberOfLogicalProcessors", "MaxClockSpeed", "L2CacheSize", "L3CacheSize"]);

        var elements = lignes.Select(l => new Processeur(
            Texte(l, "Name"),
            Texte(l, "Manufacturer"),
            (int?)Entier(l, "NumberOfCores"),
            (int?)Entier(l, "NumberOfLogicalProcessors"),
            (int?)Entier(l, "MaxClockSpeed"),
            (int?)Entier(l, "L2CacheSize"),
            (int?)Entier(l, "L3CacheSize"))).ToList();

        return Evaluer(elements, p => p.Nom is not null, "Win32_Processor");
    }

    private Section<CarteMere> LireCarteMere()
    {
        var lignes = _source.Requeter(CimV2, "Win32_BaseBoard", ["Manufacturer", "Product", "Version"]);

        var elements = lignes.Select(l => new CarteMere(
            Texte(l, "Manufacturer"), Texte(l, "Product"), Texte(l, "Version"))).ToList();

        return Evaluer(elements, c => c.Modele is not null, "Win32_BaseBoard");
    }

    private Section<Bios> LireBios()
    {
        var lignes = _source.Requeter(CimV2, "Win32_BIOS", ["Manufacturer", "SMBIOSBIOSVersion", "ReleaseDate"]);

        var elements = lignes.Select(l => new Bios(
            Texte(l, "Manufacturer"),
            Texte(l, "SMBIOSBIOSVersion"),
            DateCim(Texte(l, "ReleaseDate")))).ToList();

        return Evaluer(elements, b => b.Version is not null, "Win32_BIOS");
    }

    private Section<Memoire> LireMemoire()
    {
        var lignes = _source.Requeter(CimV2, "Win32_PhysicalMemory", ["DeviceLocator", "Capacity", "Speed", "Manufacturer"]);

        var elements = lignes.Select(l => new Memoire(
            Texte(l, "DeviceLocator"),
            Entier(l, "Capacity"),
            (int?)Entier(l, "Speed"),
            Texte(l, "Manufacturer"))).ToList();

        return Evaluer(elements, m => m.CapaciteOctets is not null, "Win32_PhysicalMemory");
    }

    private Section<CarteGraphique> LireCartesGraphiques()
    {
        var lignes = _source.Requeter(CimV2, "Win32_VideoController",
            ["Name", "AdapterRAM", "DriverVersion", "DriverDate", "CurrentHorizontalResolution", "CurrentVerticalResolution", "CurrentRefreshRate"]);

        var elements = lignes.Select(l => new CarteGraphique(
            Texte(l, "Name"),
            Entier(l, "AdapterRAM"),
            Texte(l, "DriverVersion"),
            DateCim(Texte(l, "DriverDate")),
            (int?)Entier(l, "CurrentHorizontalResolution"),
            (int?)Entier(l, "CurrentVerticalResolution"),
            (int?)Entier(l, "CurrentRefreshRate"))).ToList();

        return Evaluer(elements, g => g.Nom is not null, "Win32_VideoController");
    }

    private Section<Ecran> LireEcrans()
    {
        var lignes = _source.Requeter(Wmi, "WmiMonitorID", ["ManufacturerName", "UserFriendlyName"]);

        var elements = lignes.Select(l => new Ecran(
            DecoderUtf16(l.GetValueOrDefault("ManufacturerName")),
            DecoderUtf16(l.GetValueOrDefault("UserFriendlyName")))).ToList();

        return Evaluer(elements, e => e.Nom is not null, "WmiMonitorID");
    }

    private Section<AdaptateurReseau> LireReseau()
    {
        // Win32_NetworkAdapter est déprécié par Microsoft : MSFT_NetAdapter est la classe recommandée.
        var lignes = _source.Requeter(StandardCimv2, "MSFT_NetAdapter",
            ["Name", "InterfaceDescription"], "HardwareInterface = TRUE");

        var elements = lignes.Select(l => new AdaptateurReseau(
            Texte(l, "Name"), Texte(l, "InterfaceDescription"))).ToList();

        return Evaluer(elements, a => a.Nom is not null, "MSFT_NetAdapter");
    }

    private Section<PeripheriqueAudio> LireAudio()
    {
        var lignes = _source.Requeter(CimV2, "Win32_SoundDevice", ["Name", "Manufacturer", "Status"]);

        var elements = lignes.Select(l => new PeripheriqueAudio(
            Texte(l, "Name"), Texte(l, "Manufacturer"), Texte(l, "Status"))).ToList();

        return Evaluer(elements, a => a.Nom is not null, "Win32_SoundDevice");
    }
}
