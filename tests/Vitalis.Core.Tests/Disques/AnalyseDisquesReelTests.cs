using FluentAssertions;
using Vitalis.Core.Disques;
using Vitalis.Core.Materiel;
using Xunit;

namespace Vitalis.Core.Tests.Disques;

/// <summary>Lecture réelle sur la machine de test. Les disques physiques ne sont pas testés ici : ils dépendent des droits.</summary>
[Trait("Categorie", "Reel")]
public class AnalyseDisquesReelTests
{
    [Fact]
    public async Task MachineReelle_LesLecteursFixesSontLus()
    {
        var rapport = await new AnalyseDisques(new WmiSource()).AnalyserAsync();

        rapport.Lecteurs.Etat.Should().NotBe(Disponibilite.Indisponible, rapport.Lecteurs.Raison);
        rapport.Lecteurs.Elements.Should().NotBeEmpty();
        rapport.Lecteurs.Elements.Should().OnlyContain(l => l.TailleOctets > 0);
    }
}
