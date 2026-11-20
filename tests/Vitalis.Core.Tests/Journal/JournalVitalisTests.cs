using FluentAssertions;
using Vitalis.Core.Journal;
using Xunit;

namespace Vitalis.Core.Tests.Journal;

public class JournalVitalisTests
{
    [Fact]
    public void Nettoyer_MasqueLeNomDUtilisateurDansLesChemins()
    {
        JournalVitalis.Nettoyer(@"Fichier C:\Users\Shimao\AppData\x.log introuvable")
            .Should().Be(@"Fichier C:\Users\[utilisateur]\AppData\x.log introuvable");
    }

    [Fact]
    public void Nettoyer_TexteSansChemin_Inchange()
    {
        JournalVitalis.Nettoyer("Analyse terminée").Should().Be("Analyse terminée");
        JournalVitalis.Nettoyer(null).Should().BeEmpty();
    }

    [Fact]
    public void Initialiser_CreeLeDossierDeJournaux()
    {
        var dossier = Path.Combine(Path.GetTempPath(), "vitalis-journal-" + Guid.NewGuid().ToString("N"));
        try
        {
            JournalVitalis.Initialiser(dossier);
            JournalVitalis.Information("test");
            JournalVitalis.Fermer();

            Directory.Exists(JournalVitalis.DossierJournal(dossier)).Should().BeTrue();
            Directory.GetFiles(JournalVitalis.DossierJournal(dossier), "vitalis-*.log").Should().NotBeEmpty();
        }
        finally
        {
            JournalVitalis.Fermer();
            if (Directory.Exists(dossier))
            {
                Directory.Delete(dossier, recursive: true);
            }
        }
    }
}
