using FluentAssertions;
using Vitalis.Core.Materiel;
using Vitalis.Core.MisesAJour;
using Xunit;

namespace Vitalis.Core.Tests.MisesAJour;

/// <summary>Appels réels à Windows Update Agent sur la machine de test (Windows uniquement).</summary>
public class WuaReelTests
{
    [Fact]
    public void MachineReelle_HistoriqueWindowsUpdateLisible()
    {
        var entrees = new WuaCom().Historique(AnalyseMisesAJour.NombreHistorique);

        entrees.Should().NotBeNull();
        entrees.Should().OnlyContain(e => e.Titre != null || e.Date != null);
    }

    [Fact]
    public async Task MachineReelle_RapportMisesAJourSansExceptionNonGeree()
    {
        var rapport = await new AnalyseMisesAJour(new WuaCom()).AnalyserAsync();

        rapport.EnAttente.Etat.Should().NotBe(Disponibilite.Partielle);
        rapport.EnAttente.Raison.Should().NotContain("Exception");
    }
}
