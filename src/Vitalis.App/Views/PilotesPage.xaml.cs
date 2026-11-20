using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Vitalis.Core.Materiel;
using Vitalis.Core.Pilotes;

namespace Vitalis.App.Views;

/// <summary>Page « Pilotes » : liste, règle des deux ans, liens officiels vérifiés. Lecture seule.</summary>
public sealed partial class PilotesPage : Page
{
    public PilotesPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Charger();
    }

    private void Charger()
    {
        try
        {
            var rapport = new AnalysePilotes(new WmiSource()).Analyser(DateTime.Now);
            Afficher(rapport);
        }
        catch (Exception e)
        {
            TexteResume.Text = $"Analyse impossible : {e.Message}";
        }
    }

    private void Afficher(RapportPilotes rapport)
    {
        ListePilotes.Children.Clear();
        if (rapport.Pilotes.Etat == Disponibilite.Indisponible)
        {
            TexteResume.Text = rapport.Pilotes.Raison ?? "Pilotes indisponibles.";
            return;
        }

        TexteResume.Text = $"{rapport.Pilotes.Elements.Count} pilote(s) · {rapport.NombreAnciens} ancien(s) (plus de deux ans) · liens vérifiés le {LiensFabricants.DateVerification}.";

        // Anciens d'abord : ce sont ceux qui demandent une action. Limité à 300 lignes pour rester fluide.
        foreach (var p in rapport.Pilotes.Elements
            .OrderByDescending(p => p.Ancien)
            .ThenBy(p => p.Appareil, StringComparer.CurrentCultureIgnoreCase)
            .Take(300))
        {
            var date = p.Date is null ? "date inconnue" : p.Date.Value.ToString("d MMMM yyyy", CultureInfo.CurrentCulture);
            ListePilotes.Children.Add(Texte($"{(p.Ancien ? "⚠ Ancien" : "✔ Récent")} · {p.Appareil} · version {p.Version ?? "n/d"} · {date}", secondaire: false));

            if (p.Lien is { EstVerifie: true } lien)
            {
                ListePilotes.Children.Add(new HyperlinkButton
                {
                    Content = $"Page officielle {lien.Nom} (vérifiée le {lien.VerifieLe})",
                    NavigateUri = new Uri(lien.Url!),
                });
            }
            else if (p.Lien is not null)
            {
                ListePilotes.Children.Add(Texte($"Lien {p.Lien.Nom} : {p.Lien.Statut}. Recherchez le pilote sur le site du fabricant.", secondaire: true));
            }
        }
    }

    private static TextBlock Texte(string texte, bool secondaire) => new()
    {
        Text = texte,
        TextWrapping = TextWrapping.Wrap,
        Foreground = (Brush)Application.Current.Resources[secondaire ? "VitalisTexteSecondaire" : "VitalisTexte"],
    };
}
