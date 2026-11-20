using FluentAssertions;
using Vitalis.Core.Disques;
using Vitalis.Core.Materiel;
using Vitalis.Core.MisesAJour;
using Vitalis.Core.Rapport;
using Vitalis.Core.TableauDeBord;
using Xunit;

namespace Vitalis.Core.Tests.Rapport;

public class RapportHtmlTests
{
    private const string Utilisateur = "Shimao";
    private const string Machine = "DESKOP-SHIMAO";
    private static readonly DateTime Date = new(2026, 11, 20, 18, 0, 0);

    private static ResultatTableauDeBord Resultat(string nomDisque = "SSD Samsung", string nomVolume = "Données de Shimao")
    {
        var disques = new RapportDisques(
            new Section<DisqueSante>(Disponibilite.Disponible, [new DisqueSante("1", $"{nomDisque} {Utilisateur}", TypeMedia.Ssd, 500_000_000_000, StatutSante.Sain, null, null, null, null, null)]),
            new Section<LecteurEspace>(Disponibilite.Disponible, [new LecteurEspace("C:", nomVolume, 400_000_000_000, 100_000_000_000, 25, StatutEspace.Sain)]),
            false,
            "Windows ne fournit aucun compteur de fiabilité pour ces disques.");

        var maj = new RapportMisesAJour(
            new Section<MiseAJour>(Disponibilite.Disponible, [new MiseAJour("Cumulative", ["KB1"], "Important", false, false)]),
            new DateTime(2026, 11, 20),
            null,
            new Section<MiseAJourLogiciel>(Disponibilite.Disponible, [new MiseAJourLogiciel("Git", "Git.Git", "2.49.0", "2.56.0")]));

        return new ResultatTableauDeBord(86, [new ComposantScore("Disques", 40, 100)], [], Date, null, disques, maj);
    }

    [Fact]
    public void Rapport_NeContientNiNomUtilisateurNiNomMachine()
    {
        var html = new RapportHtml(Utilisateur, Machine).Generer(Resultat());

        html.Should().NotContainEquivalentOf(Utilisateur);
        html.Should().NotContainEquivalentOf(Machine);
    }

    [Fact]
    public void NomVolume_EstExclu()
    {
        var html = new RapportHtml(Utilisateur, Machine).Generer(Resultat(nomVolume: "Photos perso"));

        html.Should().NotContain("Photos perso");
    }

    [Fact]
    public void AdresseMac_EstMasquee()
    {
        var rapport = new RapportHtml(Utilisateur, Machine);

        rapport.Masquer("Carte 00:1A:2B:3C:4D:5E active").Should().Be("Carte [adresse masquée] active");
    }

    [Fact]
    public void Masquer_RemplaceLeNomDansLesTextes()
    {
        new RapportHtml(Utilisateur, Machine).Masquer("SSD de shimao").Should().Be("SSD de [masqué]");
    }

    [Fact]
    public void Rapport_ContientLesSections()
    {
        var html = new RapportHtml(Utilisateur, Machine).Generer(Resultat());

        html.Should().Contain("Score global").And.Contain("86 / 100");
        html.Should().Contain("Disques et espace libre");
        html.Should().Contain("Mises à jour").And.Contain("Git");
        html.Should().Contain("Données exclues");
    }

    [Fact]
    public void Rapport_EchappeLeHtml()
    {
        var html = new RapportHtml(Utilisateur, Machine).Generer(Resultat(nomDisque: "<script>alert(1)</script>"));

        html.Should().NotContain("<script>alert(1)</script>");
        html.Should().Contain("&lt;script&gt;");
    }

    [Fact]
    public void SansAnalyse_SectionIndiqueeNonDisponible()
    {
        var resultat = new ResultatTableauDeBord(null, [], [], Date);

        var html = new RapportHtml(Utilisateur, Machine).Generer(resultat);

        html.Should().Contain("indisponible");
        html.Should().Contain("Analyse matériel non disponible");
    }

    [Fact]
    public void Rapport_PilotesEtCapteurs_SansDonneePersonnelle()
    {
        var ancien = new Vitalis.Core.Pilotes.Pilote($"Carte de {Utilisateur}", "Intel", "1.0", new DateTime(2020, 1, 1), "NET", true, null);
        var pilotes = new Vitalis.Core.Pilotes.RapportPilotes(new Section<Vitalis.Core.Pilotes.Pilote>(Disponibilite.Disponible, [ancien]), 1);
        var dispo = new Vitalis.Core.Pilotes.ComparaisonPilote(
            new Vitalis.Core.Pilotes.PiloteDisponible("Pilote", "Modèle X", "Intel", null, new DateTime(2026, 1, 1), "pci-x", "id", false), ancien);
        var capteurs = new[]
        {
            new Vitalis.Core.SanteLive.ComposantLive("NVIDIA GeForce RTX 3050", Vitalis.Core.SanteLive.Categorie.CarteGraphique,
                [new Vitalis.Core.SanteLive.Mesure("GPU Core", Vitalis.Core.SanteLive.TypeMesure.Temperature, 84)]),
        };

        var html = new RapportHtml(Utilisateur, Machine).Generer(Resultat(), pilotes, [dispo], capteurs);

        html.Should().Contain("Pilotes plus récents").And.Contain("<td>1.0 (").And.Contain("84 °C").And.Contain("chaud");
        html.Should().NotContain(Utilisateur);
    }

    [Fact]
    public void Rapport_SansPilotes_LeDit() =>
        new RapportHtml(Utilisateur, Machine).Generer(Resultat()).Should().Contain("Analyse des pilotes non faite");
}
