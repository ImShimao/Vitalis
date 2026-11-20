---
id: JOURNAL-03
type: journal
priorite: P1
statut: fait
etape: 3
tags: [journal, disques, smart, elevation]
cree: 2026-11-20
---

# Étape 3 : santé des disques et espace libre

Branche : `etape-03-sante-disques`. Tâche : [[T-004-sante-disques]]. Fonction : [[sante-disques]]. Décision : [[ADR-005-elevation-a-la-demande]].

## Ce qui est fait

- `AnalyseDisques` (Core) : disques physiques (MSFT_PhysicalDisk), compteurs SMART (MSFT_StorageReliabilityCounter, joints par DeviceId), lecteurs fixes (Win32_LogicalDisk, DriveType = 3).
- Statuts : santé du disque (HealthStatus) ; espace libre (sain au-dessus de 20 %, attention entre 10 et 20 %, critique sous 10 %).
- Droits : `Droits.EstAdministrateur()` et message de confirmation avant relance (`runas`), refus de l'UAC géré.
- Page « Disques » (`DisquesPage`) : statuts écrits en toutes lettres, bouton de relance si SMART refusé. Les autres pages restent des placeholders.
- Note `sante-disques`, ADR-005, tâche T-004.

## Résultats des tests

- `dotnet build Vitalis.sln -c Release` : 0 erreur, 0 avertissement.
- `dotnet test Vitalis.sln -c Release` : 60 réussis, 0 échec. Dont 24 nouveaux tests disques (simulés et réel).
- Capture de la page sur la machine de développement : 5 disques physiques et 5 lecteurs fixes lus ; SMART refusé sans administrateur et la page le dit.

## Problèmes rencontrés

1. **Ctrl+3 ne changeait pas de page** pendant les tests : le focus clavier n'était pas dans la fenêtre. Contourné par un clic d'invocation UI Automation.
2. **Test de lecteur erroné** : 25 % libre était attendu « attention » ; le seuil est sous 20 %. Corrigé dans le test, pas dans le code.
3. **Message d'erreur** avec un point et un espace en trop : nettoyé.
4. **Lecture des compteurs** : refusée sans administrateur ; la jointure n'a donc pas été vérifiée sur machine réelle.

## Non testé

- Relance en administrateur et acceptation de l'UAC : le clic et la boîte de dialogue ne sont pas testés automatiquement.
- Jointure disque ↔ compteurs SMART (DeviceId) sur machine réelle : demande les droits administrateur.
- `OperationalStatus` : non lu, table de valeurs non vérifiée.
- Disques USB : pas de SMART attendu, cas non reproduit.

## Vérification administrateur (2026-11-20)

- Relance en administrateur : fonctionne (UAC accepté, la page ne propose plus la relance).
- Journal diagnostic : 5 disques lus (DeviceId 0 à 4, GUID d'ObjectId). **0 compteur, 0 lien d'association** : Windows ne fournit pas les compteurs SMART sur cette machine.
- Conclusion : la jointure n'était pas en cause. La jointure par association est gardée (elle sera utile sur une machine qui fournit les compteurs). Le message affiché est corrigé : « Windows ne fournit aucun compteur de fiabilité pour ces disques (pilote ou matériel non compatible). »
- Tests : 64 réussis, 0 échec.

## Ce qui reste

- Valider l'étape 3, et faire la vérification administrateur (relance, puis lecture SMART réelle) avant de clore la jointure.
