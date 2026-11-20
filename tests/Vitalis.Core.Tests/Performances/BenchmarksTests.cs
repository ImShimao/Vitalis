using FluentAssertions;
using Vitalis.Core.Performances;
using Xunit;

namespace Vitalis.Core.Tests.Performances;

public class BenchmarksTests : IDisposable
{
    private readonly string _dossier = Path.Combine(Path.GetTempPath(), "vitalis-bench-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dossier))
        {
            Directory.Delete(_dossier, recursive: true);
        }
    }

    [Theory]
    [InlineData(10, 4)]
    [InlineData(100, 25)]
    [InlineData(1, 0)]
    public void Crible_ComptePremiersCorrectement(int limite, int attendu)
    {
        Benchmarks.Crible(limite).Should().Be(attendu);
    }

    [Fact]
    public void Mediane_ImpairEtPair()
    {
        Benchmarks.Mediane([3, 1, 2]).Should().Be(2);
        Benchmarks.Mediane([4, 1, 2, 3]).Should().Be(2.5);
    }

    [Fact]
    public void Cpu_RenvoieUneMesureAvecMethode()
    {
        var r = Benchmarks.Cpu();

        r.Mesure.Should().BeTrue();
        r.Score.Should().BeGreaterThan(0);
        r.Methode.Should().Contain("Crible");
        r.Limites.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Gpu_NonMesure_PasDeScoreInvente()
    {
        var r = Benchmarks.GpuIndisponible();

        r.Mesure.Should().BeFalse();
        r.Score.Should().BeNull();
    }

    [Fact]
    public void Disque_EcritEtSupprimeLeFichierTemporaire()
    {
        var r = Benchmarks.Disque(_dossier);

        r.Mesure.Should().BeTrue();
        r.Score.Should().BeGreaterThan(0);
        Directory.GetFiles(_dossier).Should().BeEmpty("le fichier de benchmark est supprimé");
    }

    [Fact]
    public void Variation_EntreDeuxAnalyses()
    {
        var avant = new AnalysePerformance(DateTime.Now.AddDays(-1), [new ResultatBenchmark("Processeur", null, "ms", 100, "m", "l", true)]);
        var apres = new AnalysePerformance(DateTime.Now, [new ResultatBenchmark("Processeur", null, "ms", 110, "m", "l", true)]);

        HistoriquePerformances.Variation(avant, apres, "Processeur").Should().Be(10.0);
        HistoriquePerformances.Variation(avant, apres, "Mémoire vive").Should().BeNull();
    }

    [Fact]
    public void Historique_AjouteEtRelit()
    {
        var fichier = Path.Combine(_dossier, "historique.json");
        var analyse = new AnalysePerformance(new DateTime(2026, 11, 20), [new ResultatBenchmark("Processeur", null, "ms", 50, "m", "l", true)]);

        HistoriquePerformances.Ajouter(fichier, analyse);
        HistoriquePerformances.Ajouter(fichier, analyse);

        HistoriquePerformances.Charger(fichier).Should().HaveCount(2);
    }
}
