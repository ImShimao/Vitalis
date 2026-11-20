using FluentAssertions;
using Vitalis.Core.Disques;
using Vitalis.Core.Materiel;
using Vitalis.Core.Tests.Materiel;
using Xunit;
using static Vitalis.Core.Tests.Materiel.FauxSourceWmi;

namespace Vitalis.Core.Tests.Disques;

public class AnalyseDisquesTests
{
    private const long Gio = 1024L * 1024 * 1024;

    private static FauxSourceWmi DisquesComplets() => new FauxSourceWmi()
        .Avec("MSFT_PhysicalDisk",
            Ligne(("DeviceId", "1"), ("FriendlyName", "Samsung SSD 980"), ("MediaType", (ushort)4), ("HealthStatus", (ushort)0), ("Size", (ulong)(500 * Gio))),
            Ligne(("DeviceId", "2"), ("FriendlyName", "WDC WD10EZEX"), ("MediaType", (ushort)3), ("HealthStatus", (ushort)1), ("Size", (ulong)(1000 * Gio))))
        .Avec("MSFT_StorageReliabilityCounter",
            Ligne(("DeviceId", "1"), ("Temperature", (byte)38), ("TemperatureMax", (byte)70), ("Wear", (byte)3), ("PowerOnHours", (ushort)5200), ("ReadErrorsTotal", (ulong)0)),
            Ligne(("DeviceId", "2"), ("Temperature", (byte)41), ("TemperatureMax", (byte)60), ("Wear", (byte)0), ("PowerOnHours", (ushort)31000), ("ReadErrorsTotal", (ulong)12)))
        .Avec("Win32_LogicalDisk",
            Ligne(("DeviceID", "C:"), ("VolumeName", "Système"), ("Size", (ulong)(400 * Gio)), ("FreeSpace", (ulong)(100 * Gio))),
            Ligne(("DeviceID", "D:"), ("VolumeName", "Données"), ("Size", (ulong)(1000 * Gio)), ("FreeSpace", (ulong)(50 * Gio))));

    [Fact]
    public async Task DisquesComplets_SmartLuEtStatutsCorrects()
    {
        var rapport = await new AnalyseDisques(DisquesComplets()).AnalyserAsync();

        rapport.SmartComplet.Should().BeTrue();
        rapport.Disques.Etat.Should().Be(Disponibilite.Disponible);

        var ssd = rapport.Disques.Elements.Single(d => d.Identifiant == "1");
        ssd.Type.Should().Be(TypeMedia.Ssd);
        ssd.Sante.Should().Be(StatutSante.Sain);
        ssd.TemperatureC.Should().Be(38);
        ssd.UsurePourcent.Should().Be(3);
        ssd.HeuresMarche.Should().Be(5200);

        var hdd = rapport.Disques.Elements.Single(d => d.Identifiant == "2");
        hdd.Type.Should().Be(TypeMedia.Hdd);
        hdd.Sante.Should().Be(StatutSante.Attention);
        hdd.ErreursLectureTotal.Should().Be(12);
    }

    [Fact]
    public async Task ComptesRefuses_LesDisquesRestentAffiches_SansSmart()
    {
        var source = DisquesComplets().SansClasse("MSFT_StorageReliabilityCounter");

        var rapport = await new AnalyseDisques(source).AnalyserAsync();

        rapport.SmartComplet.Should().BeFalse();
        rapport.RaisonSmart.Should().Contain("administrateur");
        rapport.Disques.Etat.Should().Be(Disponibilite.Disponible);
        rapport.Disques.Elements.Should().HaveCount(2);
        rapport.Disques.Elements.Should().OnlyContain(d => d.TemperatureC == null && d.UsurePourcent == null);
        rapport.Disques.Elements.Single(d => d.Identifiant == "2").Sante.Should().Be(StatutSante.Attention);
    }

    [Fact]
    public async Task CompteurSansDisqueCorrespondant_SmartPartiel_SansInventer()
    {
        var source = DisquesComplets()
            .Avec("MSFT_StorageReliabilityCounter", Ligne(("DeviceId", "99"), ("Temperature", (byte)30)));

        var rapport = await new AnalyseDisques(source).AnalyserAsync();

        rapport.SmartComplet.Should().BeFalse();
        rapport.Disques.Elements.Should().OnlyContain(d => d.TemperatureC == null);
    }

    [Fact]
    public async Task ClasseDisquesAbsente_SectionIndisponible_LecteursOk()
    {
        var source = DisquesComplets().SansClasse("MSFT_PhysicalDisk");

        var rapport = await new AnalyseDisques(source).AnalyserAsync();

        rapport.Disques.Etat.Should().Be(Disponibilite.Indisponible);
        rapport.Disques.Raison.Should().Contain("MSFT_PhysicalDisk");
        rapport.Lecteurs.Etat.Should().Be(Disponibilite.Disponible);
    }

    [Fact]
    public async Task Lecteurs_NeLitQueLesDisquesFixes()
    {
        var source = DisquesComplets();

        await new AnalyseDisques(source).AnalyserAsync();

        source.Appels.Should().Contain(a => a.Classe == "Win32_LogicalDisk" && a.Filtre == "DriveType = 3");
    }

    [Fact]
    public async Task Lecteurs_PourcentageEtStatut()
    {
        var rapport = await new AnalyseDisques(DisquesComplets()).AnalyserAsync();

        var c = rapport.Lecteurs.Elements.Single(l => l.Lettre == "C:");
        c.PourcentLibre.Should().BeApproximately(25.0, 0.001);
        c.Statut.Should().Be(StatutEspace.Sain, "25 % libre est au-dessus du seuil de 20 %");

        var d = rapport.Lecteurs.Elements.Single(l => l.Lettre == "D:");
        d.PourcentLibre.Should().BeApproximately(5.0, 0.001);
        d.Statut.Should().Be(StatutEspace.Critique);
    }

