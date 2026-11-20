using FluentAssertions;
using Vitalis.Core.MisesAJour;
using Xunit;

namespace Vitalis.Core.Tests.MisesAJour;

/// <summary>Appel réel de winget sur la machine de test (lecture seule).</summary>
[Trait("Categorie", "Reel")]
public class WingetReelTests
{
    [Fact]
    public void MachineReelle_WingetListeLesMisesAJour()
    {
        var elements = new WingetCli().RechercherMisesAJour();

        elements.Should().OnlyContain(m => m.Id.Contains('.') && m.Nom.Length > 0);
        elements.Should().OnlyContain(m => m.VersionDisponible.Length > 0 && m.VersionActuelle.Length > 0);
    }
}
