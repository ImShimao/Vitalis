---
id: ROADMAP
type: doc
statut: en-cours
cree: 2026-11-19
---

# Roadmap Vitalis

Statuts : **À faire** · **En cours** · **Fait** (code testé, en attente de validation) · **Validé** (validé par l'utilisateur).

Règle : on ne commence pas une étape avant la validation de la précédente.

| Étape | Objectif | Statut | Tâche | Journal |
|---|---|---|---|---|
| 0 | Mise en place : dépôt, solution, CI, coffre, ADR-001 | Validé | [[T-001-mise-en-place]] | [[etape-00]] |
| 1 | Shell de l'interface : fenêtre, Mica, 10 pages, thèmes | Validé (par délégation, ajustements faits) | [[T-002-shell-interface]] | [[etape-01]] |
| 2 | Inventaire matériel (WMI) | Validé | [[T-003-inventaire-materiel]] | [[etape-02]] |
| 3 | Santé disques et espace libre | Validé (SMART non fourni par cette machine) | [[T-004-sante-disques]] | [[etape-03]] |
| 4 | Windows Update et logiciels (lecture seule) | Validé (par délégation ; winget par CLI, ADR-007) | [[T-005-windows-update-logiciels]] | [[etape-04]] |
| 5 | Tableau de bord et analyse asynchrone | Validé (formule du score à revoir, ADR-008) | [[T-006-tableau-de-bord]] | [[etape-05]] |
| 6 | Rapport HTML puis PDF | Fait (PDF non vérifié visuellement, ADR-009) | [[T-007-rapport]] | [[etape-06]] |
| 7 | Pilotes : liste, règle des 2 ans, liens officiels | Fait (4 liens fabricants vérifiés sur 11) | [[T-008-pilotes]] | |
| 8 | Démarrage : désactivation réversible | Fait (clé Run + sauvegarde, ADR-011) | [[T-009-demarrage]] | |
| 9 | Performances : benchmarks et historique | Fait (GPU non mesuré) | [[T-010-performances]] | |
| 10 | Santé live (LibreHardwareMonitor) | Non commencée : décision requise (pilote noyau, licence, Vanguard) | [[T-011-sante-live]] | |
| 11 | Paramètres et journal d'erreurs | Partiel (thème, journal Serilog ; analyse planifiée et notifications à faire) | [[T-012-parametres-journal]] | |
| 12 | Distribution : installeur, signature, politique, changelog | Partiel (changelog, politique, publication portable ; installeur et signature à décider) | [[T-013-distribution]] | |
| 13 | Refonte de l'interface : styles, menu, chargements, pages Matériel et Rapport | Fait (Windows 10 et thème clair non vérifiés, ADR-013) | | |

## Critères de fin par étape

- **Étape 0** : `dotnet build` et `dotnet test` passent ; la CI est verte ; le coffre s'ouvre dans Obsidian sans lien cassé.
- **Étape 1** : les pages s'ouvrent ; contraste WCAG AA vérifié ; `04-Fonctions/` contient un fichier par page.
- **Étape 2** : tests avec données simulées (machine complète et machine avec classe WMI manquante).
- **Étape 3** : demande d'élévation seulement ici, avec message explicatif.
- **Étape 4** : aucune installation, lecture seule.
- **Étape 5** : une erreur dans une carte n'empêche pas les autres.
- **Étape 6** : aucune donnée personnelle dans le rapport.
- **Étape 7** : chaque URL fabricant vérifiée, date de vérification notée.
- **Étape 8** : état précédent sauvegardé, confirmation demandée.
- **Étape 9** : chaque résultat étiqueté avec sa méthode.
- **Étape 10** : seulement si l'étape 9 est validée.
- **Étape 11** : logs locaux uniquement.
- **Étape 12** : installeur, signature, politique de confidentialité, changelog v0.1.0.

Matrice de tests : [[matrice-compatibilite]]. Vision : [[01-Vision]].
