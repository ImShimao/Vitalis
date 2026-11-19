using FluentAssertions;
using Vitalis.Core;
using Xunit;

namespace Vitalis.Core.Tests;

public class AppInfoTests
{
    [Fact]
    public void Nom_EstVitalis()
    {
        AppInfo.Nom.Should().Be("Vitalis");
    }

    [Fact]
    public void Version_EstAuFormatSemver()
    {
        Version.TryParse(AppInfo.Version, out _).Should().BeTrue();
        AppInfo.Version.Should().Be("0.1.0");
    }
}
