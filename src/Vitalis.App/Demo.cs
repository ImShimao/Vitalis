using Vitalis.Core.Demarrage;
using Vitalis.Core.Disques;
using Vitalis.Core.Materiel;
using Vitalis.Core.MisesAJour;
using Vitalis.Core.Performances;
using Vitalis.Core.Pilotes;
using Vitalis.Core.SanteLive;
using Vitalis.Core.TableauDeBord;

namespace Vitalis.App;

/// <summary>
/// Mode démo (argument --demo) : un PC fictif pour les captures d'écran du README. Aucune donnée du vrai PC n'est lue,
/// et rien n'est écrit dans les fichiers de l'utilisateur (historique des performances dans un dossier temporaire).
/// Les vraies analyses tournent sur ces fausses sources : les pages affichent exactement ce qu'elles afficheraient.
/// </summary>
public static class Demo
{
    public static bool Actif { get; private set; }

    /// <summary>Page ouverte au lancement (argument --page=cle), pour les captures.</summary>
    public static string? PageInitiale { get; private set; }

    public static string Dossier { get; } = Path.Combine(Path.GetTempPath(), "Vitalis-demo");

    public static void Activer(string[] args)
    {
        Actif = args.Contains("--demo");
        PageInitiale = args.FirstOrDefault(a => a.StartsWith("--page=", StringComparison.Ordinal))?["--page=".Length..];
        if (!Actif)
        {
            return;
        }

        var maintenant = DateTime.Now;
        var wmi = Wmi();
        var tableau = new TableauDeBordService(new InventaireMateriel(wmi), new AnalyseDisques(wmi),
            new AnalyseMisesAJour(new FausseWua(maintenant), new FauxWinget())).AnalyserAsync().GetAwaiter().GetResult();
        EtatAnalyse.Tableau.Definir(tableau, maintenant);
        EtatAnalyse.Repartir(tableau);

        var installes = new AnalysePilotes(wmi).Analyser(maintenant);
        var disponibles = CataloguePilotes.Comparer(PilotesProposes(), installes.Pilotes.Elements);
        EtatAnalyse.Pilotes.Definir(new ResultatPilotes(installes,
            new Section<ComparaisonPilote>(Disponibilite.Disponible, disponibles), tableau.Materiel), maintenant);

        Directory.CreateDirectory(Dossier);
        var historique = Path.Combine(Dossier, "performances.json");
        File.Delete(historique);
        HistoriquePerformances.Ajouter(historique, Performances(maintenant.AddDays(-14), 1.0));
        HistoriquePerformances.Ajouter(historique, Performances(maintenant.AddMinutes(-3), 1.06));
    }

    private static ResultatBenchmark Mesure(string composant, double valeur, string unite, double score, string methode) =>
        new(composant, valeur.ToString("0.0", System.Globalization.CultureInfo.CurrentCulture), unite, score, methode,
            "Comparaison sur ce PC seulement.", true);

    private static AnalysePerformance Performances(DateTime date, double f) => new(date,
    [
        Mesure("Processeur", 11.8 / f, "ms", 84.7 * f, "Crible d'Ératosthène jusqu'à 2 000 000, médiane de 3 exécutions."),
        Mesure("Mémoire", 24.6 * f, "Go/s", 246 * f, "Copie de 256 Mo en mémoire, médiane de 3 exécutions."),
        Mesure("Disque (écriture)", 2650 * f, "Mo/s", 265 * f, "Écriture séquentielle de 256 Mo, cache contourné."),
        Mesure("Disque (lecture)", 3420 * f, "Mo/s", 342 * f, "Lecture séquentielle de 256 Mo."),
        new("Carte graphique", "9,9", "indice WinSAT", 99, "Indice officiel de Windows (WinSAT, D3DScore).", "Plafonné à 9,9 par Windows.", true),
    ]);

