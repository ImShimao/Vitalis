---
id: ADR-012
type: adr
priorite: P1
statut: fait
etape: 11
tags: [journal, serilog, licence]
cree: 2026-11-20
---

# ADR-012 : journalisation avec Serilog

## Contexte

La spec impose Serilog, un fichier local, une rotation quotidienne et les niveaux Information, Avertissement, Erreur.

## Décision

Serilog 4.3.1 et Serilog.Sinks.File 6.0.0 (Apache-2.0, vérifiée sur NuGet). Rotation quotidienne, 14 jours conservés. Masquage des chemins de profil avant écriture.

## Alternatives écartées

- **Microsoft.Extensions.Logging seul** : pas de fichier par défaut ; Serilog est imposé par la spec.
- **Écriture manuelle dans un fichier** : pas de rotation ni de niveaux sans reprendre Serilog.

## Conséquences

- Les nouvelles fonctions doivent passer par `JournalVitalis` et ne jamais écrire de donnée personnelle.

Liens : [[journal-erreurs]], [[parametres]].
