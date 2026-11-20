using FluentAssertions;
using Vitalis.Core.Materiel;
using Xunit;
using static Vitalis.Core.Materiel.Conversions;

namespace Vitalis.Core.Tests.Materiel;

public class InventaireMaterielTests
{
    [Fact]
    public async Task MachineComplete_ToutesLesSectionsSontDisponibles()
    {
        var rapport = await new InventaireMateriel(FauxSourceWmi.MachineComplete()).AnalyserAsync();

        rapport.Processeur.Etat.Should().Be(Disponibilite.Disponible);
        rapport.CarteMere.Etat.Should().Be(Disponibilite.Disponible);
        rapport.Bios.Etat.Should().Be(Disponibilite.Disponible);
        rapport.Memoire.Etat.Should().Be(Disponibilite.Disponible);
        rapport.CartesGraphiques.Etat.Should().Be(Disponibilite.Disponible);
        rapport.Ecrans.Etat.Should().Be(Disponibilite.Disponible);
        rapport.Reseau.Etat.Should().Be(Disponibilite.Disponible);
        rapport.Audio.Etat.Should().Be(Disponibilite.Disponible);
    }

    [Fact]
    public async Task MachineComplete_ValeursCorrectementConverties()
    {
        var rapport = await new InventaireMateriel(FauxSourceWmi.MachineComplete()).AnalyserAsync();

        var cpu = rapport.Processeur.Elements.Single();
        cpu.Nom.Should().Be("Intel(R) Core(TM) i7-12700");
        cpu.Coeurs.Should().Be(12);
        cpu.ThreadsLogiques.Should().Be(20);
        cpu.FrequenceMaxMHz.Should().Be(2100);

        rapport.Bios.Elements.Single().DateSortie.Should().Be(new DateTime(2023, 1, 15));

        Conversions.MemoireTotaleOctets(rapport.Memoire.Elements).Should().Be(17179869184L); // 16 Gio

        var gpu = rapport.CartesGraphiques.Elements.Single();
        gpu.VersionPilote.Should().Be("31.0.15.3623");
        gpu.DatePilote.Should().Be(new DateTime(2024, 5, 10));
        gpu.FrequenceHz.Should().Be(144);

        rapport.Ecrans.Elements.Single().Nom.Should().Be("DELL U2720Q");
        rapport.Ecrans.Elements.Single().Fabricant.Should().Be("DEL");
        rapport.Audio.Elements.Single().Statut.Should().Be("OK");
    }

    [Fact]
    public async Task Reseau_NeLitQueLesAdaptateursMateriels()
    {
        var source = FauxSourceWmi.MachineComplete();

        await new InventaireMateriel(source).AnalyserAsync();

        source.Appels.Should().Contain(a =>
            a.Espace == @"root\StandardCimv2" && a.Classe == "MSFT_NetAdapter" && a.Filtre == "HardwareInterface = TRUE");
    }

    [Fact]
    public async Task ClasseWmiManquante_SeuleLaSectionConcerneeEstIndisponible()
    {
        var source = FauxSourceWmi.MachineComplete().SansClasse("Win32_SoundDevice");

        var rapport = await new InventaireMateriel(source).AnalyserAsync();

        rapport.Audio.Etat.Should().Be(Disponibilite.Indisponible);
        rapport.Audio.Elements.Should().BeEmpty();
        rapport.Audio.Raison.Should().Contain("Audio").And.Contain("Win32_SoundDevice");

        rapport.Processeur.Etat.Should().Be(Disponibilite.Disponible);
        rapport.Ecrans.Etat.Should().Be(Disponibilite.Disponible);
        rapport.Reseau.Etat.Should().Be(Disponibilite.Disponible);
    }

    [Fact]
    public async Task DonneeEssentielleAbsente_EtatPartiel_SansInventer()
    {
        var source = FauxSourceWmi.MachineComplete()
            .Avec("Win32_Processor", FauxSourceWmi.Ligne(
                ("Name", null), ("Manufacturer", "GenuineIntel"), ("NumberOfCores", (uint)4)));

        var rapport = await new InventaireMateriel(source).AnalyserAsync();

        rapport.Processeur.Etat.Should().Be(Disponibilite.Partielle);
        var cpu = rapport.Processeur.Elements.Single();
        cpu.Nom.Should().BeNull("un nom absent reste absent, il n'est pas inventé");
        cpu.Coeurs.Should().Be(4);
        rapport.Processeur.Raison.Should().Contain("Win32_Processor");
    }

    [Fact]
    public async Task AucuneLigne_EtatIndisponibleAvecRaison()
    {
        var rapport = await new InventaireMateriel(new FauxSourceWmi()).AnalyserAsync();

        rapport.Processeur.Etat.Should().Be(Disponibilite.Indisponible);
        rapport.Processeur.Raison.Should().Contain("Aucun élément");
        rapport.Ecrans.Etat.Should().Be(Disponibilite.Indisponible);
        rapport.Reseau.Etat.Should().Be(Disponibilite.Indisponible);
    }

    [Fact]
    public async Task AnnulationDemandee_LanceOperationCanceled()
    {
        var annulation = new CancellationTokenSource();
        annulation.Cancel();

        var action = () => new InventaireMateriel(FauxSourceWmi.MachineComplete()).AnalyserAsync(annulation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }
}
