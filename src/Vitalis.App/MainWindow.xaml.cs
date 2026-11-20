using System.Runtime.InteropServices;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Vitalis.App.ViewModels;
using Vitalis.App.Views;
using Vitalis.Core;
using Vitalis.Core.Navigation;
using Windows.System;
using Windows.UI.Core;

namespace Vitalis.App;

public sealed partial class MainWindow : Window
{
    /// <summary>Icônes du menu (police Segoe Fluent Icons sur Windows 11, Segoe MDL2 Assets sur Windows 10).</summary>
    private static readonly Dictionary<string, string> Icones = new()
    {
        ["tableau-de-bord"] = "\uE80F",
        ["materiel"] = "\uE950",
        ["disques"] = "\uEDA2",
        ["mises-a-jour"] = "\uE895",
        ["pilotes"] = "\uE772",
        ["demarrage"] = "\uE7E8",
        ["performances"] = "\uEC4A",
        ["sante-live"] = "\uE95E",
        ["rapport"] = "\uE9F9",
        ["parametres"] = "\uE713",
    };

    private readonly Dictionary<string, ListViewItem> _items = new();
    private IReadOnlyList<PageDefinition> _pages = [];
    private bool _synchronisation;
    private string? _pageActive;

    public MainWindow()
    {
        InitializeComponent();
        Title = AppInfo.Nom;
        TitreFenetre.Text = $"{AppInfo.Nom}  ·  diagnostic du PC";

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(BarreTitre);

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

        DimensionnerFenetre();
        AppliquerIcone();
        AppliquerTheme(App.Theme);

        _pages = new MainViewModel().Pages;
        ConstruireMenu(_pages);
        Racine.KeyDown += Racine_KeyDown;
        Naviguer(_pages[0].Cle);
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string fichier, int index);

    /// <summary>Icône de la fenêtre et de la barre des tâches : celle intégrée à Vitalis.exe.</summary>
    private void AppliquerIcone()
    {
        var icone = Environment.ProcessPath is { } exe ? ExtractIcon(IntPtr.Zero, exe, 0) : IntPtr.Zero;
        if (icone > 1)
        {
            AppWindow.SetIcon(Microsoft.UI.Win32Interop.GetIconIdFromIcon(icone));
        }
    }

    /// <summary>Taille de départ 1280×840 (à l'échelle de l'écran) et taille minimale lisible.</summary>
    private void DimensionnerFenetre()
    {
        var echelle = GetDpiForWindow(WinRT.Interop.WindowNative.GetWindowHandle(this)) / 96.0;
        if (echelle <= 0)
        {
            echelle = 1;
        }

        var zone = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var largeur = Math.Min((int)(1280 * echelle), zone.Width);
        var hauteur = Math.Min((int)(840 * echelle), zone.Height);
        AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(
            zone.X + (zone.Width - largeur) / 2, zone.Y + (zone.Height - hauteur) / 2, largeur, hauteur));

        if (AppWindow.Presenter is OverlappedPresenter p)
        {
            p.PreferredMinimumWidth = (int)(900 * echelle);
            p.PreferredMinimumHeight = (int)(600 * echelle);
        }
    }

    /// <summary>Thème du contenu : « Clair », « Sombre », ou « Systeme » (suit Windows). La barre de titre suit aussi.</summary>
    public void AppliquerTheme(string theme)
    {
        Racine.RequestedTheme = theme switch
        {
            "Clair" => ElementTheme.Light,
            "Sombre" => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };

        AppWindow.TitleBar.PreferredTheme = theme switch
        {
            "Clair" => TitleBarTheme.Light,
            "Sombre" => TitleBarTheme.Dark,
            _ => TitleBarTheme.UseDefaultAppMode,
        };
    }

    private void ConstruireMenu(IReadOnlyList<PageDefinition> pages)
    {
        for (var i = 0; i < pages.Count; i++)
        {
            var page = pages[i];
            var contenu = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, Padding = new Thickness(4, 6, 4, 6) };
            contenu.Children.Add(new FontIcon { Glyph = Icones.GetValueOrDefault(page.Cle, "\uE8A5"), FontSize = 16 });
            contenu.Children.Add(new TextBlock { Text = page.Titre, VerticalAlignment = VerticalAlignment.Center });

            var raccourci = i < 9 ? $"Ctrl+{i + 1}" : "Ctrl+0";
            var item = new ListViewItem { Content = contenu, Tag = page.Cle };
            ToolTipService.SetToolTip(item, $"{page.Titre} ({raccourci})");
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(item, page.Titre);
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAcceleratorKey(item, raccourci);

            _items[page.Cle] = item;
            (page.Cle == "parametres" ? MenuBas : Menu).Items.Add(item);
        }
    }

    private void Menu_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_synchronisation || sender is not ListView liste || liste.SelectedItem is not ListViewItem { Tag: string cle })
        {
            return;
        }

        Naviguer(cle);
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

        Naviguer(_pages[index].Cle);
        e.Handled = true;
    }

    /// <summary>Ouvre une page par sa clé (utilisé aussi par les cartes du tableau de bord).</summary>
    public void Naviguer(string cle)
    {
        var page = _pages.FirstOrDefault(p => p.Cle == cle);
        if (page is null || cle == _pageActive)
        {
            return;
        }

        var type = page.Cle switch
        {
            "tableau-de-bord" => typeof(TableauDeBordPage),
            "materiel" => typeof(MaterielPage),
            "disques" => typeof(DisquesPage),
            "mises-a-jour" => typeof(MisesAJourPage),
            "pilotes" => typeof(PilotesPage),
            "demarrage" => typeof(DemarragePage),
            "performances" => typeof(PerformancesPage),
            "rapport" => typeof(RapportPage),
            "parametres" => typeof(ParametresPage),
            _ => typeof(PagePlaceholder),
        };

        try
        {
            ContenuFrame.Navigate(type, page, new EntranceNavigationTransitionInfo());
            _pageActive = cle;
        }
        catch (Exception ex)
        {
            // Une page en erreur ne doit pas bloquer le menu : l'erreur est journalisée, la page actuelle reste affichée.
            Vitalis.Core.Journal.JournalVitalis.Erreur($"Ouverture de la page {cle} impossible", ex);
            cle = _pageActive ?? _pages[0].Cle;
        }

        // Sélection du menu synchronisée (les deux listes : une seule page active).
        _synchronisation = true;
        Menu.SelectedItem = cle == "parametres" ? null : _items[cle];
        MenuBas.SelectedItem = cle == "parametres" ? _items[cle] : null;
        _synchronisation = false;
    }
}