    /// <summary>Capteurs de la page Santé live : valeurs qui varient un peu à chaque lecture.</summary>
    public static IReadOnlyList<ComposantLive> Capteurs()
    {
        var t = DateTime.Now.Second / 60.0 * Math.PI * 2;
        float V(double baseValeur, double amplitude) => (float)(baseValeur + amplitude * Math.Sin(t + baseValeur));
        return
        [
            new("AMD Ryzen 7 7800X3D", Categorie.Processeur,
            [
                new Mesure("Core (Tctl/Tdie)", TypeMesure.Temperature, V(58, 4)),
                new Mesure("CPU Total", TypeMesure.Charge, V(23, 8)),
                new Mesure("Core #1", TypeMesure.Frequence, V(4850, 120)),
                new Mesure("Package", TypeMesure.Puissance, V(48, 6)),
            ]),
            new("NVIDIA GeForce RTX 4070", Categorie.CarteGraphique,
            [
                new Mesure("GPU Core", TypeMesure.Temperature, V(47, 3)),
                new Mesure("GPU Hot Spot", TypeMesure.Temperature, V(56, 3)),
                new Mesure("GPU Core", TypeMesure.Charge, V(12, 6)),
                new Mesure("GPU Core", TypeMesure.Frequence, V(2475, 60)),
                new Mesure("GPU Fan", TypeMesure.Ventilateur, V(1180, 40)),
                new Mesure("GPU Package", TypeMesure.Puissance, V(38, 5)),
            ]),
            new("Total Memory", Categorie.Memoire, [new Mesure("Memory", TypeMesure.Charge, V(41, 1))]),
            new("MSI MAG B650 TOMAHAWK WIFI (MS-7D75)", Categorie.CarteMere,
            [
                new Mesure("System", TypeMesure.Temperature, V(36, 1)),
                new Mesure("CPU Fan", TypeMesure.Ventilateur, V(980, 30)),
                new Mesure("System Fan #1", TypeMesure.Ventilateur, V(760, 20)),
            ]),
            new("Samsung SSD 990 PRO 1TB", Categorie.Stockage,
            [
                new Mesure("Composite Temperature", TypeMesure.Temperature, V(42, 2)),
                new Mesure("Used Space", TypeMesure.Charge, 61),
            ]),
        ];
    }

    /// <summary>Programmes au démarrage (clés Run fictives, rien n'est écrit dans le registre).</summary>
    public sealed class Registre : IRegistreDemarrage
    {
        private readonly List<EntreeDemarrage> _entrees =
        [
            new("Discord", "\"C:\\Users\\Demo\\AppData\\Local\\Discord\\Update.exe\" --processStart Discord.exe", "HKCU\\Run"),
            new("Steam", "\"C:\\Program Files (x86)\\Steam\\steam.exe\" -silent", "HKCU\\Run"),
            new("OneDrive", "\"C:\\Program Files\\Microsoft OneDrive\\OneDrive.exe\" /background", "HKCU\\Run"),
            new("SecurityHealth", "%windir%\\system32\\SecurityHealthSystray.exe", "HKLM\\Run"),
            new("RtkAudUService", "\"C:\\Windows\\System32\\RtkAudUService64.exe\" -background", "HKLM\\Run"),
        ];

        public IReadOnlyList<EntreeDemarrage> Lister() => _entrees.ToList();

        public void Ecrire(EntreeDemarrage entree) => _entrees.Add(entree);

        public void Supprimer(EntreeDemarrage entree) => _entrees.RemoveAll(e => e.Nom == entree.Nom && e.Emplacement == entree.Emplacement);
    }

    private sealed class FausseWua(DateTime maintenant) : IServiceWua
    {
        public IReadOnlyList<MiseAJour> RechercherEnAttente() =>
        [
            new("Mise à jour cumulative pour Windows 11 Version 25H2 pour les systèmes x64", ["KB5070311"], "Important", false, false),
            new("Mise à jour de la plateforme antimalware de Microsoft Defender", ["KB4052623"], null, false, false),
        ];

        public IReadOnlyList<EntreeHistorique> Historique(int nombreMax) =>
        [
            new(maintenant.AddDays(-6), "Mise à jour de sécurité Windows", true),
            new(maintenant.AddDays(-20), "Mise à jour cumulative pour .NET Framework", true),
        ];
    }

    private sealed class FauxWinget : IServiceWinget
    {
        public IReadOnlyList<MiseAJourLogiciel> RechercherMisesAJour() =>
        [
            new("Mozilla Firefox (x64 fr)", "Mozilla.Firefox.fr", "144.0.2", "145.0.1"),
            new("7-Zip 24.09 (x64)", "7zip.7zip", "24.09", "25.01"),
            new("VLC media player", "VideoLAN.VLC", "3.0.20", "3.0.21"),
            new("Visual Studio Code", "Microsoft.VisualStudioCode", "1.105.1", "1.106.2"),
        ];
    }

