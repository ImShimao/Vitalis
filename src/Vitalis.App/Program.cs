using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Vitalis.Core.Disques;
using Vitalis.Core.Journal;
using Vitalis.Core.Materiel;
using Vitalis.Core.MisesAJour;
using Vitalis.Core.Parametres;
using Vitalis.Core.Pilotes;
using Vitalis.Core.TableauDeBord;
using WinRT;

namespace Vitalis.App;

/// <summary>
/// Point d'entrée explicite. Une application WinUI 3 non empaquetée (portable) doit initialiser
/// le Windows App Runtime avant de créer la moindre fenêtre ; sinon elle se ferme au démarrage.
/// Version 0x00010008 = Windows App SDK 1.8.
/// </summary>
public static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
#if !VITALIS_AUTONOME
        // Version autonome : le Windows App SDK est embarqué, il ne faut pas chercher le runtime installé.
        Bootstrap.Initialize(0x00010008);
#endif
        try
        {
            ComWrappersSupport.InitializeComWrappers();
            if (args.Contains(AnalysePlanifiee.Argument))
            {
                // Lancé par le Planificateur de tâches : analyse sans fenêtre, notification, puis fin.
                AnalyseSansFenetre();
                return;
            }

            Application.Start(parametres =>
            {
                var contexte = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(contexte);
                var application = new App();
            });
        }
        finally
        {
#if !VITALIS_AUTONOME
            Bootstrap.Shutdown();
#endif
        }
    }

    private static void AnalyseSansFenetre()
    {
        var dossier = Views.ParametresPage.DossierLocal;
        var prefs = StockagePreferences.Charger(Path.Combine(dossier, "preferences.json"));
        JournalVitalis.Initialiser(dossier, prefs.ModeDiagnostic);
        JournalVitalis.Information("Analyse planifiée : début.");
        try
        {
            var resultat = Task.Run(() => new TableauDeBordService(
                    new InventaireMateriel(new WmiSource()),
                    new AnalyseDisques(new WmiSource()),
                    new AnalyseMisesAJour(new WuaCom(), new WingetCli()))
                .AnalyserAsync()).GetAwaiter().GetResult();

            int? pilotes;
            try
            {
                pilotes = CataloguePilotes.Rechercher().Count(p => !p.Masque);
            }
            catch (Exception)
            {
                pilotes = null;
            }

            JournalVitalis.Information($"Analyse planifiée : score {resultat.Score?.ToString() ?? "n/d"}.");
            if (prefs.NotificationsActives)
            {
                var (titre, texte) = AnalysePlanifiee.Notification(resultat, pilotes);
                Notifier(titre, texte);
            }
        }
        catch (Exception e)
        {
            JournalVitalis.Erreur("Analyse planifiée impossible", e);
        }
    }

    /// <summary>Notification Windows (Windows App SDK, AppNotificationManager). Un clic ouvre Vitalis.</summary>
    public static void Notifier(string titre, string texte)
    {
        try
        {
            AppNotificationManager.Default.Register();
            AppNotificationManager.Default.Show(new AppNotificationBuilder().AddText(titre).AddText(texte).BuildNotification());
        }
        catch (Exception e)
        {
            JournalVitalis.Erreur("Notification impossible", e);
        }
    }
}
