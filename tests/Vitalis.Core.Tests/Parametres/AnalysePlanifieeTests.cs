using FluentAssertions;
using Vitalis.Core.Materiel;
using Vitalis.Core.MisesAJour;
using Vitalis.Core.Parametres;
using Vitalis.Core.TableauDeBord;
using Xunit;

namespace Vitalis.Core.Tests.Parametres;

public class AnalysePlanifieeTests
{
    private static ResultatTableauDeBord Resultat(int windows, int logiciels) => new(
        82, [], [], DateTime.Now,
        MisesAJour: new RapportMisesAJour(
            new Section<MiseAJour>(Disponibilite.Disponible, Enumerable.Range(0, windows).Select(i => new MiseAJour($"KB{i}", [], null, false, false)).ToList()),
            null, null,
            new Section<MiseAJourLogiciel>(Disponibilite.Disponible, Enumerable.Range(0, logiciels).Select(i => new MiseAJourLogiciel($"L{i}", $"a.b{i}", "1", "2")).ToList())));

    [Fact]
    public void Notification_ResumeCeQuiDemandeUneAction()
    {
        var (titre, texte) = AnalysePlanifiee.Notification(Resultat(1, 8), 2);

        titre.Should().Be("Vitalis : score 82/100");
        texte.Should().Be("1 mise à jour Windows · 8 logiciels à mettre à jour · 2 pilotes plus récents. Ouvrez Vitalis pour les installer.");
    }

    [Fact]
    public void Notification_ToutAJour() =>
        AnalysePlanifiee.Notification(Resultat(0, 0), 0).Texte.Should().Be("Tout est à jour. Rien à faire.");

    [Theory]
    [InlineData("2026-11-20", "2026-11-23")] // vendredi → lundi suivant
    [InlineData("2026-11-23", "2026-11-30")] // lundi → lundi suivant
    public void ProchainLundi(string depuis, string attendu) =>
        AnalysePlanifiee.ProchainLundi(DateTime.Parse(depuis)).Should().Be(DateTime.Parse(attendu));
}