    private static PiloteDisponible[] PilotesProposes() =>
    [
        new("NVIDIA - Display - 32.0.15.8180", "NVIDIA GeForce RTX 4070", "NVIDIA", "Display", new DateTime(2026, 10, 14),
            "PCI\\VEN_10DE&DEV_2786", "demo-1", false),
        new("Realtek Semiconductor Corp. - Net - 10.78.1022.2025", "Realtek Gaming 2.5GbE Family Controller", "Realtek", "Net",
            new DateTime(2025, 10, 22), "PCI\\VEN_10EC&DEV_8125", "demo-2", false),
        new("Advanced Micro Devices, Inc. - System - 6.0.0.80", "AMD GPIO Controller", "Advanced Micro Devices, Inc.", "System",
            new DateTime(2026, 6, 3), "ACPI\\AMDI0030", "demo-3", false),
    ];

    private static Dictionary<string, object?> L(params (string Cle, object? Valeur)[] champs) =>
        champs.ToDictionary(c => c.Cle, c => c.Valeur, StringComparer.OrdinalIgnoreCase);

    private static ushort[] Codes(string texte)
    {
        var codes = new ushort[16];
        for (var i = 0; i < texte.Length && i < codes.Length; i++)
        {
            codes[i] = texte[i];
        }

        return codes;
    }

    private static Dictionary<string, object?> Pilote(string appareil, string fabricant, string version, string date, string classe, string id) =>
        L(("DeviceName", appareil), ("Manufacturer", fabricant), ("DriverVersion", version),
            ("DriverDate", date + "000000.000000-000"), ("DeviceClass", classe), ("HardWareID", id));

