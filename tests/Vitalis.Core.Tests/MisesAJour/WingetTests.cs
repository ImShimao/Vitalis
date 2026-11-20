using FluentAssertions;
using Vitalis.Core.MisesAJour;
using Xunit;

namespace Vitalis.Core.Tests.MisesAJour;

/// <summary>Sortie réelle de « winget upgrade » (machine de développement, 2026-11-20).</summary>
public class WingetTests
{
    private const string SortieReelle = """
Nom                                                          ID                                Version         Disponible
----------------------------------------------------------------------------------------------------------------------------
7-Zip 24.09 (x64)                                            7zip.7zip                         24.09           26.06
ASIO4ALL                                                     MichaelTippach.ASIO4ALL           2.15            2.22
Git                                                          Git.Git                           2.49.0          2.56.0.4
Microsoft Visual C++ v14 Redistributable (x64) - 14.51.36247 Microsoft.VCRedist.2015+.x64      14.51.36247.0   14.52.37029.0
Python 3.13.1 (64-bit)                                       Python.Python.3.13                3.13.1          3.13.17
rtk                                                          rtk-ai.rtk                        v0.50.0         1.2.1
Ubisoft Connect                                              Ubisoft.Connect                   < 175.2.0.13423 175.2.0.13423
WinRAR 7.01 (64-bit)                                         RARLab.WinRAR                     7.01.0          7.23.0
8 mises à niveau disponibles.
1 package(s) a(ont) des numéros de version qui ne peuvent pas être déterminés. Utilisez --include-unknown pour afficher tous les résultats.
""";

    [Fact]
    public void SortieReelle_LesLignesSontLues()
    {
        var resultats = AnalyseurWinget.Lire(SortieReelle);

        resultats.Should().HaveCount(8);
    }

    [Fact]
    public void Ligne_ChampsCorrectement()
    {
        var git = AnalyseurWinget.Lire(SortieReelle).Single(m => m.Id == "Git.Git");

        git.Nom.Should().Be("Git");
        git.VersionActuelle.Should().Be("2.49.0");
        git.VersionDisponible.Should().Be("2.56.0.4");
    }

    [Fact]
    public void NomAvecTiretsEtParentheses_NonCoupe()
    {
        var vc = AnalyseurWinget.Lire(SortieReelle).Single(m => m.Id == "Microsoft.VCRedist.2015+.x64");

        vc.Nom.Should().Be("Microsoft Visual C++ v14 Redistributable (x64) - 14.51.36247");
    }

    [Fact]
    public void VersionActuelleAvecPrefixeInferieur_Nettoyee()
    {
        var ubisoft = AnalyseurWinget.Lire(SortieReelle).Single(m => m.Id == "Ubisoft.Connect");

        ubisoft.VersionActuelle.Should().Be("175.2.0.13423");
        ubisoft.VersionDisponible.Should().Be("175.2.0.13423");
    }

    [Fact]
    public void Pied_DePageEtAvertissements_Ignores()
    {
        AnalyseurWinget.Lire(SortieReelle).Should().OnlyContain(m => !m.Nom.Contains("mises à niveau"));
    }

    [Fact]
    public void SansMiseAJour_ListeVide()
    {
        AnalyseurWinget.Lire("Aucune mise à niveau disponible.\r\n").Should().BeEmpty();
    }

    [Fact]
    public void CaracteresDeCommandeEtRotation_IgnoresSansCasser()
    {
        var sortie = "Nom   ID   Version   Disponible\r\n" +
                     "-------------------------------------------\r\n" +
                     "\u0008\u0008|  \r\n" +
                     "Git                 Git.Git                 2.49.0          2.56.0\r\n";

        AnalyseurWinget.Lire(sortie).Should().ContainSingle(m => m.Id == "Git.Git");
    }
}
