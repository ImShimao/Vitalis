---
id: ADR-010
type: adr
priorite: P1
statut: fait
etape: 7
tags: [pilotes, liens, verification]
cree: 2026-11-20
---

# ADR-010 : liens fabricants vérifiés uniquement

## Contexte

La spec demande des liens officiels par fabricant, chacun vérifié avec sa date. Le réseau de la machine de test peut contrôler les adresses, mais beaucoup de sites bloquent les requêtes automatisées (HTTP 403).

## Décision

- Une table fixe de liens. Un lien n'est affiché que s'il a répondu HTTP 2xx (ou 202, signalé « à confirmer ») au contrôle, à la date indiquée.
- Les fabricants non vérifiés n'ont pas de lien : on écrit à l'utilisateur de chercher sur le site du fabricant.
- Aucun pilote n'est téléchargé ni installé.

## Alternatives écartées

- **Lien de recherche générique** (ex. moteur de recherche) : pas une source officielle.
- **Adresses non vérifiées mais plausibles** : interdit par la spec ; une adresse fausse mènerait à un site inconnu.

## Conséquences

- La couverture des liens est partielle (4 fabricants sur 11). À compléter après contrôle manuel des autres.
- Les adresses doivent être recontrôlées régulièrement.

Liens : [[pilotes]], [[T-008-pilotes]].