    private static FausseWmi Wmi() => new FausseWmi()
        .Avec("Win32_Processor", L(("Name", "AMD Ryzen 7 7800X3D 8-Core Processor"), ("Manufacturer", "AuthenticAMD"),
            ("NumberOfCores", (uint)8), ("NumberOfLogicalProcessors", (uint)16), ("MaxClockSpeed", (uint)4201),
            ("L2CacheSize", (uint)8192), ("L3CacheSize", (uint)98304)))
        .Avec("Win32_BaseBoard", L(("Manufacturer", "Micro-Star International Co., Ltd."), ("Product", "MAG B650 TOMAHAWK WIFI (MS-7D75)"), ("Version", "1.0")))
        .Avec("Win32_BIOS", L(("Manufacturer", "American Megatrends International, LLC."), ("SMBIOSBIOSVersion", "1.G0"),
            ("ReleaseDate", "20250912000000.000000+000")))
        .Avec("Win32_PhysicalMemory",
            L(("DeviceLocator", "DIMMA2"), ("Capacity", (ulong)17179869184), ("Speed", (uint)6000), ("Manufacturer", "G Skill Intl")),
            L(("DeviceLocator", "DIMMB2"), ("Capacity", (ulong)17179869184), ("Speed", (uint)6000), ("Manufacturer", "G Skill Intl")))
        .Avec("Win32_VideoController", L(("Name", "NVIDIA GeForce RTX 4070"), ("AdapterRAM", (uint)4293918720),
            ("DriverVersion", "32.0.15.7680"), ("DriverDate", "20250715000000.000000+000"),
            ("CurrentHorizontalResolution", (uint)2560), ("CurrentVerticalResolution", (uint)1440), ("CurrentRefreshRate", (uint)165)))
        .Avec("WmiMonitorID", L(("ManufacturerName", Codes("GSM")), ("UserFriendlyName", Codes("LG ULTRAGEAR"))))
        .Avec("MSFT_NetAdapter",
            L(("Name", "Ethernet"), ("InterfaceDescription", "Realtek Gaming 2.5GbE Family Controller")),
            L(("Name", "Wi-Fi"), ("InterfaceDescription", "MediaTek Wi-Fi 6E MT7922 160MHz Wireless LAN Card")))
        .Avec("Win32_SoundDevice",
            L(("Name", "Realtek USB Audio"), ("Manufacturer", "Realtek"), ("Status", "OK")),
            L(("Name", "NVIDIA High Definition Audio"), ("Manufacturer", "NVIDIA"), ("Status", "OK")))
        .Avec("MSFT_PhysicalDisk",
            L(("DeviceId", "0"), ("FriendlyName", "Samsung SSD 990 PRO 1TB"), ("MediaType", (ushort)4), ("HealthStatus", (ushort)0), ("Size", (ulong)1000204886016)),
            L(("DeviceId", "1"), ("FriendlyName", "ST2000DM008-2UB102"), ("MediaType", (ushort)3), ("HealthStatus", (ushort)0), ("Size", (ulong)2000398934016)))
        .Avec("MSFT_StorageReliabilityCounter",
            L(("DeviceId", "0"), ("Temperature", (ushort)42), ("TemperatureMax", (ushort)82), ("Wear", (byte)3), ("PowerOnHours", (uint)2140), ("ReadErrorsTotal", (ulong)0)),
            L(("DeviceId", "1"), ("Temperature", (ushort)35), ("TemperatureMax", (ushort)60), ("Wear", null), ("PowerOnHours", (uint)9876), ("ReadErrorsTotal", (ulong)0)))
        .Avec("Win32_LogicalDisk",
            L(("DeviceID", "C:"), ("VolumeName", "Windows"), ("Size", (ulong)999556165632), ("FreeSpace", (ulong)389826904064)),
            L(("DeviceID", "D:"), ("VolumeName", "Jeux"), ("Size", (ulong)2000396746752), ("FreeSpace", (ulong)184000000000)))
        .Avec("Win32_PnPSignedDriver",
            Pilote("NVIDIA GeForce RTX 4070", "NVIDIA", "32.0.15.7680", "20250715", "DISPLAY", "PCI\\VEN_10DE&DEV_2786&SUBSYS_51721462"),
            Pilote("Realtek Gaming 2.5GbE Family Controller", "Realtek", "10.68.815.2023", "20230815", "NET", "PCI\\VEN_10EC&DEV_8125&SUBSYS_7D751462"),
            Pilote("MediaTek Wi-Fi 6E MT7922 160MHz Wireless LAN Card", "MediaTek, Inc.", "3.3.0.908", "20240620", "NET", "PCI\\VEN_14C3&DEV_0616"),
            Pilote("AMD GPIO Controller", "Advanced Micro Devices, Inc", "2.2.0.130", "20230309", "SYSTEM", "ACPI\\AMDI0030"),
            Pilote("AMD PSP 11.0 Device", "Advanced Micro Devices Inc.", "5.39.0.0", "20250402", "SECURITYDEVICES", "PCI\\VEN_1022&DEV_1649"),
            Pilote("Realtek USB Audio", "Realtek", "6.3.9600.2402", "20240918", "MEDIA", "USB\\VID_0DB0&PID_CD0E"),
            Pilote("NVIDIA High Definition Audio", "NVIDIA", "1.4.3.2", "20240929", "MEDIA", "HDAUDIO\\FUNC_01&VEN_10DE&DEV_00A5"),
            Pilote("Samsung NVMe Controller", "Samsung Electronics Co., Ltd.", "3.3.0.2003", "20230210", "SCSIADAPTER", "PCI\\VEN_144D&DEV_A80C"),
            Pilote("Logitech G502 X", "Logitech", "1.10.88.0", "20240102", "HIDCLASS", "USB\\VID_046D&PID_C099"),
            Pilote("Contrôleur hôte USB xHCI", "Microsoft", "10.0.26100.1", "20060621", "USB", "PCI\\VEN_1022&DEV_43F7"),
            Pilote("Périphérique d'entrée USB", "Microsoft", "10.0.26100.1", "20060621", "HIDCLASS", "USB\\VID_046D&PID_C33F"),
            Pilote("Disque", "Microsoft", "10.0.26100.1", "20060621", "DISKDRIVE", "SCSI\\DiskSamsung_SSD_990_PRO_1TB"),
            Pilote("Moniteur PnP générique", "Microsoft", "10.0.26100.1", "20060621", "MONITOR", "MONITOR\\GSM5BEE"));

    private sealed class FausseWmi : ISourceWmi
    {
        private readonly Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> _donnees = new();

        public FausseWmi Avec(string classe, params IReadOnlyDictionary<string, object?>[] lignes)
        {
            _donnees[classe] = lignes;
            return this;
        }

        public IReadOnlyList<IReadOnlyDictionary<string, object?>> Requeter(string espaceNoms, string classe, IReadOnlyList<string> proprietes, string? filtre = null) =>
            _donnees.TryGetValue(classe, out var lignes) ? lignes : [];
    }
}
