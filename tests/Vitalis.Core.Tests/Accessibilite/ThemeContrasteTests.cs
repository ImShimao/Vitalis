using System.Xml.Linq;
using FluentAssertions;
using Vitalis.Core.Accessibilite;
using Xunit;

namespace Vitalis.Core.Tests.Accessibilite;

/// <summary>Lit la palette réelle (src/Vitalis.App/Themes/Couleurs.xaml) et vérifie WCAG AA sur chaque thème.</summary>
public class ThemeContrasteTests
{
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace XamlX = "http://schemas.microsoft.com/winfx/2006/xaml";

    public static IEnumerable<object[]> Couples()
    {
        yield return ["Light", "VitalisTexte", "VitalisFond"];
        yield return ["Light", "VitalisTexteSecondaire", "VitalisFond"];
        yield return ["Dark", "VitalisTexte", "VitalisFond"];
        yield return ["Dark", "VitalisTexteSecondaire", "VitalisFond"];

        // Couleurs d'état : sur le fond de page et sur leur fond de badge.
        foreach (var theme in new[] { "Light", "Dark" })
        {
            foreach (var etat in new[] { "Succes", "Attention", "Critique" })
            {
                yield return [theme, "Vitalis" + etat, "VitalisFond"];
                yield return [theme, "Vitalis" + etat, "Vitalis" + etat + "Fond"];
            }

            yield return [theme, "VitalisTexte", "VitalisNeutreFond"];
        }
    }

    [Theory]
    [MemberData(nameof(Couples))]
    public void CoupleTexteFond_AtteintWcagAA(string theme, string texte, string fond)
    {
        var couleurs = LireCouleurs(theme);

        var rapport = Contraste.Rapport(couleurs[texte], couleurs[fond]);

        rapport.Should().BeGreaterThanOrEqualTo(Contraste.SeuilAA,
            $"thème {theme} : {texte} ({couleurs[texte]}) sur {fond} ({couleurs[fond]}) doit atteindre 4,5:1");
    }

    private static Dictionary<string, string> LireCouleurs(string theme)
    {
        var xaml = XDocument.Load(Path.Combine(DossierSolution(), "src", "Vitalis.App", "Themes", "Couleurs.xaml"));

        var dictionnaire = xaml.Root!
            .Element(Xaml + "ResourceDictionary.ThemeDictionaries")!
            .Elements(Xaml + "ResourceDictionary")
            .Single(d => d.Attribute(XamlX + "Key")?.Value == theme);

        return dictionnaire
            .Elements(Xaml + "SolidColorBrush")
            .ToDictionary(
                b => b.Attribute(XamlX + "Key")!.Value,
                b => b.Attribute("Color")!.Value);
    }

    private static string DossierSolution()
    {
        var dossier = new DirectoryInfo(AppContext.BaseDirectory);
        while (dossier is not null && !File.Exists(Path.Combine(dossier.FullName, "Vitalis.sln")))
        {
            dossier = dossier.Parent;
        }

        return dossier?.FullName ?? throw new InvalidOperationException("Vitalis.sln introuvable en remontant depuis les tests.");
    }
}
