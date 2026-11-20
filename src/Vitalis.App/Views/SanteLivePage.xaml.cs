using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Vitalis.Core.Journal;
using Vitalis.Core.MisesAJour;
using Vitalis.Core.SanteLive;

namespace Vitalis.App.Views;

/// <summary>
/// Page « Santé live » : capteurs lus toutes les deux secondes hors du thread d'interface (LibreHardwareMonitor).
/// L'interface est construite une fois, puis seuls les textes changent. Les capteurs sont fermés en quittant la page.
/// </summary>
public sealed partial class SanteLivePage : Page
{
    private const string IdPawnIo = "namazso.PawnIO";
    private static readonly TimeSpan Intervalle = TimeSpan.FromSeconds(2);

    private readonly Dictionary<string, TextBlock> _valeurs = new();
    private readonly Dictionary<string, ContentControl> _badges = new();
    private CapteursLive? _capteurs;
    private DispatcherTimer? _minuteur;
    private string? _structure;
    private bool _lectureEnCours;
    private bool _pause;
    private bool _alerteAffichee;

    public SanteLivePage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Corps.Children.Add(Ui.Chargement("Ouverture des capteurs… (quelques secondes)"));
        _capteurs = new CapteursLive();
        _minuteur = new DispatcherTimer { Interval = Intervalle };
        _minuteur.Tick += async (_, _) => await LireAsync();
        _minuteur.Start();
        _ = LireAsync();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _minuteur?.Stop();
        var capteurs = _capteurs;
        _capteurs = null;
        // Fermeture hors du thread d'interface : elle peut attendre la fin d'une lecture.
        _ = Task.Run(() => capteurs?.Dispose());
    }

    private void BoutonPause_Click(object sender, RoutedEventArgs e)
    {
        _pause = !_pause;
        TextePause.Text = _pause ? "Reprendre" : "Pause";
        IconePause.Glyph = _pause ? "" : "";
    }

    private async Task LireAsync()
    {
        if (_pause || _lectureEnCours || _capteurs is not { } capteurs)
        {
            return;
        }

        _lectureEnCours = true;
        try
        {
            var composants = await Task.Run(() =>
            {
                lock (capteurs)
                {
                    return Demo.Actif ? Demo.Capteurs() : capteurs.Lire();
                }
            });

            if (_capteurs is null)
            {
                return;
            }

            var structure = string.Join("|", composants.Select(c => c.Nom + ":" + c.Mesures.Count));
            if (structure != _structure)
            {
                _structure = structure;
                Construire(composants);
            }

            MettreAJour(composants);
        }
        catch (Exception ex)
        {
            _minuteur?.Stop();
            JournalVitalis.Erreur("Lecture des capteurs impossible", ex);
            Corps.Children.Clear();
            Corps.Children.Add(Ui.Message(InfoBarSeverity.Error, "Capteurs illisibles", $"{ex.GetType().Name} : {ex.Message}"));
        }
        finally
        {
            _lectureEnCours = false;
        }
    }

    private static string Cle(ComposantLive c, Mesure m) => $"{c.Nom}/{m.Type}/{m.Capteur}";

    private static string NomComposant(ComposantLive c) => c.Nom switch
    {
        "Total Memory" => "Mémoire vive",
        "Virtual Memory" => "Mémoire virtuelle (fichier d'échange)",
        _ => c.Nom,
    };

    private static string Glyphe(Categorie c) => c switch
    {
        Categorie.Processeur => "",
        Categorie.CarteGraphique => "",
        Categorie.Memoire => "",
        Categorie.CarteMere => "",
        Categorie.Stockage => "",
        _ => "",
    };

    private void Construire(IReadOnlyList<ComposantLive> composants)
    {
        _valeurs.Clear();
        _badges.Clear();
        Corps.Children.Clear();
        AfficherAlertePawnIo(composants);

        if (composants.Count == 0)
        {
            Corps.Children.Add(Ui.Vide("", "Aucun capteur", "Windows ne donne accès à aucun capteur sur ce PC (machine virtuelle, ou matériel non pris en charge)."));
            return;
        }

        // Synthèse : une carte par composant (température principale, charge principale).
        var synthese = new GrilleAdaptative { LargeurMinimale = 240 };
        foreach (var c in composants.OrderBy(c => c.Categorie))
        {
            var p = new StackPanel { Spacing = 8 };
            var badge = new ContentControl();
            _badges[c.Nom] = badge;
            p.Children.Add(Ui.EnTeteCarte(Glyphe(c.Categorie), NomComposant(c), badge));
            var temp = new TextBlock { Style = Ui.Style("SubtitleTextBlockStyle"), Foreground = Ui.Pinceau("VitalisTexte") };
            var charge = Ui.Texte(string.Empty, secondaire: true);
            _valeurs[c.Nom + "/temp"] = temp;
            _valeurs[c.Nom + "/charge"] = charge;
            p.Children.Add(temp);
            p.Children.Add(charge);
            synthese.Children.Add(Ui.Carte(p));
        }

        Corps.Children.Add(synthese);

        // Détail : tous les capteurs, groupés par type.
        foreach (var c in composants.OrderBy(c => c.Categorie))
        {
            var detail = new StackPanel { Spacing = 14 };
            foreach (var groupe in c.Mesures.GroupBy(m => m.Type).OrderBy(g => g.Key))
            {
                detail.Children.Add(Ui.Texte(NomType(groupe.Key), gras: true));
                var grille = new GrilleAdaptative { LargeurMinimale = 220, Espacement = 6 };
                foreach (var m in groupe)
                {
                    var ligne = new Grid { ColumnSpacing = 8 };
                    ligne.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    ligne.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    var nom = Ui.Texte(m.Capteur, secondaire: true);
                    nom.TextTrimming = TextTrimming.CharacterEllipsis;
                    nom.TextWrapping = TextWrapping.NoWrap;
                    var valeur = Ui.Texte(string.Empty);
                    Grid.SetColumn(valeur, 1);
                    ligne.Children.Add(nom);
                    ligne.Children.Add(valeur);
                    _valeurs[Cle(c, m)] = valeur;
                    grille.Children.Add(ligne);
                }

                detail.Children.Add(grille);
            }

            Corps.Children.Add(new Expander
            {
                Header = $"{NomComposant(c)} · {Ui.Pluriel(c.Mesures.Count, "capteur")}",
                Content = detail,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            });
        }

        Corps.Children.Add(Ui.Legende("Seuils d'alerte : processeur 85 / 95 °C, carte graphique 80 / 90 °C, stockage 55 / 65 °C. Source : LibreHardwareMonitor (MPL-2.0). « n/d » : capteur présent mais non lu."));
    }

    private void MettreAJour(IReadOnlyList<ComposantLive> composants)
    {
        foreach (var c in composants)
        {
            var temp = AnalyseCapteurs.TemperaturePrincipale(c);
            var charge = AnalyseCapteurs.ChargePrincipale(c);
            if (_valeurs.TryGetValue(c.Nom + "/temp", out var t))
            {
                t.Text = temp is null ? (charge is null ? "—" : Format(TypeMesure.Charge, charge)) : Format(TypeMesure.Temperature, temp);
            }

            if (_valeurs.TryGetValue(c.Nom + "/charge", out var ch))
            {
                ch.Text = temp is null
                    ? (c.Categorie == Categorie.Processeur ? "Température non disponible" : "Utilisation")
                    : charge is null ? "Température" : $"Température · utilisation {Format(TypeMesure.Charge, charge)}";
            }

            if (_badges.TryGetValue(c.Nom, out var b))
            {
                b.Content = temp is null ? null : AnalyseCapteurs.Niveau(c.Categorie, temp) switch
                {
                    NiveauMesure.Critique => Ui.Badge(Niveau.Critique, "Trop chaud"),
                    NiveauMesure.Attention => Ui.Badge(Niveau.Attention, "Chaud"),
                    _ => Ui.Badge(Niveau.Succes, "Normal"),
                };
            }

            foreach (var m in c.Mesures)
            {
                if (_valeurs.TryGetValue(Cle(c, m), out var v))
                {
                    v.Text = Format(m.Type, m.Valeur);
                }
            }
        }
    }

    private static string NomType(TypeMesure t) => t switch
    {
        TypeMesure.Temperature => "Températures",
        TypeMesure.Charge => "Utilisation",
        TypeMesure.Frequence => "Fréquences",
        TypeMesure.Ventilateur => "Ventilateurs",
        TypeMesure.Puissance => "Consommation",
        _ => t.ToString(),
    };

    private static string Format(TypeMesure t, float? v) => v is not { } x ? "n/d" : t switch
    {
        TypeMesure.Temperature => x.ToString("0", CultureInfo.CurrentCulture) + " °C",
        TypeMesure.Charge => x.ToString("0", CultureInfo.CurrentCulture) + " %",
        TypeMesure.Frequence => x >= 1000 ? (x / 1000).ToString("0.00", CultureInfo.CurrentCulture) + " GHz" : x.ToString("0", CultureInfo.CurrentCulture) + " MHz",
        TypeMesure.Ventilateur => x.ToString("0", CultureInfo.CurrentCulture) + " tr/min",
        TypeMesure.Puissance => x.ToString("0.0", CultureInfo.CurrentCulture) + " W",
        _ => x.ToString("0.#", CultureInfo.CurrentCulture),
    };

    /// <summary>Températures du processeur absentes : le pilote PawnIO manque ou est bloqué. Proposé, jamais installé d'office.</summary>
    private void AfficherAlertePawnIo(IReadOnlyList<ComposantLive> composants)
    {
        if (_alerteAffichee || !AnalyseCapteurs.TemperaturesProcesseurManquantes(composants) || !composants.Any(c => c.Categorie == Categorie.Processeur))
        {
            return;
        }

        _alerteAffichee = true;
        var installer = Ui.Bouton("Installer PawnIO");
        var message = Ui.Message(InfoBarSeverity.Warning, "Températures du processeur indisponibles",
            "Elles passent par PawnIO, un petit pilote open source et signé (pawnio.eu), installé à part. Sans lui, le reste fonctionne. " +
            "Certains anti-triches (ex. Vanguard) ou la protection « Intégrité de la mémoire » peuvent le bloquer.", installer);
        installer.Click += async (_, _) =>
        {
            var dialogue = new ContentDialog
            {
                Title = "Installer PawnIO ?",
                Content = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = "PawnIO (namazso.PawnIO) sera installé depuis la source officielle winget. C'est un pilote noyau signé qui donne accès aux capteurs du processeur. " +
                           "Vous pourrez le désinstaller depuis Paramètres › Applications. Rouvrez ensuite cette page.",
                },
                PrimaryButtonText = "Installer",
                CloseButtonText = "Annuler",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot,
            };
            if (await dialogue.ShowAsync() != ContentDialogResult.Primary)
            {
                return;
            }

            installer.IsEnabled = false;
            message.Message = "Installation de PawnIO…";
            var r = await InstallateurWinget.InstallerAsync(IdPawnIo);
            JournalVitalis.Information($"PawnIO : {(r.Reussi ? "installé" : "échec")}.");
            message.Severity = r.Reussi ? InfoBarSeverity.Success : InfoBarSeverity.Error;
            message.Title = r.Reussi ? "PawnIO installé" : "Installation de PawnIO impossible";
            message.Message = r.Reussi ? "Quittez puis rouvrez cette page pour lire les températures du processeur." : r.Message;
            installer.IsEnabled = !r.Reussi;
        };
        Messages.Children.Add(message);
    }
}
