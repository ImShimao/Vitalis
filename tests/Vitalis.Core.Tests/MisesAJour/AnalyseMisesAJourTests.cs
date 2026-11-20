using FluentAssertions;
using Vitalis.Core.Materiel;
using Vitalis.Core.MisesAJour;
using Xunit;

namespace Vitalis.Core.Tests.MisesAJour;

public class AnalyseMisesAJourTests
{
    private sealed class FauxWua : IServiceWua
    {
        public Func<IReadOnlyList<MiseAJour>>? FonctionRecherche { get; init; }
        public Func<IReadOnlyList<EntreeHistorique>>? FonctionHistorique { get; init; }

        public IReadOnlyList<MiseAJour> RechercherEnAttente() => FonctionRecherche?.Invoke() ?? [];
        public IReadOnlyList<EntreeHistorique> Historique(int nombreMax) => FonctionHistorique?.Invoke() ?? [];
    }

    private static MiseAJour Mise(string titre, bool pilote = false) =>
        new(titre, ["KB5040000"], "Important", false, pilote);

    [Fact]
    public async Task MisesEnAttente_LogicielsEtPilotes()
    {
        var wua = new FauxWua
        {
            FonctionRecherche = () => [Mise("Mise à jour cumulative"), Mise("Pilote GPU", pilote: true)],
        };

        var rapport = await new AnalyseMisesAJour(wua).AnalyserAsync();

        rapport.EnAttente.Etat.Should().Be(Disponibilite.Disponible);
        rapport.EnAttente.Elements.Should().HaveCount(2);
        rapport.EnAttente.Elements.Should().ContainSingle(m => m.Pilote);
        rapport.EnAttente.Elements[0].Kb.Should().ContainSingle("KB5040000");
    }

    [Fact]
    public async Task AucuneMiseAJour_EtatDisponibleAvecMessage()
    {
        var rapport = await new AnalyseMisesAJour(new FauxWua()).AnalyserAsync();

        rapport.EnAttente.Etat.Should().Be(Disponibilite.Disponible);
        rapport.EnAttente.Elements.Should().BeEmpty();
        rapport.EnAttente.Raison.Should().Contain("Aucune mise à jour");
    }

    [Fact]
    public async Task RechercheEnErreur_SectionIndisponible_HistoriqueLu()
    {
        var wua = new FauxWua
        {
            FonctionRecherche = () => throw new System.Runtime.InteropServices.COMException("Service arrêté."),
            FonctionHistorique = () => [new EntreeHistorique(new DateTime(2026, 10, 14), "Cumulative", true)],
        };

        var rapport = await new AnalyseMisesAJour(wua).AnalyserAsync();

        rapport.EnAttente.Etat.Should().Be(Disponibilite.Indisponible);
        rapport.EnAttente.Raison.Should().Contain("Service arrêté");
        rapport.DerniereInstallationReussie.Should().Be(new DateTime(2026, 10, 14));
    }

    [Fact]
    public async Task DerniereInstallation_EstLaPlusRecenteReussie()
    {
        var wua = new FauxWua
        {
            FonctionHistorique = () =>
            [
                new EntreeHistorique(new DateTime(2026, 11, 2), "Échec", false),
                new EntreeHistorique(new DateTime(2026, 10, 14), "Cumulative", true),
                new EntreeHistorique(new DateTime(2026, 9, 10), "Ancienne", true),
            ],
        };

        var rapport = await new AnalyseMisesAJour(wua).AnalyserAsync();

        rapport.DerniereInstallationReussie.Should().Be(new DateTime(2026, 10, 14));
        rapport.RaisonHistorique.Should().BeNull();
    }

    [Fact]
    public async Task HistoriqueVide_PasDeDateEtRaison()
    {
        var rapport = await new AnalyseMisesAJour(new FauxWua()).AnalyserAsync();

        rapport.DerniereInstallationReussie.Should().BeNull();
        rapport.RaisonHistorique.Should().Contain("Aucune installation");
    }

    [Fact]
    public async Task AnnulationDemandee_LanceOperationCanceled()
    {
        var annulation = new CancellationTokenSource();
        annulation.Cancel();

        var action = () => new AnalyseMisesAJour(new FauxWua()).AnalyserAsync(annulation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }
}
