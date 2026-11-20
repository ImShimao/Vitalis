using FluentAssertions;
using Vitalis.Core.Materiel;
using Xunit;

namespace Vitalis.Core.Tests.Materiel;

/// <summary>Lecture réelle WMI sur la machine qui exécute les tests. Ne vérifie que ce qui est garanti sous Windows.</summary>
public class InventaireMaterielReelTests
{
    [Fact]
    public async Task MachineReelle_LeProcesseurEtLaMemoireSontLus()
    {
        var rapport = await new InventaireMateriel(new WmiSource()).AnalyserAsync();

        rapport.Processeur.Etat.Should().NotBe(Disponibilite.Indisponible, rapport.Processeur.Raison);
        rapport.Processeur.Elements.Should().NotBeEmpty();
        rapport.Processeur.Elements[0].Nom.Should().NotBeNullOrWhiteSpace();

        rapport.Memoire.Etat.Should().NotBe(Disponibilite.Indisponible, rapport.Memoire.Raison);
    }
}
