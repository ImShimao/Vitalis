using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
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
}
