namespace Vitalis.Core.Pilotes;

/// <summary>
/// Page officielle de pilotes ou d'assistance d'un fabricant. Un lien n'est proposé que s'il a été vérifié
/// (page ouverte et titre contrôlé, à la date indiquée). Les autres fabricants sont signalés « non vérifié ».
/// </summary>
/// <param name="Outil">Outil officiel de détection automatique, s'il existe (NVIDIA App, Intel DSA, AMD Adrenalin…).</param>
public sealed record LienFabricant(string Nom, string? Url, string? Statut, string? VerifieLe, string? Outil = null, string? UrlOutil = null)
{
    public bool EstVerifie => Url is not null;
}

public static class LiensFabricants
{
    /// <summary>Date du dernier contrôle des adresses ci-dessous (2026-11-20). Méthode : curl puis navigateur pour les sites anti-robots.</summary>
    public const string DateVerification = "2026-11-20";

    private const string D = DateVerification;

    private static LienFabricant L(string nom, string url, string? outil = null, string? urlOutil = null) =>
        new(nom, url, "page vérifiée", D, outil, urlOutil);

    private static readonly LienFabricant Nvidia = L("NVIDIA", "https://www.nvidia.com/fr-fr/drivers/", "NVIDIA App", "https://www.nvidia.com/fr-fr/software/nvidia-app/");
    private static readonly LienFabricant Amd = L("AMD", "https://www.amd.com/fr/support/download/drivers.html");
    private static readonly LienFabricant Intel = L("Intel", "https://www.intel.fr/content/www/fr/fr/support/detect.html", "Assistant pilotes et support Intel", "https://www.intel.fr/content/www/fr/fr/support/detect.html");
    private static readonly LienFabricant Msi = L("MSI", "https://www.msi.com/support/download");
    private static readonly LienFabricant Hp = L("HP", "https://support.hp.com/fr-fr/drivers");

    /// <summary>Motif cherché dans le nom du fabricant renvoyé par Windows (premier trouvé gagne).</summary>
    private static readonly (string Motif, LienFabricant Lien)[] Table =
    [
        ("NVIDIA", Nvidia),
        ("Advanced Micro", Amd),
        ("AMD", Amd),
        ("ATI ", Amd),
        ("Intel", Intel),
        ("Realtek", L("Realtek", "https://www.realtek.com/Download/Index")),
        ("ASUS", L("ASUS", "https://www.asus.com/fr/support/download-center/")),
        ("ASRock", L("ASRock", "https://www.asrock.com/support/index.asp")),
        ("Micro-Star", Msi),
        ("MSI", Msi),
        ("Gigabyte", L("Gigabyte", "https://www.gigabyte.com/fr/Support")),
        ("Dell", L("Dell", "https://www.dell.com/support/home/fr-fr")),
        ("Hewlett", Hp),
        ("HP", Hp),
        ("Lenovo", L("Lenovo", "https://pcsupport.lenovo.com/fr/fr")),
        ("Acer", L("Acer", "https://www.acer.com/fr-fr/support/drivers-and-manuals")),
        ("Surface", L("Microsoft Surface", "https://support.microsoft.com/fr-fr/surface/drivers-firmware/download-drivers-and-firmware-for-surface")),
        ("Logitech", L("Logitech", "https://www.logitech.com/fr-fr/support.html")),
        ("Razer", L("Razer", "https://www.razer.com/synapse-4", "Razer Synapse", "https://www.razer.com/synapse-4")),
        ("SteelSeries", L("SteelSeries", "https://steelseries.com/gg", "SteelSeries GG", "https://steelseries.com/gg")),
        ("Corsair", L("Corsair", "https://www.corsair.com/fr/fr/s/downloads")),
        ("Creative", L("Creative", "https://www.creative.com/support/")),
        ("Brother", L("Brother", "https://support.brother.com/g/b/countrytop.aspx?c=fr&lang=fr")),
        ("TOSHIBA", L("Toshiba (stockage)", "https://www.toshiba-storage.com/support/")),
        ("Samsung", L("Samsung", "https://www.samsung.com/fr/support/")),
        ("Western Digital", L("Western Digital", "https://www.wd.com/fr-fr/support.html")),
        ("WDC", L("Western Digital", "https://www.wd.com/fr-fr/support.html")),
        ("Seagate", L("Seagate", "https://www.seagate.com/fr/fr/support/downloads/")),
        ("Crucial", L("Crucial", "https://www.crucial.fr/support/storage-executive")),
        ("Micron", L("Crucial", "https://www.crucial.fr/support/storage-executive")),
        ("Broadcom", L("Broadcom", "https://www.broadcom.com/support/download-search")),
        // Non vérifiés à cette date : Kingston (page 404), Qualcomm (page 404), Synaptics (page 404).
        ("Kingston", new LienFabricant("Kingston", null, "non vérifié (page introuvable)", D)),
        ("Qualcomm", new LienFabricant("Qualcomm", null, "non vérifié (page introuvable)", D)),
        ("Synaptics", new LienFabricant("Synaptics", null, "non vérifié (page introuvable)", D)),
    ];

    /// <summary>Page Microsoft : mettre à jour un pilote manuellement dans Windows (vérifiée le 2026-11-20).</summary>
    public const string AideMicrosoft = "https://support.microsoft.com/fr-fr/windows/mettre-%C3%A0-jour-les-pilotes-manuellement-dans-windows-ec62f46c-ff14-c91d-eead-d7126dc1f7b6";

    /// <summary>Mises à jour facultatives de Windows Update (pilotes proposés par Microsoft et les fabricants).</summary>
    public const string MisesAJourFacultatives = "ms-settings:windowsupdate-optionalupdates";

    /// <summary>Trouve le lien d'un fabricant d'après le nom renvoyé par Windows. Null si inconnu.</summary>
    public static LienFabricant? Trouver(string? fabricant)
    {
        if (string.IsNullOrWhiteSpace(fabricant))
        {
            return null;
        }

        foreach (var (motif, lien) in Table)
        {
            if (fabricant.Contains(motif, StringComparison.OrdinalIgnoreCase))
            {
                return lien;
            }
        }

        return null;
    }

    /// <summary>
    /// Pilote générique de Windows (fabricant « Microsoft », « (Standard …) », « Generic … ») :
    /// il est fourni et mis à jour par Windows Update, sa date ancienne n'est pas un problème.
    /// </summary>
    public static bool EstGenerique(string? fabricant)
    {
        if (string.IsNullOrWhiteSpace(fabricant))
        {
            return true;
        }

        var f = fabricant.Trim();
        return f.StartsWith('(')
            || f.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) && !f.Contains("Surface", StringComparison.OrdinalIgnoreCase)
            || f.StartsWith("Generic", StringComparison.OrdinalIgnoreCase)
            || f.StartsWith("Standard", StringComparison.OrdinalIgnoreCase)
            || f.StartsWith("Compatible", StringComparison.OrdinalIgnoreCase)
            || f.Equals("Xbox", StringComparison.OrdinalIgnoreCase);
    }
}
