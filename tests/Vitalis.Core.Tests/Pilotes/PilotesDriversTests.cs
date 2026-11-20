using FluentAssertions;
using Vitalis.Core.Pilotes;
using Vitalis.Core.Tests.Materiel;
using Xunit;
using static Vitalis.Core.Tests.Materiel.FauxSourceWmi;

namespace Vitalis.Core.Tests.Pilotes;

/// <summary>Pilotes génériques, liens par carte graphique, comparaison avec le catalogue Windows Update.</summary>
public class PilotesDriversTests
{
    private static readonly DateTime Maintenant = new(2026, 11, 20);

    [Theory]
    [InlineData("Microsoft", true)]
    [InlineData("(Standard system devices)", true)]
    [InlineData("(Périphériques système standard)", true)]
    [InlineData("Generic USB xHCI Host Controller", true)]
    [InlineData("", true)]
    [InlineData("Intel", false)]
    [InlineData("NVIDIA", false)]
    [InlineData("Microsoft Surface", false)]
    public void Generique_SelonFabricant(string fabricant, bool attendu) =>
        LiensFabricants.EstGenerique(fabricant).Should().Be(attendu);

    [Fact]
    public void PiloteGeneriqueAncien_PasCompteAncien_SansLien()
    {
        var source = new FauxSourceWmi().Avec("Win32_PnPSignedDriver",
            Ligne(("DeviceName", "Volume"), ("Manufacturer", "Microsoft"), ("DriverVersion", "10.0"), ("DriverDate", "20060621000000.000000+000"), ("DeviceClass", "VOLUME")),
            Ligne(("DeviceName", "Carte réseau"), ("Manufacturer", "Intel"), ("DriverVersion", "1.0"), ("DriverDate", "20200101000000.000000+000"), ("DeviceClass", "NET")));

        var r = new AnalysePilotes(source).Analyser(Maintenant);

        r.NombreAnciens.Should().Be(1);
        var generique = r.Pilotes.Elements.Single(p => p.Appareil == "Volume");
        generique.Generique.Should().BeTrue();
        generique.Ancien.Should().BeFalse();
        generique.Lien.Should().BeNull();
    }

    [Theory]
    [InlineData("NVIDIA GeForce RTX 3050", "NVIDIA")]
    [InlineData("AMD Radeon RX 7800 XT", "AMD")]
    [InlineData("Intel(R) UHD Graphics 630", "Intel")]
    public void CarteGraphique_LienOfficiel(string gpu, string fabricant)
    {
        var lien = LiensFabricants.Trouver(gpu);

        lien!.Nom.Should().Be(fabricant);
        lien.Url.Should().StartWith("https://");
    }

    [Fact]
    public void Comparer_AssocieParIdentifiantMateriel()
    {
        var installe = new Pilote("Apple Mobile Device USB Device", "Apple", "486.0", new DateTime(2020, 1, 1), "USB", true, null, false,
            @"USB\VID_05AC&PID_12A8&MI_01&REV_0001");
        var autre = new Pilote("Clavier", "(Standard keyboards)", "10.0", null, "Keyboard", false, null, true, @"HID\VID_046D");
        var dispo = new PiloteDisponible("Apple USB", "Apple Mobile Device USB Device", "Apple, Inc.", "OtherHardware",
            new DateTime(2025, 4, 25), @"usb\vid_05ac&pid_12a8&mi_01", "id-1", false);
        var orphelin = dispo with { HardwareId = @"usb\vid_ffff", UpdateId = "id-2" };

        var r = CataloguePilotes.Comparer([dispo, orphelin], [autre, installe]);

        r[0].Installe.Should().Be(installe);
        r[1].Installe.Should().BeNull("aucun matériel installé ne correspond : rien d'inventé");
    }
}
