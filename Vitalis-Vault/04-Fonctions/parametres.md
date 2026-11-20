---
id: F-parametres
type: fonction
priorite: P1
statut: partiel
etape: 11
tags: [fonction, parametres, journal, theme]
cree: 2026-11-20
---

# Paramètres

## Couverture

Thème (suivre Windows, clair, sombre), mode diagnostic, accès au dossier des journaux. Code : `src/Vitalis.Core/Parametres/` et `src/Vitalis.App/Views/ParametresPage.xaml`. Tâche : [[T-012-parametres-journal]].

## Ce qui est fait

- **Thème** : enregistré dans `%LOCALAPPDATA%\Vitalis\preferences.json`, appliqué au démarrage et immédiatement. Une valeur inconnue revient à « Suivre Windows ». Fichier corrompu : valeurs par défaut.
- **Mode diagnostic** : active le niveau Débogage du journal, au prochain démarrage.
- **Dossier des journaux** : ouvre `%LOCALAPPDATA%\Vitalis\journaux`.

## Journal (voir [[journal-erreurs]])

- Serilog 4.3.1 et Serilog.Sinks.File 6.0.0, licence Apache-2.0 (vérifiée sur NuGet).
- Un fichier par jour (`vitalis-AAAAMMJJ.log`), 14 jours conservés, niveaux Information, Avertissement, Erreur (et Débogage en mode diagnostic).
- Les chemins de profil `C:\Users\<nom>` sont remplacés par `C:\Users\[utilisateur]` avant écriture ; les erreurs ne journalisent que le type d'exception, pas son message complet.
- Vérifié : le journal de démarrage ne contient pas le nom d'utilisateur.

## Non faits (étape 11 partielle)

- **Analyse planifiée (Planificateur de tâches)** : non implémentée. La tâche lancerait l'application avec un mode « analyse » qui n'existe pas encore ; créer la tâche sans ce mode serait trompeur. À faire avec confirmation, sans droits administrateur (tâche de l'utilisateur).
- **Notifications Windows** : non implémentées (aucune méthode vérifiée dans la doc pour cette version du Windows App SDK).

## Tests associés

- `JournalVitalisTests` : masquage des chemins de profil, texte inchangé, création du dossier et du fichier de journal.
- `PreferencesTests` : thème inconnu, aller-retour, fichier absent, fichier corrompu.
- Réel : démarrage de l'application, page Paramètres ouverte, journal écrit sans nom d'utilisateur.

Liens : [[journal-erreurs]], [[ADR-012-serilog]].
