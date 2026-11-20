using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Vitalis.Core.Materiel;
using Vitalis.Core.MisesAJour;

namespace Vitalis.App.Views;

/// <summary>Page « Mises à jour » : mises à jour Windows en attente et dernière installation. Lecture seule.</summary>
public sealed partial class MisesAJourPage : Page
{
    private CancellationTokenSource? _annulation;

    public MisesAJourPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await ChargerAsync();
        Unloaded += (_, _) => _annulation?.Cancel();
    }

    private async Task ChargerAsync()
    {
        _annulation?.Cancel();
        _annulation = new CancellationTokenSource();

        TexteResume.Text = "Recherche en cours (cela peut prendre une trentaine de secondes)…";
        ListeMises.Children.Clear();

        try
        {
            var rapport = await new AnalyseMisesAJour(new WuaCom(), new WingetCli()).AnalyserAsync(_annulation.Token);
            Afficher(rapport);
        }
        catch (OperationCanceledException)
        {
            // Page quittée pendant la recherche.
        }
        catch (Exception e)
        {
            TexteResume.Text = $"Analyse impossible : {e.Message}";
        }
    }

    private void Afficher(RapportMisesAJour rapport)
    {
        var nombre = rapport.EnAttente.Elements.Count;
        var date = rapport.DerniereInstallationReussie is { } d
            ? d.ToString("d MMMM yyyy", CultureInfo.CurrentCulture)
            : "inconnue";

        TexteResume.Text = $"{nombre} mise(s) à jour Windows en attente · {rapport.Logiciels.Elements.Count} logiciel(s) à mettre à jour · " +
            $"dernière installation Windows réussie : {date}."
            + (rapport.RaisonHistorique is null ? string.Empty : $" {rapport.RaisonHistorique}");

        ListeLogiciels.Children.Clear();
        if (rapport.Logiciels.Etat == Disponibilite.Indisponible)
        {
            ListeLogiciels.Children.Add(Texte(rapport.Logiciels.Raison ?? "winget indisponible.", secondaire: true));
        }
        else
        {
            if (rapport.Logiciels.Elements.Count == 0)
            {
                ListeLogiciels.Children.Add(Texte(rapport.Logiciels.Raison ?? "Aucune mise à jour de logiciel.", secondaire: true));
            }

            foreach (var l in rapport.Logiciels.Elements)
            {
                ListeLogiciels.Children.Add(Texte($"{l.Nom} · {l.Id} · {l.VersionActuelle} → {l.VersionDisponible}", secondaire: false));
            }
        }

        if (rapport.EnAttente.Etat == Disponibilite.Indisponible)
        {
            ListeMises.Children.Add(Texte(rapport.EnAttente.Raison ?? "Recherche indisponible.", secondaire: true));
            return;
        }

        foreach (var m in rapport.EnAttente.Elements)
        {
            var kb = m.Kb.Count == 0 ? string.Empty : $" ({string.Join(", ", m.Kb)})";
            var gravite = m.Gravite is null ? string.Empty : $" · gravité : {m.Gravite}";
            var type = m.Pilote ? "Pilote" : "Logiciel";
            var obligatoire = m.Obligatoire ? " · obligatoire" : string.Empty;
            ListeMises.Children.Add(Texte($"{type} · {m.Titre}{kb}{gravite}{obligatoire}", secondaire: false));
        }
    }

    private static TextBlock Texte(string texte, bool secondaire) => new()
    {
        Text = texte,
        TextWrapping = TextWrapping.Wrap,
        Foreground = (Brush)Application.Current.Resources[secondaire ? "VitalisTexteSecondaire" : "VitalisTexte"],
    };
}
