using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Vitalis.Core.Disques;
using Vitalis.Core.Materiel;
using Vitalis.Core.MisesAJour;
using Vitalis.Core.TableauDeBord;

namespace Vitalis.Core.Rapport;

/// <summary>
/// Rapport HTML autonome. Ce qui est exclu, par conception : nom d'utilisateur, nom de la machine,
/// adresse MAC (jamais lue), numéros de série (jamais lus), noms de volumes (choisis par l'utilisateur),
/// identifiants internes (ObjectId). Un filet de sécurité remplace tout nom d'utilisateur ou de machine résiduel.
/// </summary>
public sealed class RapportHtml
{
    private static readonly Regex MacRegex = new(@"\b([0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}\b", RegexOptions.Compiled);

    private readonly string _utilisateur;
    private readonly string _machine;

    /// <param name="utilisateur">Nom d'utilisateur Windows, masqué s'il apparaît dans un texte.</param>
    /// <param name="machine">Nom de la machine, masqué s'il apparaît dans un texte.</param>
    public RapportHtml(string utilisateur, string machine)
    {
        _utilisateur = utilisateur;
        _machine = machine;
    }

    /// <summary>Texte nettoyé : nom d'utilisateur, nom de machine et adresses MAC remplacés.</summary>
    public string Masquer(string? texte)
    {
        if (string.IsNullOrEmpty(texte))
        {
            return string.Empty;
        }

        var propre = texte;
        foreach (var secret in new[] { _utilisateur, _machine }.Where(s => s.Length >= 3))
        {
            propre = Regex.Replace(propre, Regex.Escape(secret), "[masqué]", RegexOptions.IgnoreCase);
        }

        return MacRegex.Replace(propre, "[adresse masquée]");
    }

    public string Generer(ResultatTableauDeBord r)
    {
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html lang=\"fr\"><head><meta charset=\"utf-8\"><title>Rapport Vitalis</title>");
        sb.Append("<style>body{font-family:'Segoe UI',sans-serif;margin:32px;color:#1a1a1a;max-width:960px}")
          .Append("h1{font-size:26px}h2{font-size:18px;margin-top:28px;border-bottom:1px solid #ccc;padding-bottom:4px}")
          .Append("table{border-collapse:collapse;width:100%}td,th{border:1px solid #ddd;padding:6px 8px;text-align:left;font-size:14px}")
          .Append(".gris{color:#555}.score{font-size:40px;font-weight:600}.note{font-size:13px;color:#555}</style></head><body>");

        sb.Append("<h1>Rapport Vitalis</h1>");
        sb.Append($"<p class=\"gris\">Généré le {Enc(r.Date.ToString("g", CultureInfo.CurrentCulture))}</p>");

        sb.Append("<h2>Score global</h2>");
        sb.Append(r.Score is null
            ? "<p class=\"score\">indisponible</p>"
            : $"<p class=\"score\">{r.Score} / 100</p>");
        sb.Append("<table><tr><th>Composant</th><th>Poids</th><th>Valeur</th></tr>");
        foreach (var c in r.Composants)
        {
            sb.Append($"<tr><td>{Enc(c.Nom)}</td><td>{c.Poids} %</td><td>{(c.Valeur is null ? "indisponible" : c.Valeur + " / 100")}</td></tr>");
        }

        sb.Append("</table>");
        sb.Append("<p class=\"note\">Le score est une synthèse indicative. Formule : ADR-008 de la documentation Vitalis.</p>");

        AjouterMateriel(sb, r.Materiel);
        AjouterDisques(sb, r.Disques);
        AjouterMisesAJour(sb, r.MisesAJour);

        sb.Append("<h2>Données exclues</h2><p class=\"note\">Ce rapport ne contient pas : le nom d'utilisateur, le nom de la machine, ")
          .Append("l'adresse MAC, les numéros de série, les noms de volumes choisis par l'utilisateur ni les identifiants internes.</p>");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    private void AjouterMateriel(StringBuilder sb, RapportMateriel? m)
    {
        sb.Append("<h2>Matériel</h2>");
        if (m is null)
        {
            sb.Append("<p class=\"gris\">Analyse matériel non disponible.</p>");
            return;
        }

        sb.Append("<table>");
        foreach (var cpu in m.Processeur.Elements)
        {
            sb.Append($"<tr><th>Processeur</th><td>{Enc(Masquer(cpu.Nom))} · {cpu.Coeurs} cœurs · {cpu.ThreadsLogiques} threads · {cpu.FrequenceMaxMHz} MHz</td></tr>");
        }

        var memoire = Conversions.MemoireTotaleOctets(m.Memoire.Elements);
        sb.Append($"<tr><th>Mémoire</th><td>{(memoire is null ? "indisponible" : Gio(memoire.Value) + " Gio")}</td></tr>");
        foreach (var carte in m.CartesGraphiques.Elements)
        {
            sb.Append($"<tr><th>Carte graphique</th><td>{Enc(Masquer(carte.Nom))} · pilote {Enc(carte.VersionPilote ?? "n/d")}</td></tr>");
        }

        foreach (var e in m.Ecrans.Elements)
        {
            sb.Append($"<tr><th>Écran</th><td>{Enc(Masquer(e.Nom))}</td></tr>");
        }

        foreach (var a in m.Reseau.Elements)
        {
            sb.Append($"<tr><th>Réseau</th><td>{Enc(Masquer(a.Nom))}</td></tr>");
        }

        foreach (var a in m.Audio.Elements)
        {
            sb.Append($"<tr><th>Audio</th><td>{Enc(Masquer(a.Nom))} ({Enc(a.Statut ?? "statut inconnu")})</td></tr>");
        }

        sb.Append("</table>");
    }

