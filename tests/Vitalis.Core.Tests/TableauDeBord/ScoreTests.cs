using FluentAssertions;
using Vitalis.Core.Disques;
using Vitalis.Core.TableauDeBord;
using Xunit;

namespace Vitalis.Core.Tests.TableauDeBord;

public class ScoreTests
{
    private static readonly DateTime Maintenant = new(2026, 11, 20);

    [Fact]
    public void MisesAJour_SansAttente_RecenteCent()
    {
        Score.MisesAJour(0, Maintenant.AddDays(-5), Maintenant)!.Valeur.Should().Be(100);
    }

    [Fact]
    public void MisesAJour_DixPointsParMiseEnAttente()
    {
        Score.MisesAJour(3, Maintenant.AddDays(-5), Maintenant)!.Valeur.Should().Be(70);
    }

    [Fact]
    public void MisesAJour_VingtPointsSiInstallationAncienne()
    {
        Score.MisesAJour(0, Maintenant.AddDays(-45), Maintenant)!.Valeur.Should().Be(80);
    }

    [Fact]
    public void MisesAJour_NeDescendPasSousZero()
    {
        Score.MisesAJour(15, null, Maintenant)!.Valeur.Should().Be(0);
    }

    [Fact]
    public void MisesAJour_SansDonnee_Indetermine()
    {
        Score.MisesAJour(null, null, Maintenant)!.Valeur.Should().BeNull();
    }

    [Fact]
    public void Disques_MoyenneSainAttention()
    {
        var disques = new[]
        {
            new DisqueSante("1", "A", TypeMedia.Ssd, null, StatutSante.Sain, null, null, null, null, null),
            new DisqueSante("2", "B", TypeMedia.Hdd, null, StatutSante.Attention, null, null, null, null, null),
        };

        Score.Disques(disques).Valeur.Should().Be(75);
    }

    [Fact]
    public void Disques_SanteInconnueExclue()
    {
        var disques = new[]
        {
            new DisqueSante("1", "A", TypeMedia.Inconnu, null, StatutSante.Inconnu, null, null, null, null, null),
            new DisqueSante("2", "B", TypeMedia.Ssd, null, StatutSante.Critique, null, null, null, null, null),
        };

        Score.Disques(disques).Valeur.Should().Be(0);
        Score.Disques([]).Valeur.Should().BeNull();
    }

    [Fact]
    public void Espace_MoyenneParLecteur()
    {
        var lecteurs = new[]
        {
            new LecteurEspace("C:", null, 1, 1, 50, StatutEspace.Sain),
            new LecteurEspace("D:", null, 1, 1, 5, StatutEspace.Critique),
        };

        Score.Espace(lecteurs).Valeur.Should().Be(60);
    }

    [Fact]
    public void Global_PonderationDesComposantsConnus()
    {
        // Disques 50 (poids 40), mises à jour 100 (poids 35), espace indisponible : (2000 + 3500) / 75 = 73,3
        var composants = new[]
        {
            new ComposantScore("Disques", 40, 50),
            new ComposantScore("Mises à jour", 35, 100),
            new ComposantScore("Espace libre", 25, null),
        };

        Score.Global(composants).Should().Be(73);
    }

    [Fact]
    public void Global_AucunComposantConnu_Null()
    {
        Score.Global([new ComposantScore("Disques", 40, null)]).Should().BeNull();
    }
}
