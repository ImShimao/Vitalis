---
id: F-pilotes
type: fonction
priorite: P1
statut: fait
etape: 7
tags: [fonction, pilotes, liens-officiels]
cree: 2026-11-20
---

# Pilotes

## Couverture

Liste les pilotes installés (appareil, fabricant, version, date, classe), signale ceux de plus de deux ans, et propose le lien officiel du fabricant **quand il a été vérifié**. Lecture seule : rien n'est téléchargé ni installé. Code : `src/Vitalis.Core/Pilotes/`, page `src/Vitalis.App/Views/PilotesPage.xaml`. Tâche : [[T-008-pilotes]].

## Source

- `Win32_PnPSignedDriver` (root\CIMV2) : DeviceName, Manufacturer, DriverVersion, DriverDate, DeviceClass. Doc Microsoft consultée le 2026-11-20.
- `DriverDate` accepte le format CIM (AAAAMMJJ…) et le format « AAAA-MM-JJ HH:MM:SS ».

## Règle des deux ans

Un pilote est « ancien » si sa date est antérieure à aujourd'hui moins deux ans. Sans date : non signalé. Cette règle ne prouve pas qu'une mise à jour existe : elle indique seulement qu'il faut vérifier.

## Table des liens fabricants (contrôle du 2026-11-20)

| Fabricant | Adresse | Résultat du contrôle | Affiché |
|---|---|---|---|
| NVIDIA | nvidia.com/Download/index.aspx | HTTP 200 | oui |
| Intel | intel.com/.../download-center/home.html | HTTP 200 | oui |
| Realtek | realtek.com/downloads | HTTP 200 | oui |
| ASUS | asus.com/support/download-center/ | HTTP 202 | oui, « à confirmer à l'ouverture » |
| AMD | amd.com/en/support/download/drivers.html | pas de réponse | non |
| MSI, Gigabyte, Dell, HP, Lenovo | sites officiels | HTTP 403 (blocage automatique) | non |
| Acer | acer.com/us-en/support | pas de réponse | non |

Un fabricant non vérifié n'a pas de lien : l'application écrit « Recherchez le pilote sur le site du fabricant ». Aucune adresse n'a été inventée.

## Cas « indisponible »

- Classe absente ou erreur WMI : section indisponible avec raison.
- Fabricant inconnu : pas de lien.

## Limites connues

- **Pilotes génériques Microsoft** : beaucoup sont datés de 2006 et sont signalés « anciens » alors qu'ils sont mis à jour par Windows Update. Sur la machine de développement, 214 pilotes sur 256 sont signalés. À affiner (exclure ou distinguer les pilotes fournis par Microsoft).
- Les liens ne sont vérifiés qu'à la date indiquée : les sites changent.

## Tests associés

- `AnalysePilotesTests` : règle des deux ans (bornes, sans date), deux formats de date, anciens comptés, liens selon le fabricant, fabricant non vérifié, adresses HTTPS, lecture indisponible.
- `AnalysePilotesReelTests` : lecture réelle sur la machine de test (256 pilotes).

Liens : [[ADR-010-pilotes]].
