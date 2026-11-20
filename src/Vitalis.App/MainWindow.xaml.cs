using System.Collections.Generic;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Vitalis.App.ViewModels;
using Vitalis.App.Views;
using Vitalis.Core;
using Vitalis.Core.Navigation;
using Windows.System;

namespace Vitalis.App;

public sealed partial class MainWindow : Window
{
    private readonly Dictionary<string, Button> _boutons = new();

    public MainWindow()
    {
        InitializeComponent();
        Title = AppInfo.Nom;

        // Mica seulement sur Windows 11 ; sur Windows 10 la fenêtre garde le fond du thème.
        if (MicaController.IsSupported())
        {
            SystemBackdrop = new MicaBackdrop();
        }

        var pages = new MainViewModel().Pages;
        ConstruireMenu(pages);
        Naviguer(pages[0]);
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

            // Ctrl+1 à Ctrl+9 pour les neuf premières pages, Ctrl+0 pour la dixième.
            bouton.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = i < 9 ? VirtualKey.Number1 + i : VirtualKey.Number0,
                Modifiers = VirtualKeyModifiers.Control,
            });

            bouton.Click += (_, _) => Naviguer(page);

            _boutons[page.Cle] = bouton;
            MenuPanneau.Children.Add(bouton);
        }
    }

    private void Naviguer(PageDefinition page)
    {
        ContenuFrame.Navigate(typeof(PagePlaceholder), page);

        // Page active en gras, pour que l'état soit visible sans couleur seule.
        foreach (var (cle, bouton) in _boutons)
        {
            bouton.FontWeight = cle == page.Cle ? FontWeights.Bold : FontWeights.Normal;
        }
    }
}
