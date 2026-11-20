---
id: ADR-006
type: adr
priorite: P1
statut: fait
etape: 4
tags: [windows-update, com, dependance]
cree: 2026-11-20
---

# ADR-006 : Windows Update via l'API COM, liaison tardive, lecture seule

## Contexte

L'étape 4 doit lister les mises à jour Windows en attente et la dernière installation, sans rien installer. La spec demande l'API WUA COM.

## Décision

- Appeler `Microsoft.Update.Session` par **liaison tardive** (`dynamic`) : pas de référence de type COM à maintenir, et le code reste dans une seule classe (`WuaCom`) derrière `IServiceWua`.
- **Lecture seule** : seules les recherches et l'historique sont appelés. Aucun téléchargement, aucune installation.
- Deux recherches séparées (logiciel, pilote) plutôt qu'un filtre sur le contenu.
- Les appels COM sont faits dans `Task.Run` : l'interface ne se bloque pas pendant la recherche (environ 30 secondes).

## Alternatives écartées

- **Référence de type COM (interop)** : plus de code généré, et une dépendance de build à maintenir pour peu d'appels.
- **PowerShell `PSWindowsUpdate`** : module tiers, non vérifié, et lancement d'un processus.
- **Clé de registre `WindowsUpdate`** : donne l'état de l'installation mais pas la liste précise des mises en attente.

## Piège rencontré

Passer un argument `dynamic` à une méthode rend son résultat `dynamic` : `Concat` échouait à l'exécution. Corrigé en typant l'objet recherche en `object` avant l'appel. Ce bug n'était visible qu'avec le test réel : d'où l'intérêt des tests sur la machine.

## Conséquences

- Les tests simulés couvrent la logique ; les tests réels couvrent l'appel COM.
- Une erreur COM est capturée et affichée comme section « indisponible ».
- La doc Microsoft des propriétés `IUpdate` a été vérifiée ; la valeur de `MsrcSeverity` peut être vide (limite connue).

Liens : [[windows-update]], [[ADR-004-wmi-system-management]].
