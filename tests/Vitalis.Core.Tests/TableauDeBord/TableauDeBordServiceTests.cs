using FluentAssertions;
using Vitalis.Core.Disques;
using Vitalis.Core.Materiel;
using Vitalis.Core.MisesAJour;
using Vitalis.Core.TableauDeBord;
using Vitalis.Core.Tests.Materiel;
using Xunit;
using static Vitalis.Core.Tests.Materiel.FauxSourceWmi;

namespace Vitalis.Core.Tests.TableauDeBord;

public class TableauDeBordServiceTests
{
    private static readonly DateTime Maintenant = new(2026, 11, 20, 12, 0, 0);

    private sealed class SyncProgress : IProgress<int>
    {
        public List<int> Valeurs { get; } = new();
        public void Report(int value) => Valeurs.Add(value);
    }

    private sealed class FauxWua : IServiceWua
    {
        public bool Echec { get; init; }
        public IReadOnlyList<MiseAJour> RechercherEnAttente() =>
            Echec ? throw new InvalidOperationException("WUA indisponible") : [new("Cumulative", ["KB1"], null, false, false)];
        public IReadOnlyList<EntreeHistorique> Historique(int nombreMax) =>
            [new EntreeHistorique(Maintenant.AddDays(-10), "Cumulative", true)];
    }

    private sealed class FauxWinget : IServiceWinget
    {
        public IReadOnlyList<MiseAJourLogiciel> RechercherMisesAJour() => [];
    }

    private static FauxSourceWmi SourceAvecDisques() => MachineComplete()
        .Avec("MSFT_PhysicalDisk",
            Ligne(("DeviceId", "1"), ("FriendlyName", "SSD"), ("MediaType", (ushort)4), ("HealthStatus", (ushort)0), ("Size", (ulong)500_000_000_000)))
        .Avec("Win32_LogicalDisk",
            Ligne(("DeviceID", "C:"), ("VolumeName", "Système"), ("Size", (ulong)400_000_000_000), ("FreeSpace", (ulong)100_000_000_000)));

    private static TableauDeBordService Service(ISourceWmi source, IServiceWua wua) =>
        new(new InventaireMateriel(source), new AnalyseDisques(source), new AnalyseMisesAJour(wua, new FauxWinget()), () => Maintenant);

    [Fact]
    public async Task TousLesServicesOk_QuatreCartesEtUnScore()
    {
        var resultat = await Service(SourceAvecDisques(), new FauxWua()).AnalyserAsync();

        resultat.Cartes.Should().HaveCount(4);
        resultat.Cartes.Should().OnlyContain(c => c.Etat != Disponibilite.Indisponible);
        resultat.Score.Should().NotBeNull();
        resultat.Date.Should().Be(Maintenant);
    }

    [Fact]
    public async Task UneAnalyseEnErreur_LesAutresCartesRestentAffichees()
    {
        var resultat = await Service(SourceAvecDisques(), new FauxWua { Echec = true }).AnalyserAsync();

        var miseAJour = resultat.Cartes.Single(c => c.Titre == "Mises à jour");
        miseAJour.Etat.Should().Be(Disponibilite.Indisponible);
        miseAJour.Raison.Should().Contain("WUA indisponible");

        resultat.Cartes.Single(c => c.Titre == "Matériel").Etat.Should().Be(Disponibilite.Disponible);
        resultat.Cartes.Single(c => c.Titre == "Disques").Etat.Should().Be(Disponibilite.Disponible);
        resultat.Cartes.Single(c => c.Titre == "Espace libre").Valeur.Should().Contain("%");
    }

    [Fact]
    public async Task Score_ComposantMisesAJourIndisponible_NeFaitPasPlonger()
    {
        var resultat = await Service(SourceAvecDisques(), new FauxWua { Echec = true }).AnalyserAsync();

        resultat.Composants.Single(c => c.Nom == "Mises à jour").Valeur.Should().BeNull();
        resultat.Score.Should().NotBeNull("les autres composants restent comptés");
    }

    [Fact]
    public async Task Progression_AtteintCentPourCent()
    {
        var progression = new SyncProgress();

        await Service(SourceAvecDisques(), new FauxWua()).AnalyserAsync(progression);

        progression.Valeurs.Should().HaveCount(3, "trois analyses rapportent leur fin : matériel, disques, mises à jour");
        progression.Valeurs.Should().BeInAscendingOrder();
        progression.Valeurs.Last().Should().Be(100);
    }

    [Fact]
    public async Task Annulation_LanceOperationCanceled()
    {
        var annulation = new CancellationTokenSource();
        annulation.Cancel();

        var action = () => Service(SourceAvecDisques(), new FauxWua()).AnalyserAsync(null, annulation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }
}
