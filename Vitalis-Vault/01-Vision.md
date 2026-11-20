---
id: VISION
type: doc
statut: fait
cree: 2026-11-19
---

# Vision de Vitalis

## Objectif

Vitalis est une application de bureau Windows qui fait deux choses :

1. **Diagnostiquer le PC** : matériel, santé des disques, températures, performances, démarrage, mises à jour Windows.
2. **Dire quoi mettre à jour** : pilotes et applications, avec liens officiels et règles claires.

Inspirations : UserDiag (diagnostic, santé, benchmark) et DriversCloud (matériel et pilotes).

## Public

Utilisateurs Windows 10 et Windows 11 qui veulent comprendre l'état de leur PC sans outil technique.

Niveau visé : grand public. Les explications restent simples, sans jargon technique dans l'interface.

## Périmètre v1

- Interface entièrement en français.
- Inventaire matériel (CPU, carte mère, BIOS, RAM, GPU, écrans, réseau, audio).
- Santé des disques (SMART) et espace libre.
- Mises à jour Windows et d'applications, en lecture seule (aucune installation).
- Tableau de bord avec score global sur 100.
- Rapport HTML et PDF, sans données personnelles.
- Pilotes : liste, règle des 2 ans, liens officiels.
- Démarrage : lecture et désactivation réversible avec confirmation.
- Performances : benchmarks CPU, RAM, disque, GPU, avec méthode indiquée.
- Paramètres, analyse planifiée, notifications, journal d'erreurs.
- Distribution : MSIX et version portable .exe, puis installeur.
- Cible : Windows 10 22H2 et Windows 11, x64.

## Hors périmètre v1

- ARM64.
- Garantie « tous les PC » : la [[matrice-compatibilite]] montre ce qui est réellement testé.
- Santé live (températures temps réel) : étape 10, ajoutée seulement si l'étape 9 est validée.
- Installation automatique de pilotes ou de mises à jour sans confirmation.
- Modification de services système sans confirmation de l'utilisateur.

## Principes

- Lecture d'abord. Tout changement du système demande une confirmation et reste réversible.
- Aucune donnée personnelle dans les rapports.
- Une erreur dans une partie ne bloque jamais les autres.
- Chaque résultat indique sa méthode et ce qui est indisponible.

Voir la [[02-Roadmap]] pour l'ordre de travail et [[ADR-001-winui3]] pour le choix d'interface.
