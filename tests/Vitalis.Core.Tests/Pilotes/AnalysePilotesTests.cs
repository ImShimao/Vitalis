using FluentAssertions;
using Vitalis.Core.Materiel;
using Vitalis.Core.Pilotes;
using Vitalis.Core.Tests.Materiel;
using Xunit;
using static Vitalis.Core.Tests.Materiel.FauxSourceWmi;

namespace Vitalis.Core.Tests.Pilotes;

public class AnalysePilotesTests
{
    private static readonly DateTime Maintenant = new(2026, 11, 20);

    private static FauxSourceWmi Pilotes(params (string Nom, string? Fabricant, string? Date)[] lignes)
    {
        var rows = lignes.Select(l => Ligne(
            ("DeviceName", l.Nom), ("Manufacturer", l.Fabricant), ("DriverVersion", "1.0"),
            ("DriverDate", l.Date), ("DeviceClass", "DISPLAY"))).ToArray();
        return new FauxSourceWmi().Avec("Win32_PnPSignedDriver", rows);
    }

    [Fact]
    public void RegleDeuxAns_AncienSiPlusDeDeuxAns()
    {
        AnalysePilotes.EstAncien(new DateTime(2024, 11, 19), Maintenant).Should().BeTrue();
        AnalysePilotes.EstAncien(new DateTime(2024, 11, 21), Maintenant).Should().BeFalse();
    }

    [Fact]
    public void SansDate_PasMarqueAncien()
    {
        AnalysePilotes.EstAncien(null, Maintenant).Should().BeFalse();
    }

    [Fact]
    public void DateFormatCim_EtFormatTexte()
    {
        AnalysePilotes.DateDriver("20240510000000.000000+000").Should().Be(new DateTime(2024, 5, 10));
        AnalysePilotes.DateDriver("2006-06-21 02:00:00").Should().Be(new DateTime(2006, 6, 21));
        AnalysePilotes.DateDriver("pas une date").Should().BeNull();
    }

    [Fact]
    public void Analyse_PilotesAnciensComptes_LiensSelonFabricant()
    {
        var source = Pilotes(
            ("NVIDIA GeForce RTX 3060", "NVIDIA", "20230101000000.000000+000"),
            ("Realtek Audio", "Realtek Semiconductor Corp.", "20251201000000.000000+000"),
            ("Périphérique inconnu", "Acme", "20200101000000.000000+000"));

        var rapport = new AnalysePilotes(source).Analyser(Maintenant);

        rapport.Pilotes.Etat.Should().Be(Disponibilite.Disponible);
        rapport.Pilotes.Elements.Should().HaveCount(3);
        rapport.NombreAnciens.Should().Be(2);

        var nvidia = rapport.Pilotes.Elements.Single(p => p.Appareil.StartsWith("NVIDIA"));
        nvidia.Ancien.Should().BeTrue();
        nvidia.Lien!.EstVerifie.Should().BeTrue();

        var inconnu = rapport.Pilotes.Elements.Single(p => p.Appareil.StartsWith("Périph"));
        inconnu.Lien.Should().BeNull("fabricant inconnu : pas de lien inventé");
    }

    [Fact]
    public void FabricantNonVerifie_LienSansUrl()
    {
        var lien = LiensFabricants.Trouver("Kingston");

        lien.Should().NotBeNull();
        lien!.EstVerifie.Should().BeFalse();
        lien.Statut.Should().Contain("non vérifié");
    }

    [Fact]
    public void FabricantVerifie_UrlHttps()
    {
        foreach (var nom in new[] { "NVIDIA", "Intel", "Realtek", "ASUS", "AMD", "Micro-Star INT'L CO., LTD.", "Gigabyte", "Dell", "Lenovo", "HP", "Acer" })
        {
            var lien = LiensFabricants.Trouver(nom);
            lien!.EstVerifie.Should().BeTrue(nom);
            lien.Url.Should().StartWith("https://");
            lien.VerifieLe.Should().Be(LiensFabricants.DateVerification);
        }
    }

    [Fact]
    public void SansLigne_EtatIndisponible()
    {
        var rapport = new AnalysePilotes(new FauxSourceWmi()).Analyser(Maintenant);

        rapport.Pilotes.Etat.Should().Be(Disponibilite.Indisponible);
    }

    [Fact]
    public void ClasseAbsente_EtatIndisponibleAvecRaison()
    {
        var rapport = new AnalysePilotes(new FauxSourceWmi().SansClasse("Win32_PnPSignedDriver")).Analyser(Maintenant);

        rapport.Pilotes.Etat.Should().Be(Disponibilite.Indisponible);
        rapport.Pilotes.Raison.Should().Contain("Win32_PnPSignedDriver");
    }
}
