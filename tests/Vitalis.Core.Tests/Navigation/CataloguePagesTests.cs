using FluentAssertions;
using Vitalis.Core.Navigation;
using Xunit;

namespace Vitalis.Core.Tests.Navigation;

public class CataloguePagesTests
{
    [Fact]
    public void Toutes_ContientExactementDixPages()
    {
        CataloguePages.Toutes.Should().HaveCount(10);
    }

    [Fact]
    public void Cles_SontUniques()
    {
        CataloguePages.Toutes.Select(p => p.Cle).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Cles_SontEnKebabCase()
    {
        foreach (var page in CataloguePages.Toutes)
        {
            page.Cle.Should().MatchRegex("^[a-z]+(-[a-z]+)*$", $"la clé « {page.Cle} » doit être en kebab-case");
        }
    }

    [Fact]
    public void Titres_NePasEtreVides()
    {
        CataloguePages.Toutes.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p.Titre));
    }

    [Theory]
    [InlineData(2, "materiel")]
    [InlineData(3, "disques")]
    [InlineData(4, "mises-a-jour")]
    [InlineData(11, "parametres")]
    public void EtapeDisponible_CorrespondAuRoadmap(int etape, string cle)
    {
        CataloguePages.Toutes.Should().Contain(p => p.Cle == cle && p.EtapeDisponible == etape);
    }
}
