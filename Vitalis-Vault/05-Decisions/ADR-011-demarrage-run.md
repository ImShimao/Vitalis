---
id: ADR-011
type: adr
priorite: P2
statut: fait
etape: 8
tags: [demarrage, registre, reversibilite]
cree: 2026-11-20
---

# ADR-011 : désactivation des programmes de démarrage via la clé Run, avec sauvegarde

## Contexte

Désactiver un programme au démarrage doit être réversible, avec confirmation et sauvegarde de l'état précédent (spec). Windows distingue « présent dans Run » et « activé dans le gestionnaire des tâches » (clé StartupApproved).

## Décision

Désactiver = sauvegarder la valeur Run puis la retirer. Réactiver = réécrire la valeur sauvegardée. Seule la clé HKCU est modifiée.

## Alternatives écartées

- **Écrire StartupApproved** : format binaire non documenté par Microsoft ; la recherche de documentation n'a trouvé que des observations de la communauté. Interdit par la règle de vérification.
- **Supprimer définitivement** : non réversible, interdit par la spec.
- **Modifier HKLM** : droits administrateur et impact sur tous les comptes.

## Conséquences

- Une désactivation retire l'entrée de Run : Windows ne la verra plus comme « activée ». Le programme n'est pas supprimé.
- Un programme installé par son propre service (non Run) n'est pas couvert.
- Si l'utilisateur réinstalle le programme, celui-ci peut recréer sa valeur Run : la sauvegarde ne doit alors pas écraser la nouvelle valeur (à traiter).

Liens : [[demarrage]], [[T-009-demarrage]].
