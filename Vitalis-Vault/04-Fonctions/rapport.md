---
id: F-rapport
type: fonction
priorite: P1
statut: fait
etape: 6
tags: [fonction, rapport, html, pdf, vie-privee]
cree: 2026-11-20
---

# Rapport HTML et PDF

## Couverture

Exporte l'analyse du tableau de bord en HTML autonome, puis en PDF. Code : `src/Vitalis.Core/Rapport/` (`RapportHtml`, `ExportateurPdf`), boutons dans `src/Vitalis.App/Views/TableauDeBordPage.xaml`. Tâche : [[T-007-rapport]]. Décision : [[ADR-009-pdf-edge]].

## Contenu du rapport

1. Score global et composants (poids, valeur).
2. Matériel : processeur, mémoire, cartes graphiques (pilote), écrans, réseau, audio.
3. Disques : type, taille, santé, température et usure si lues ; lecteurs avec taille, espace libre et statut.
4. Mises à jour : dernière installation Windows, mises Windows en attente, logiciels winget (versions actuelle et disponible).
5. Liste des données exclues.

## Données exclues (par conception)

| Donnée | Traitement |
|---|---|
| Nom d'utilisateur | Non écrit. Filet de sécurité : toute occurrence dans un texte est remplacée par « [masqué] ». |
| Nom de la machine | Non écrit. Même filet de sécurité. |
| Adresse MAC | Jamais lue (le réseau n'expose que le nom et la description). Filet : tout motif MAC dans un texte devient « [adresse masquée] ». |
| Numéro de série | Jamais lu (ni BIOS, ni carte mère, ni disque). |
| Nom de volume | Non écrit (choisi par l'utilisateur, il peut contenir son nom). |
| Identifiants internes (ObjectId, DeviceId, GUID) | Non écrits. |

Le texte HTML est échappé (`WebUtility.HtmlEncode`) : un nom de disque contenant du HTML ne casse pas la page.

## Export PDF

- Microsoft Edge en mode sans affichage : `msedge --headless --print-to-pdf`. Edge est fourni avec Windows 10 et 11.
- Si Edge est absent ou ne produit pas de fichier (délai de 60 secondes), le message l'indique ; le HTML reste disponible.
- Aucune bibliothèque tierce.

## Emplacement

`Documents\Vitalis\rapport-AAAAMMJJ-HHmm.html` (et `.pdf`).

## Tests associés

- `RapportHtmlTests` (simulés) : absence du nom d'utilisateur et de la machine, nom de volume exclu, adresse MAC masquée, remplacement du nom dans les textes, sections présentes, échappement HTML, rapport sans analyse.
- Réel : export PDF lancé depuis l'application sur la machine de développement. Résultat : `rapport-20261120-1856.pdf` (121 Ko) et `.html` (6 Ko) ; le HTML ne contient ni le nom d'utilisateur, ni le nom de la machine, ni d'adresse MAC.

## Limites connues

- Le PDF n'a pas pu être vérifié visuellement sur cette machine (aucun outil de rendu de pages PDF). Le HTML source est vérifié.
- Le rapport ne contient que les données de la dernière analyse, pas l'historique.

Liens : [[ADR-009-pdf-edge]], [[tableau-de-bord]].

## Interface (étape 13)

L'export a quitté le tableau de bord pour la page Rapport : export HTML ou PDF hors du thread d'interface, boutons « Ouvrir » et « Ouvrir le dossier ». Sans analyse, la page renvoie vers le tableau de bord. Voir [[ADR-013-refonte-interface]].
