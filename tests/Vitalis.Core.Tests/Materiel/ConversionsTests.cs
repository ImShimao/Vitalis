using FluentAssertions;
using Vitalis.Core.Materiel;
using Xunit;
using static Vitalis.Core.Materiel.Conversions;

namespace Vitalis.Core.Tests.Materiel;

public class ConversionsTests
{
    [Fact]
    public void DecoderUtf16_S_ArreteAuPremierZero()
    {
        DecoderUtf16(new ushort[] { 'D', 'E', 'L', 0, 0 }).Should().Be("DEL");
    }

    [Fact]
    public void DecoderUtf16_ValeurAbsenteOuVide_RenvoieNull()
    {
        DecoderUtf16(null).Should().BeNull();
        DecoderUtf16(new ushort[] { 0, 0 }).Should().BeNull();
    }

    [Fact]
    public void DateCim_ConvertitLeFormatWmi()
    {
        DateCim("20240510000000.000000+000").Should().Be(new DateTime(2024, 5, 10));
    }

    [Fact]
    public void DateCim_MalFormee_RenvoieNull()
    {
        DateCim("pas une date").Should().BeNull();
        DateCim(null).Should().BeNull();
    }

    [Fact]
    public void Entier_AccepteLesTypesNumeriquesEtLesChaines()
    {
        var ligne = FauxSourceWmi.Ligne(("a", (uint)12), ("b", "42"), ("c", "abc"), ("d", null));

        Entier(ligne, "a").Should().Be(12);
        Entier(ligne, "b").Should().Be(42);
        Entier(ligne, "c").Should().BeNull();
        Entier(ligne, "d").Should().BeNull();
        Entier(ligne, "absente").Should().BeNull();
    }

    [Fact]
    public void Texte_RetireLesEspacesEtLesVidesDevientNull()
    {
        var ligne = FauxSourceWmi.Ligne(("a", "  Intel  "), ("b", "   "));

        Texte(ligne, "a").Should().Be("Intel");
        Texte(ligne, "b").Should().BeNull();
    }

    [Fact]
    public void MemoireTotale_SommeDesBarrettes_ouNullSiAucuneCapacite()
    {
        var modules = new[]
        {
            new Memoire("DIMM 1", 8L * 1024 * 1024 * 1024, 3200, null),
            new Memoire("DIMM 2", null, null, null),
            new Memoire("DIMM 3", 4L * 1024 * 1024 * 1024, 2666, null),
        };

        MemoireTotaleOctets(modules).Should().Be(12L * 1024 * 1024 * 1024);
        MemoireTotaleOctets(Array.Empty<Memoire>()).Should().BeNull();
    }
}
