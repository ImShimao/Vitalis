using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Vitalis.Core.Navigation;

namespace Vitalis.App.Views;

/// <summary>Page vide : affiche le titre et l'étape où elle sera remplie.</summary>
public sealed partial class PagePlaceholder : Page
{
    public PagePlaceholder()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is PageDefinition page)
        {
            TitrePage.Text = page.Titre;
            TexteEtape.Text = $"Cette page sera remplie à l'étape {page.EtapeDisponible}.";
        }
    }
}
