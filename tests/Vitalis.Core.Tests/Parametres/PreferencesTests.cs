using FluentAssertions;
using Vitalis.Core.Parametres;
using Xunit;

namespace Vitalis.Core.Tests.Parametres;

public class PreferencesTests : IDisposable
{
    private readonly string _dossier = Path.Combine(Path.GetTempPath(), "vitalis-prefs-" + Guid.NewGuid().ToString("N"));
    private string Fichier => Path.Combine(_dossier, "preferences.json");

    public void Dispose()
    {
        if (Directory.Exists(_dossier))
        {
            Directory.Delete(_dossier, recursive: true);
        }
    }

    [Fact]
    public void ThemeInconnu_RevientAuSysteme()
    {
        new Preferences("Violet").Normalisees().Theme.Should().Be("Systeme");
    }

    [Fact]
    public void Enregistrer_PuisCharger_ConserveLesValeurs()
    {
        StockagePreferences.Enregistrer(Fichier, new Preferences("Sombre", ModeDiagnostic: true));

        var p = StockagePreferences.Charger(Fichier);

        p.Theme.Should().Be("Sombre");
        p.ModeDiagnostic.Should().BeTrue();
    }

    [Fact]
    public void FichierAbsent_ValeursParDefaut()
    {
        StockagePreferences.Charger(Fichier).Should().Be(new Preferences());
    }

    [Fact]
    public void FichierCorrompu_ValeursParDefaut_SansErreur()
    {
        Directory.CreateDirectory(_dossier);
        File.WriteAllText(Fichier, "{ pas du json");

        StockagePreferences.Charger(Fichier).Theme.Should().Be("Systeme");
    }
}
