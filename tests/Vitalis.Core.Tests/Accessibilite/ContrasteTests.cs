using FluentAssertions;
using Vitalis.Core.Accessibilite;
using Xunit;

namespace Vitalis.Core.Tests.Accessibilite;

public class ContrasteTests
{
    [Fact]
    public void NoirSurBlanc_DonneLeMaximum()
    {
        Contraste.Rapport("#000000", "#FFFFFF").Should().BeApproximately(21.0, 0.01);
    }

    [Fact]
    public void MemeCouleur_DonneUn()
    {
        Contraste.Rapport("#3A7BD5", "#3A7BD5").Should().BeApproximately(1.0, 0.0001);
    }

    [Fact]
    public void ArgumentsInverses_DonnentLeMemeRapport()
    {
        Contraste.Rapport("#1A1A1A", "#F3F3F3").Should().BeApproximately(Contraste.Rapport("#F3F3F3", "#1A1A1A"), 0.0001);
    }

    [Fact]
    public void FormatAvecAlpha_EstIgnore()
    {
        Contraste.Rapport("#FF000000", "#FFFFFFFF").Should().BeApproximately(21.0, 0.01);
    }

    [Theory]
    [InlineData("#zzzzzz")]
    [InlineData("#12345")]
    [InlineData("")]
    public void CouleurInvalide_LanceUneErreur(string couleur)
    {
        var action = () => Contraste.Rapport(couleur, "#FFFFFF");
        action.Should().Throw<ArgumentException>();
    }
}
