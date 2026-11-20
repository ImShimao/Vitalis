using FluentAssertions;
using Vitalis.Core.Demarrage;
using Xunit;

namespace Vitalis.Core.Tests.Demarrage;

public class DemarrageServiceTests : IDisposable
{
    private sealed class FauxRegistre : IRegistreDemarrage
    {
        public List<EntreeDemarrage> Entrees { get; } = new();
        public IReadOnlyList<EntreeDemarrage> Lister() => Entrees.ToList();
        public void Ecrire(EntreeDemarrage entree) => Entrees.Add(entree);
        public void Supprimer(EntreeDemarrage entree) => Entrees.RemoveAll(e => e.Nom == entree.Nom && e.Emplacement == entree.Emplacement);
    }

    private readonly string _dossier = Path.Combine(Path.GetTempPath(), "vitalis-tests-" + Guid.NewGuid().ToString("N"));
    private string Fichier => Path.Combine(_dossier, "demarrage-desactive.json");

    public void Dispose()
    {
        if (Directory.Exists(_dossier))
        {
            Directory.Delete(_dossier, recursive: true);
        }
    }

    private static EntreeDemarrage Entree(string nom = "Spotify") =>
        new(nom, @"""C:\Users\Test\AppData\Spotify.exe"" /minimized", @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run");

    [Fact]
    public void Desactiver_SansConfirmation_EstRefuse()
    {
        var registre = new FauxRegistre();
        registre.Entrees.Add(Entree());
        var service = new DemarrageService(registre, Fichier);

        var action = () => service.Desactiver(Entree(), confirme: false);

        action.Should().Throw<InvalidOperationException>();
        registre.Entrees.Should().HaveCount(1, "rien ne doit être modifié sans confirmation");
    }

    [Fact]
    public void Desactiver_SauvegardeEtRetireLEntree()
    {
        var registre = new FauxRegistre();
        registre.Entrees.Add(Entree());
        var service = new DemarrageService(registre, Fichier);

        service.Desactiver(Entree(), confirme: true);

        registre.Entrees.Should().BeEmpty();
        service.Desactivees().Should().ContainSingle(e => e.Nom == "Spotify" && e.Commande.Contains("minimized"));
    }

    [Fact]
    public void Restaurer_RemetLaCommandeExacte()
    {
        var registre = new FauxRegistre();
        var original = Entree();
        registre.Entrees.Add(original);
        var service = new DemarrageService(registre, Fichier);
        service.Desactiver(original, confirme: true);

        service.Restaurer(original);

        registre.Entrees.Should().ContainSingle().Which.Should().Be(original);
        service.Desactivees().Should().BeEmpty();
    }

    [Fact]
    public void Desactiver_DeuxFois_PasDeDoublon()
    {
        var registre = new FauxRegistre();
        registre.Entrees.Add(Entree());
        var service = new DemarrageService(registre, Fichier);

        service.Desactiver(Entree(), confirme: true);
        registre.Entrees.Add(Entree());
        service.Desactiver(Entree(), confirme: true);

        service.Desactivees().Should().ContainSingle();
    }

    [Fact]
    public void Lister_RenvoieLesEntreesDuRegistre()
    {
        var registre = new FauxRegistre();
        registre.Entrees.Add(Entree("A"));
        registre.Entrees.Add(Entree("B"));

        new DemarrageService(registre, Fichier).Lister().Should().HaveCount(2);
    }
}
