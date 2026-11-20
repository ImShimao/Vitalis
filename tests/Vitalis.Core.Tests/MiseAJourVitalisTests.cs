using FluentAssertions;
using Xunit;

namespace Vitalis.Core.Tests;

public class MiseAJourVitalisTests
{
    [Theory]
    [InlineData("v0.3.0", "0.2.0", true)]
    [InlineData("0.2.1", "0.2.0", true)]
    [InlineData("v1.0", "0.9.9", true)]
    [InlineData("v0.2.0", "0.2.0", false)]
    [InlineData("v0.1.9", "0.2.0", false)]
    [InlineData("v0.3.0-beta", "0.2.0", true)]
    [InlineData("nimporte", "0.2.0", false)]
    public void EstPlusRecente(string etiquette, string actuelle, bool attendu) =>
        MiseAJourVitalis.EstPlusRecente(etiquette, actuelle).Should().Be(attendu);
}
