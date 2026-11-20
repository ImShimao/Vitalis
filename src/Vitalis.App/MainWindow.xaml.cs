using System.Collections.Generic;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Input;
using Vitalis.App.ViewModels;
using Vitalis.App.Views;
using Vitalis.Core;
using Vitalis.Core.Navigation;
using Windows.System;
using Windows.UI.Core;

namespace Vitalis.App;

public sealed partial class MainWindow : Window
{
    private readonly Dictionary<string, Button> _boutons = new();
    private IReadOnlyList<PageDefinition> _pages = [];

    public MainWindow()
    {
        InitializeComponent();
        Title = AppInfo.Nom;

        // La barre de titre suit le mode du système, comme le contenu (sinon elle reste claire en mode sombre).
        AppWindow.TitleBar.PreferredTheme = Microsoft.UI.Windowing.TitleBarTheme.UseDefaultAppMode;

        // Mica seulement sur Windows 11 ; sur Windows 10 la fenêtre garde le fond du thème.
        if (MicaController.IsSupported())
        {
            SystemBackdrop = new MicaBackdrop();
        }
        else
        {
            // Sans Mica, le contenu prend la couleur de fond de la palette (sinon il serait noir en mode sombre).
            Racine.Background = (Brush)Application.Current.Resources["VitalisFond"];
        }

        _pages = new MainViewModel().Pages;
        ConstruireMenu(_pages);
        Racine.KeyDown += Racine_KeyDown;
        Naviguer(_pages[0]);
    }

    private void ConstruireMenu(IReadOnlyList<PageDefinition> pages)
    {
        for (var i = 0; i < pages.Count; i++)
        {
            var page = pages[i];
            var bouton = new Button
            {
                Content = page.Titre,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
            };

            bouton.Click += (_, _) => Naviguer(page);

            _boutons[page.Cle] = bouton;
            MenuPanneau.Children.Add(bouton);
        }
    }

    // Ctrl+1 à Ctrl+9 : pages 1 à 9 ; Ctrl+0 : page 10.
    // Géré ici plutôt que par KeyboardAccelerator, qui ne se déclenchait pas dans cette configuration.
    private void Racine_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        if (!ctrl.HasFlag(CoreVirtualKeyStates.Down))
        {
            return;
        }

        var index = e.Key switch
        {
            >= VirtualKey.Number1 and <= VirtualKey.Number9 => e.Key - VirtualKey.Number1,
            VirtualKey.Number0 => 9,
            _ => -1,
        };

        if (index < 0 || index >= _pages.Count)
        {
            return;
        }

        Naviguer(_pages[index]);
        e.Handled = true;
    }

    private void Naviguer(PageDefinition page)
    {
        var type = page.Cle switch
        {
            "disques" => typeof(Views.DisquesPage),
            "mises-a-jour" => typeof(Views.MisesAJourPage),
            "tableau-de-bord" => typeof(Views.TableauDeBordPage),
            "pilotes" => typeof(Views.PilotesPage),
            "demarrage" => typeof(Views.DemarragePage),
            "performances" => typeof(Views.PerformancesPage),
            _ => typeof(PagePlaceholder),
        };
        ContenuFrame.Navigate(type, page);

        // Page active en gras, pour que l'état soit visible sans couleur seule.
        foreach (var (cle, bouton) in _boutons)
        {
            bouton.FontWeight = cle == page.Cle ? FontWeights.Bold : FontWeights.Normal;
        }
    }
}
