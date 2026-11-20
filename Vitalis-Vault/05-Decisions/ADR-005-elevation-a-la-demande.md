---
id: ADR-005
type: adr
priorite: P1
statut: fait
etape: 3
tags: [elevation, uac, disques, securite]
cree: 2026-11-20
---

# ADR-005 : élévation administrateur à la demande, seulement pour les disques

## Contexte

Les compteurs SMART (MSFT_StorageReliabilityCounter) exigent les droits administrateur. La spec demande que l'élévation ne soit demandée qu'à l'étape 3, avec un message explicatif. Le reste de l'application doit fonctionner sans droits.

## Décision

- L'application démarre **sans élévation**. Les disques et l'espace libre sont lus sans droits.
- Quand les compteurs SMART sont refusés, la page affiche un bouton « Relancer en administrateur pour lire les compteurs SMART ».
- Le clic ouvre une boîte de dialogue : elle explique ce qui sera lu, précise que rien n'est modifié, et laisse le choix entre « Relancer » et « Annuler ».
- Si l'utilisateur confirme, l'application se relance avec `Verb = runas` (Windows affiche l'UAC). Si l'UAC est refusé, la page reste ouverte.
- Le manifeste ne demande pas d'élévation (`asInvoker` par défaut) : l'élévation n'a jamais lieu sans clic.

## Alternatives écartées

- **Élévation au démarrage** : demande l'UAC à chaque lancement, même pour des pages qui n'en ont pas besoin.
- **Service Windows séparé** : architecture lourde pour une seule lecture ; à reconsidérer si d'autres fonctions demandent l'administrateur.
- **Ignorer les compteurs SMART** : ne tient pas la promesse de l'étape 3.

## Conséquences

- L'utilisateur qui n'accepte pas l'UAC n'a pas de SMART. C'est affiché, pas caché.
- La relance crée un nouveau processus : l'état de l'interface n'est pas conservé. Acceptable à ce stade.
- La jointure disque ↔ compteurs ne peut être testée sur machine réelle qu'après une relance administrateur : à faire avant la validation.

Liens : [[sante-disques]], [[T-004-sante-disques]], [[ADR-002-dix-pages]].
