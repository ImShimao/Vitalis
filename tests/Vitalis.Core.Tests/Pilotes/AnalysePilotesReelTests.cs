using FluentAssertions;
using Vitalis.Core.Materiel;
using Vitalis.Core.Pilotes;
using Xunit;

namespace Vitalis.Core.Tests.Pilotes;

/// <summary>Lecture réelle de Win32_PnPSignedDriver sur la machine de test (lecture seule).</summary>
[Trait("Categorie", "Reel")]
public class AnalysePilotesReelTests
{
    [Fact]
    public void MachineReelle_LesPilotesSontLus()
    {
        var rapport = new AnalysePilotes(new WmiSource()).Analyser(DateTime.Now);

        rapport.Pilotes.Etat.Should().NotBe(Disponibilite.Indisponible, rapport.Pilotes.Raison);
        rapport.Pilotes.Elements.Should().NotBeEmpty();
    }
}
