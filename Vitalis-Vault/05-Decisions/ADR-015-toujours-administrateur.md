---
id: ADR-015
type: adr
priorite: P1
statut: fait
etape: 14
tags: [elevation, securite, installation]
cree: 2026-11-20
---

# ADR-015 : Vitalis se lance toujours en administrateur

## Contexte

Demande de l'utilisateur (2026-11-20) : lancement systématique en administrateur. Les fonctions ajoutées à l'étape 14 en ont besoin : compteurs SMART, températures du processeur, installation des mises à jour Windows et des pilotes, tâche planifiée avec droits élevés.

## Décision

- `app.manifest` : `requestedExecutionLevel level="requireAdministrator"`. Windows affiche l'UAC à chaque lancement.
- Remplace [[ADR-005-elevation-a-la-demande]]. Le bouton « Relancer en administrateur » de la page Disques reste dans le code mais ne s'affiche plus (le processus est déjà élevé).
- Règle inchangée : **rien n'est modifié sans confirmation explicite** (boîte de dialogue avant chaque installation, chaque désactivation au démarrage, chaque redémarrage).

## Conséquences

- La tâche planifiée doit utiliser « exécuter avec les autorisations maximales » (RunLevel 1), sinon Windows refuse de lancer un exécutable qui exige l'administrateur.
- Un compte standard (non administrateur) doit saisir un mot de passe administrateur à chaque lancement.
- Pour tester sans UAC pendant le développement : variable d'environnement `__COMPAT_LAYER=RunAsInvoker`.

Liens : [[ADR-016-sante-live-librehardwaremonitor]], [[02-Roadmap]].
