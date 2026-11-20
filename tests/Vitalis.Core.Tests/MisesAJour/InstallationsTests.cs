using Xunit;
using Vitalis.Core.MisesAJour;

namespace Vitalis.Core.Tests.MisesAJour;

// Installations est statique (état de la session) : les tests ne s'exécutent pas en parallèle entre eux.
[Collection("Installations")]
public class InstallationsTests
{
    [Fact]
    public async Task Suivi_et_bilan_restent_disponibles_apres_la_serie()
    {
        var bloque = new TaskCompletionSource<ResultatInstallation>();
        var serie = Installations.LancerAsync("test-a", ["a.un", "a.deux"],
            (id, _) => id == "a.un" ? bloque.Task : throw new InvalidOperationException("refus"),
            rs => new BilanInstallation(rs.All(r => r.Reussi), "Bilan", $"{rs.Count(r => r.Reussi)} réussi", false));

        // Pendant l'installation : état visible par n'importe quelle page, nouvelle série refusée.
        Assert.True(Installations.EnCours);
        Assert.Equal(EtatInstallation.EnCours, Installations.Suivi("a.un")!.Etat);
        Assert.False(await Installations.LancerAsync("test-a", ["x"], (_, _) => Task.FromResult(new ResultatInstallation(true, "")), _ => null!));

        bloque.SetResult(new ResultatInstallation(true, "ok"));
        Assert.True(await serie);

        Assert.False(Installations.EnCours);
        Assert.Equal(EtatInstallation.Reussie, Installations.Suivi("a.un")!.Etat);
        Assert.Equal(EtatInstallation.Echouee, Installations.Suivi("a.deux")!.Etat);
        Assert.Contains("refus", Installations.Suivi("a.deux")!.Message);
        Assert.Equal("1 réussi", Installations.Bilan("test-a")!.Message);
        Assert.Null(Installations.Suivi("x"));
    }

    [Fact]
    public async Task En_bloc_chaque_cle_prend_le_resultat_commun()
    {
        await Installations.LancerEnBlocAsync("test-b", ["b.un", "b.deux"],
            _ => throw new TimeoutException("délai"),
            r => new BilanInstallation(r.Reussi, "Pilotes", r.Message, r.RedemarrageRequis));

        Assert.Equal(EtatInstallation.Echouee, Installations.Suivi("b.un")!.Etat);
        Assert.Equal(EtatInstallation.Echouee, Installations.Suivi("b.deux")!.Etat);
        Assert.False(Installations.Bilan("test-b")!.Reussi);
    }
}
