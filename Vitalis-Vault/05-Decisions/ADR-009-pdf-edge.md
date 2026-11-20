---
id: ADR-009
type: adr
priorite: P1
statut: fait
etape: 6
tags: [pdf, rapport, dependance]
cree: 2026-11-20
---

# ADR-009 : export PDF via Microsoft Edge (impression sans affichage)

## Contexte

Le rapport doit exister en PDF. Les bibliothèques PDF .NET demandent une vérification de licence (QuestPDF, iText, etc.) qu'il faut faire avant usage ; la spec demande aussi de limiter les dépendances.

## Décision

Générer le rapport en HTML, puis l'imprimer en PDF avec Microsoft Edge en mode sans affichage (`--headless --print-to-pdf`). Edge est présent sur Windows 10 et 11 ; aucune dépendance NuGet n'est ajoutée.

## Alternatives écartées

- **QuestPDF** : bonne API, mais licence à vérifier (gratuite sous conditions de chiffre d'affaires) ; à réévaluer si Edge pose problème.
- **iTextSharp / iText 7** : licence AGPL, incompatible avec un logiciel distribué sans publier son code.
- **PdfSharp / MigraDoc** : licence MIT, mais mise en page PDF manuelle et plus de code pour le même rendu.

## Conséquences

- Le rendu PDF est celui d'Edge : bonne qualité, avec les en-têtes et pieds de page désactivés (`--no-pdf-header-footer`).
- Si Edge est absent, l'export PDF échoue proprement ; le HTML reste disponible.
- Le PDF n'est pas vérifié visuellement dans cet environnement (outil de rendu absent). À faire à la validation.
- Le délai est de 60 secondes ; au-delà, le processus est arrêté.

Liens : [[rapport]], [[T-007-rapport]].
