using FluentAssertions;
using Vitalis.Core.MisesAJour;
using Xunit;

namespace Vitalis.Core.Tests.MisesAJour;

public class InstallateurWingetTests
{
    [Theory]
    [InlineData("7zip.7zip")]
    [InlineData("Microsoft.VCRedist.2015+.x64")]
    [InlineData("rtk-ai.rtk")]
    public void IdentifiantsReels_Valides(string id) => InstallateurWinget.EstIdValide(id).Should().BeTrue();

    [Theory]
    [InlineData("a\" & calc")]
    [InlineData("--all")]
    [InlineData("")]
    [InlineData("x y")]
    public void IdentifiantsDangereux_Refuses(string id)
    {
        InstallateurWinget.EstIdValide(id).Should().BeFalse();
        var action = () => InstallateurWinget.Arguments(id);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Arguments_CibleExacteSilencieuse()
    {
        var a = InstallateurWinget.Arguments("Git.Git");

        a.Should().StartWith("upgrade --id \"Git.Git\" --exact");
        a.Should().Contain("--silent").And.Contain("--accept-package-agreements").And.NotContain("--all");
        InstallateurWinget.Arguments("namazso.PawnIO", installer: true).Should().StartWith("install ");
    }

    [Fact]
    public void Interpreter_Codes()
    {
        InstallateurWinget.Interpreter(0, "").Reussi.Should().BeTrue();
        var redemarrage = InstallateurWinget.Interpreter(unchecked((int)0x8A150109), "");
        redemarrage.Reussi.Should().BeTrue();
        redemarrage.RedemarrageRequis.Should().BeTrue();
        InstallateurWinget.Interpreter(unchecked((int)0x8A15002B), "").Reussi.Should().BeFalse();
        InstallateurWinget.Interpreter(5, "Téléchargement…\nL'installateur a échoué\n").Message.Should().Contain("0x00000005").And.Contain("L'installateur a échoué");
    }
}
