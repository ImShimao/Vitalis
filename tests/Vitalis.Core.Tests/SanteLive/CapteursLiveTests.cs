using FluentAssertions;
using Vitalis.Core.SanteLive;
using Xunit;
using Xunit.Abstractions;

namespace Vitalis.Core.Tests.SanteLive;

public class CapteursLiveTests
{
    private readonly ITestOutputHelper _sortie;

    public CapteursLiveTests(ITestOutputHelper sortie) => _sortie = sortie;

    private static ComposantLive Cpu(params Mesure[] m) => new("CPU test", Categorie.Processeur, m);

    [Theory]
    [InlineData(60f, NiveauMesure.Normal)]
    [InlineData(85f, NiveauMesure.Attention)]
    [InlineData(97f, NiveauMesure.Critique)]
    public void Seuils_Processeur(float t, NiveauMesure attendu) =>
        AnalyseCapteurs.Niveau(Categorie.Processeur, t).Should().Be(attendu);

    [Fact]
    public void SansTemperature_Normal_PasDeValeurInventee() =>
        AnalyseCapteurs.Niveau(Categorie.Stockage, null).Should().Be(NiveauMesure.Normal);

    [Fact]
    public void TemperaturePrincipale_PrefereLePackage()
    {
        var c = Cpu(new("Core #1", TypeMesure.Temperature, 70), new("CPU Package", TypeMesure.Temperature, 65), new("Core #2", TypeMesure.Temperature, 72));

        AnalyseCapteurs.TemperaturePrincipale(c).Should().Be(65);
    }

    [Fact]
    public void TemperatureNonLue_Null()
    {
        var c = Cpu(new Mesure("CPU Package", TypeMesure.Temperature, null));

        AnalyseCapteurs.TemperaturePrincipale(c).Should().BeNull();
        AnalyseCapteurs.TemperaturesProcesseurManquantes([c]).Should().BeTrue();
    }

    [Fact]
    public void ChargePrincipale_PrefereLeTotal()
    {
        var c = Cpu(new("CPU Core #1", TypeMesure.Charge, 90), new("CPU Total", TypeMesure.Charge, 12));

        AnalyseCapteurs.ChargePrincipale(c).Should().Be(12);
    }

    /// <summary>Lecture réelle : ne doit jamais lever d'exception, même sans pilote PawnIO ni droits administrateur.</summary>
    [Fact]
    [Trait("Categorie", "Reel")]
    public void LectureReelle_SansException()
    {
        using var capteurs = new CapteursLive();
        var composants = capteurs.Lire();

        foreach (var c in composants)
        {
            _sortie.WriteLine($"{c.Categorie} {c.Nom} : {c.Mesures.Count} mesures, temp {AnalyseCapteurs.TemperaturePrincipale(c)?.ToString() ?? "n/d"}, charge {AnalyseCapteurs.ChargePrincipale(c)?.ToString() ?? "n/d"}");
        }

        composants.Should().NotBeNull();
    }
}
