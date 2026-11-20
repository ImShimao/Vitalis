---
id: F-logiciels-winget
type: fonction
priorite: P1
statut: fait
etape: 4
tags: [fonction, winget, lecture-seule]
cree: 2026-11-20
---

# Logiciels (winget)

## Couverture

Liste les logiciels pour lesquels winget signale une version plus récente : nom, identifiant, version actuelle, version disponible. **Lecture seule** : seule la commande `winget upgrade` est lancée, sans `--all`, sans identifiant, sans installation. Code : `src/Vitalis.Core/MisesAJour/Winget.cs`. Tâche : [[T-005-windows-update-logiciels]]. Décision : [[ADR-007-winget-cli]].

## Source

- Commande : `winget upgrade --source winget --accept-source-agreements --disable-interactivity`.
- Version testée : winget 1.29.380 (machine de développement, 2026-11-20).
- L'API COM WinGet n'a pas été utilisée : elle n'a pas pu être vérifiée dans la documentation Microsoft (voir ADR-007).

## Lecture de la sortie

- Pas de sortie JSON disponible pour `upgrade` dans cette version : la sortie est un tableau texte.
- Lecture **à partir de la droite** : version disponible, version actuelle, identifiant, le reste est le nom. Pourquoi : le nom peut contenir des espaces, et l'identifiant est parfois collé au nom par un seul espace.
- Les lignes de pied de page (« 30 mises à niveau disponibles ») et les avertissements sont ignorés : une ligne n'est retenue que si l'identifiant contient un point et les versions contiennent un chiffre.
- Préfixe « < » sur la version actuelle (ex. `< 175.2.0.13423`) : le « < » est retiré.
- Caractères de contrôle (rotation, retours arrière) retirés avant l'analyse.

## Entrées, sorties

- Entrée : `IServiceWinget` (réel : `WingetCli`, lance le processus ; tests : fausse implémentation). Délai maximal : 3 minutes, puis arrêt du processus.
- Sortie : liste de `MiseAJourLogiciel` (Nom, Id, VersionActuelle, VersionDisponible).

## Cas « indisponible » et absence de donnée

- winget absent ou impossible à lancer : section « indisponible » avec la raison ; Windows Update reste affiché.
- Délai dépassé : section « indisponible ».
- Aucune mise à jour : état « disponible » avec le message « Aucune mise à jour de logiciel signalée ».
- Un logiciel dont la version n'est pas déterminée n'est pas listé : winget le signale lui-même dans un message (`--include-unknown` n'est pas utilisé).

## Limites connues

- **Format texte** : la sortie peut changer d'une version de winget à l'autre. Les tests utilisent la sortie réelle de cette version.
- **Lenteur** : environ 2 secondes sur la machine de test.
- **Logiciels inconnus** : non listés par défaut.

## Tests associés

- Analyse (`WingetTests`) : sortie réelle à 8 lignes, champs, nom avec tirets et parenthèses sans espace double, préfixe « < », pied de page ignoré, aucune mise à jour, caractères de commande.
- Réel (`WingetReelTests`) : appel réel sur la machine de test. Résultat : 30 mises à jour listées.

Liens : [[windows-update]], [[ADR-007-winget-cli]].

## Mise à jour des logiciels (étape 14)

Vitalis installe maintenant les mises à jour, **après confirmation** : bouton « Mettre à jour » par logiciel et « Tout mettre à jour » (un par un). Commande : `winget upgrade --id "<id>" --exact --source winget --silent --accept-package-agreements --accept-source-agreements --disable-interactivity`. L'identifiant est revalidé (`^[A-Za-z0-9][A-Za-z0-9._+-]{0,127}$`) avant d'être placé sur la ligne de commande. Codes interprétés (doc winget returnCodes.md) : 0 réussi, 0x8A150109 / 0x8A15010B redémarrage pour terminer, 0x8A15010A redémarrer puis réessayer, 0x8A15002B plus de mise à jour applicable. Tests : `InstallateurWingetTests`.
