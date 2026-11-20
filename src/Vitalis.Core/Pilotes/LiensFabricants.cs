namespace Vitalis.Core.Pilotes;

/// <summary>
/// Page d'assistance officielle d'un fabricant. Un lien n'est proposé que s'il a été vérifié
/// (réponse HTTP 2xx, contrôlée à la date indiquée). Les autres fabricants sont signalés « non vérifié ».
/// </summary>
public sealed record LienFabricant(string Nom, string? Url, string? Statut, string? VerifieLe)
{
    public bool EstVerifie => Url is not null;
}

public static class LiensFabricants
{
    /// <summary>Date du dernier contrôle des adresses ci-dessous (2026-11-20).</summary>
    public const string DateVerification = "2026-11-20";

    private static readonly (string Motif, LienFabricant Lien)[] Table =
    [
        ("NVIDIA", new LienFabricant("NVIDIA", "https://www.nvidia.com/Download/index.aspx", "HTTP 200", DateVerification)),
        ("Intel", new LienFabricant("Intel", "https://www.intel.com/content/www/us/en/download-center/home.html", "HTTP 200", DateVerification)),
        ("Realtek", new LienFabricant("Realtek", "https://www.realtek.com/downloads", "HTTP 200", DateVerification)),
        ("ASUS", new LienFabricant("ASUS", "https://www.asus.com/support/download-center/", "HTTP 202 (à confirmer à l'ouverture)", DateVerification)),
        // Non vérifiés : MSI, Gigabyte, Dell, HP, Lenovo (403 : blocage automatique), AMD et Acer (pas de réponse).
        ("AMD", new LienFabricant("AMD", null, "non vérifié (pas de réponse)", DateVerification)),
        ("Advanced Micro", new LienFabricant("AMD", null, "non vérifié (pas de réponse)", DateVerification)),
        ("MSI", new LienFabricant("MSI", null, "non vérifié (HTTP 403)", DateVerification)),
        ("Gigabyte", new LienFabricant("Gigabyte", null, "non vérifié (HTTP 403)", DateVerification)),
        ("Dell", new LienFabricant("Dell", null, "non vérifié (HTTP 403)", DateVerification)),
        ("Hewlett", new LienFabricant("HP", null, "non vérifié (HTTP 403)", DateVerification)),
        ("HP", new LienFabricant("HP", null, "non vérifié (HTTP 403)", DateVerification)),
        ("Lenovo", new LienFabricant("Lenovo", null, "non vérifié (HTTP 403)", DateVerification)),
        ("Acer", new LienFabricant("Acer", null, "non vérifié (pas de réponse)", DateVerification)),
    ];

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
}
