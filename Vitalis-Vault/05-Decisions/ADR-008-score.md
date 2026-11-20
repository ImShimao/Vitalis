---
id: ADR-008
type: adr
priorite: P1
statut: fait
etape: 5
tags: [score, tableau-de-bord, conception]
cree: 2026-11-20
---

# ADR-008 : formule du score global

## Contexte

Le tableau de bord affiche un score sur 100. La spec demande « score global / cartes de synthèse » sans fixer la formule : la formule et les poids sont donc à décider ici.

## Décision

- Trois composants notés : Disques (40), Mises à jour Windows (35), Espace libre (25).
- Le matériel n'est pas noté : il ne mesure pas une « bonne santé », c'est une description.
- Les logiciels winget ne sont pas dans le score. Ils sont listés sur la carte Mises à jour.
- Un composant indisponible est retiré du calcul et son poids est réparti sur les autres.

## Alternatives écartées

- **Compter les logiciels dans les mises à jour** : une seule application en retard ferait baisser le score de façon disproportionnée ; la liste winget est aussi moins fiable (format texte).
- **Noter le matériel** : aucun seuil défendable sans règle de référence.
- **Compter un composant indisponible comme zéro** : une erreur de lecture ferait chuter le score sans raison réelle.

## Conséquences

- Les poids et les seuils (10 par mise, 30 jours, 20 %/10 % de l'espace) sont des constantes testées : ils peuvent être revus sans changer le code de calcul.
- Le score n'est pas une mesure objective : c'est une synthèse à titre indicatif. L'interface et la note de fonction le disent.
- **À valider par l'utilisateur** : poids, présence des logiciels dans le score, seuils.

Liens : [[tableau-de-bord]], [[T-006-tableau-de-bord]].