    [Theory]
    [InlineData(50.0, StatutEspace.Sain)]
    [InlineData(20.0, StatutEspace.Sain)]
    [InlineData(19.9, StatutEspace.Attention)]
    [InlineData(10.0, StatutEspace.Attention)]
    [InlineData(9.9, StatutEspace.Critique)]
    [InlineData(null, StatutEspace.Critique)]
    public void SeuilsEspaceLibre(double? pourcent, StatutEspace attendu)
    {
        AnalyseDisques.StatutEspaceDe(pourcent).Should().Be(attendu);
    }

    [Theory]
    [InlineData(0L, StatutSante.Sain)]
    [InlineData(1L, StatutSante.Attention)]
    [InlineData(2L, StatutSante.Critique)]
    [InlineData(5L, StatutSante.Inconnu)]
    [InlineData(9L, StatutSante.Inconnu)]
    [InlineData(null, StatutSante.Inconnu)]
    public void StatutSanteDepuisHealthStatus(long? code, StatutSante attendu)
    {
        AnalyseDisques.StatutDe(code).Should().Be(attendu);
    }

    [Theory]
    [InlineData(3L, TypeMedia.Hdd)]
    [InlineData(4L, TypeMedia.Ssd)]
    [InlineData(5L, TypeMedia.Scm)]
    [InlineData(0L, TypeMedia.Inconnu)]
    public void TypeMediaDepuisMediaType(long code, TypeMedia attendu)
    {
        AnalyseDisques.TypeDe(code).Should().Be(attendu);
    }

    private const string ObjetDisque1 =
        @"{1}\\DESKOP-SHIMAO\root/Microsoft/Windows/Storage/Providers_v2\SPACES_PhysicalDisk.ObjectId=""{4b67c8e4-3258-11ef-8e30-806e6f6e6963}:PD:{088633e3-7f9a-8f41-0e1e-049a15674614}""";
    private const string ObjetCompteur1 =
        @"{1}\\DESKOP-SHIMAO\root/Microsoft/Windows/Storage/Providers_v2\SPACES_StorageReliabilityCounter.ObjectId=""{4b67c8e4-3258-11ef-8e30-806e6f6e6963}:SRC:{aaaaaaaa-1111-2222-3333-444444444444}""";

    [Fact]
    public async Task Association_RelieLeDisqueAuCompteurMemeSiDeviceIdDiffere()
    {
        // DeviceId des compteurs volontairement faux : seule l'association peut faire le lien.
        var source = new FauxSourceWmi()
            .Avec("MSFT_PhysicalDisk", Ligne(("DeviceId", "1"), ("ObjectId", ObjetDisque1), ("FriendlyName", "SSD test"), ("MediaType", (ushort)4), ("HealthStatus", (ushort)0), ("Size", (ulong)(500 * Gio))))
            .Avec("MSFT_StorageReliabilityCounter", Ligne(("DeviceId", "99"), ("ObjectId", ObjetCompteur1), ("Temperature", (byte)44), ("Wear", (byte)7)))
            .Avec("MSFT_PhysicalDiskToStorageReliabilityCounter", Ligne(
                ("PhysicalDisk", @"\\DESKOP-SHIMAO\root\Microsoft\Windows\Storage:MSFT_PhysicalDisk.ObjectId=""{4b67c8e4-3258-11ef-8e30-806e6f6e6963}:PD:{088633e3-7f9a-8f41-0e1e-049a15674614}"""),
                ("StorageReliabilityCounter", @"\\DESKOP-SHIMAO\root\Microsoft\Windows\Storage:MSFT_StorageReliabilityCounter.ObjectId=""{4b67c8e4-3258-11ef-8e30-806e6f6e6963}:SRC:{aaaaaaaa-1111-2222-3333-444444444444}""")));

        var rapport = await new AnalyseDisques(source).AnalyserAsync();

        rapport.Disques.Elements.Single().TemperatureC.Should().Be(44);
        rapport.Disques.Elements.Single().UsurePourcent.Should().Be(7);
        rapport.SmartComplet.Should().BeTrue();
    }

    [Fact]
    public async Task SansAssociation_LaJointureParDeviceIdReste()
    {
        var source = new FauxSourceWmi()
            .Avec("MSFT_PhysicalDisk", Ligne(("DeviceId", "1"), ("ObjectId", ObjetDisque1), ("FriendlyName", "SSD test"), ("MediaType", (ushort)4), ("HealthStatus", (ushort)0), ("Size", (ulong)(500 * Gio))))
            .Avec("MSFT_StorageReliabilityCounter", Ligne(("DeviceId", "1"), ("Temperature", (byte)39)))
            .SansClasse("MSFT_PhysicalDiskToStorageReliabilityCounter");

        var rapport = await new AnalyseDisques(source).AnalyserAsync();

        rapport.Disques.Elements.Single().TemperatureC.Should().Be(39);
    }

    [Fact]
    public void DernierGuid_ExtraitLeGuidDuChemin()
    {
        AnalyseDisques.DernierGuid(ObjetDisque1).Should().Be("{088633e3-7f9a-8f41-0e1e-049a15674614}");
        AnalyseDisques.DernierGuid("pas de guid").Should().BeNull();
        AnalyseDisques.DernierGuid(null).Should().BeNull();
    }

    [Fact]
    public async Task AnnulationDemandee_LanceOperationCanceled()
    {
        var annulation = new CancellationTokenSource();
        annulation.Cancel();

        var action = () => new AnalyseDisques(DisquesComplets()).AnalyserAsync(annulation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }
}