    private void AjouterDisques(StringBuilder sb, RapportDisques? d)
    {
        sb.Append("<h2>Disques et espace libre</h2>");
        if (d is null)
        {
            sb.Append("<p class=\"gris\">Analyse des disques non disponible.</p>");
            return;
        }

        sb.Append("<table><tr><th>Disque</th><th>Type</th><th>Taille</th><th>Santé</th><th>Température</th><th>Usure</th></tr>");
        foreach (var disque in d.Disques.Elements)
        {
            sb.Append($"<tr><td>{Enc(Masquer(disque.Nom))}</td><td>{disque.Type}</td><td>{Gio(disque.TailleOctets)}</td><td>{disque.Sante}</td>" +
                      $"<td>{(disque.TemperatureC is null ? "non lu" : disque.TemperatureC + " °C")}</td>" +
                      $"<td>{(disque.UsurePourcent is null ? "non lu" : disque.UsurePourcent + " %")}</td></tr>");
        }

        sb.Append("</table>");
        if (!d.SmartComplet && d.RaisonSmart is not null)
        {
            sb.Append($"<p class=\"note\">{Enc(Masquer(d.RaisonSmart))}</p>");
        }

        sb.Append("<table><tr><th>Lecteur</th><th>Taille</th><th>Libre</th><th>Pourcentage</th><th>Statut</th></tr>");
        foreach (var l in d.Lecteurs.Elements)
        {
            sb.Append($"<tr><td>{Enc(l.Lettre)}</td><td>{Gio(l.TailleOctets)}</td><td>{Gio(l.EspaceLibreOctets)}</td>" +
                      $"<td>{(l.PourcentLibre is null ? "n/d" : l.PourcentLibre.Value.ToString("0", CultureInfo.InvariantCulture) + " %")}</td><td>{l.Statut}</td></tr>");
        }

        sb.Append("</table>");
    }

    private void AjouterMisesAJour(StringBuilder sb, RapportMisesAJour? m)
    {
        sb.Append("<h2>Mises à jour</h2>");
        if (m is null)
        {
            sb.Append("<p class=\"gris\">Analyse des mises à jour non disponible.</p>");
            return;
        }

        var date = m.DerniereInstallationReussie is { } dt ? dt.ToString("d MMMM yyyy", CultureInfo.CurrentCulture) : "inconnue";
        sb.Append($"<p>Dernière installation Windows réussie : {Enc(date)}</p>");

        sb.Append("<h3>Windows Update</h3>");
        if (m.EnAttente.Elements.Count == 0)
        {
            sb.Append($"<p class=\"gris\">{Enc(m.EnAttente.Raison ?? "Aucune mise à jour en attente.")}</p>");
        }
        else
        {
            sb.Append("<ul>");
            foreach (var u in m.EnAttente.Elements)
            {
                var kb = u.Kb.Count == 0 ? string.Empty : " (" + string.Join(", ", u.Kb) + ")";
                sb.Append($"<li>{(u.Pilote ? "Pilote" : "Logiciel")} : {Enc(Masquer(u.Titre))}{Enc(kb)}</li>");
            }

            sb.Append("</ul>");
        }

        sb.Append("<h3>Logiciels (winget)</h3>");
        if (m.Logiciels.Elements.Count == 0)
        {
            sb.Append($"<p class=\"gris\">{Enc(m.Logiciels.Raison ?? "Aucune mise à jour de logiciel.")}</p>");
        }
        else
        {
            sb.Append("<table><tr><th>Logiciel</th><th>Actuelle</th><th>Disponible</th></tr>");
            foreach (var l in m.Logiciels.Elements)
            {
                sb.Append($"<tr><td>{Enc(Masquer(l.Nom))}</td><td>{Enc(l.VersionActuelle)}</td><td>{Enc(l.VersionDisponible)}</td></tr>");
            }

            sb.Append("</table>");
        }
    }

    private static string Gio(long? octets) => octets is null ? "n/d" : Gio(octets.Value) + " Gio";

    private static string Gio(long octets) => (octets / 1024.0 / 1024 / 1024).ToString("0.0", CultureInfo.InvariantCulture);

    private static string Enc(string? texte) => WebUtility.HtmlEncode(texte ?? string.Empty);
}
