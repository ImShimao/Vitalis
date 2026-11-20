---
id: F-windows-update
type: fonction
priorite: P1
statut: fait
etape: 4
tags: [fonction, windows-update, lecture-seule]
cree: 2026-11-20
---

# Windows Update

## Couverture

Liste les mises à jour Windows en attente (logiciels et pilotes) et donne la date de la dernière installation réussie. **Lecture seule** : rien n'est téléchargé ni installé. Code : `src/Vitalis.Core/MisesAJour/`, page `src/Vitalis.App/Views/MisesAJourPage.xaml`. Tâche : [[T-005-windows-update-logiciels]].

## Source (vérifiée dans la documentation Microsoft le 2026-11-20)

- API COM Windows Update Agent, ProgID `Microsoft.Update.Session`.
- Recherche : `IUpdateSearcher::Search`. Critères : `IsInstalled=0 and IsHidden=0`, avec `Type='Software'` puis `Type='Driver'` (deux recherches séparées). Par défaut, la doc indique `IsInstalled = 0 and IsHidden = 0`.
- Propriétés lues sur `IUpdate` : `Title`, `KBArticleIDs`, `MsrcSeverity`, `IsMandatory`.
- Historique : `IUpdateSearcher::QueryHistory` (début 0, nombre limité à 100), propriétés `Date` et `ResultCode` de `IUpdateHistoryEntry`. Code 2 = réussi, 3 = réussi avec erreurs.

## Entrées, sorties

- Entrée : `IServiceWua` (réel : `WuaCom` ; tests : fausse implémentation). Jeton d'annulation optionnel.
- Sortie : `RapportMisesAJour` : la liste des mises (titre, KB, gravité, obligatoire, pilote), la date de la dernière installation réussie et une raison si elle est absente.

## Cas « indisponible » et absence de donnée

- Recherche en erreur (service arrêté, COM indisponible) : section « indisponible » avec la raison ; l'historique reste lu.
- Historique illisible : date absente et raison affichée.
- Aucune mise en attente : état « disponible » avec « Aucune mise à jour en attente ».
- Gravité vide : affichée comme absente (non inventée).

## Limites connues

- **La recherche est lente** : environ 30 secondes sur la machine de test. Elle ne peut pas être interrompue en cours d'appel COM : l'annulation rend seulement la main à l'interface.
- **Gravité (`MsrcSeverity`)** : peut être vide, selon une question Microsoft Q&A ; ce n'est pas une erreur de l'application.
- **Historique** : limité aux 100 dernières entrées ; la « dernière installation » est la plus récente entrée réussie, sans distinguer le type d'opération.
- **Mise à jour optionnelle** : les mises facultatives ne sont pas forcément listées par la même recherche (remarque Microsoft Q&A).

## Liens (ms-settings)

La page ne propose pas encore de lien vers les paramètres Windows Update : le lien `ms-settings:windowsupdate` n'a pas été vérifié dans la documentation (à faire avant usage).

## Tests associés

- Simulés (`AnalyseMisesAJourTests`) : mises logiciel et pilote, aucune mise, recherche en erreur avec historique lu, dernière installation réussie la plus récente, historique vide, annulation.
- Réels (`WuaReelTests`) : historique lu sur la machine de test ; rapport sans exception non gérée. Résultat sur la machine de test : 4 mises en attente (1 logiciel, 3 pilotes).

Liens : [[ADR-006-windows-update-com]], [[logiciels-winget]].
