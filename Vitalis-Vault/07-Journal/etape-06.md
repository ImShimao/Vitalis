---
id: JOURNAL-06
type: journal
priorite: P1
statut: fait
etape: 6
tags: [journal, rapport, pdf, vie-privee]
cree: 2026-11-20
---

# Étape 6 : rapport HTML et PDF

Branche : `etape-06-rapport`. Tâche : [[T-007-rapport]]. Fonction : [[rapport]]. Décision : [[ADR-009-pdf-edge]].

## Validation de l'étape 5

L'utilisateur a demandé de continuer : l'étape 5 est considérée validée. La formule du score reste à revoir (ADR-008).

## Ce qui est fait

- `RapportHtml` : rapport autonome (score, matériel, disques, lecteurs, mises à jour, liste des données exclues). Texte échappé.
- Filet de sécurité : nom d'utilisateur et nom de machine remplacés par « [masqué] », adresses MAC par « [adresse masquée] ».
- `ExportateurPdf` : Microsoft Edge en mode sans affichage. Aucune dépendance ajoutée.
- Boutons « Exporter en HTML » et « Exporter en PDF » dans la page Tableau de bord, actifs après une analyse.
- `ResultatTableauDeBord` transmet les rapports détaillés au rapport.

## Résultats des tests

- `dotnet test` : 102 réussis, 0 échec (7 nouveaux tests du rapport).
- Build : 0 erreur, 0 avertissement.
- Export réel depuis l'application : `rapport-20261120-1856.pdf` (121 Ko) et `.html` (6 Ko). Audit du HTML : aucun nom d'utilisateur, aucun nom de machine, aucune adresse MAC.

## Problèmes rencontrés

- Premier essai d'export PDF impossible sans Edge : vérifié que Edge est présent et fonctionne en mode sans affichage avant d'écrire le code.

## Non testé

- **Rendu visuel du PDF** : aucun outil de rendu de pages PDF sur cette machine. Le contenu HTML est vérifié, pas la mise en page PDF.
- Export sans Edge (désinstallé) : cas traité par code, non exécuté.
- Rapport d'une machine sans analyse complète : cas simulé seulement.

## Ce qui reste

- Valider l'étape 6, en vérifiant le PDF à l'œil.
- Étape 7 (pilotes) : liens fabricants à vérifier un par un avant usage.
