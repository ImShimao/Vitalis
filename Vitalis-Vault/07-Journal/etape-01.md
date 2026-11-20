---
id: JOURNAL-01
type: journal
priorite: P0
statut: en-cours
etape: 1
tags: [journal, winui, navigation]
cree: 2026-11-20
---

# Étape 1 : shell de l'interface

Branche : `etape-01-shell-interface`. Tâche : [[T-002-shell-interface]]. Décisions : [[ADR-002-dix-pages]], [[ADR-003-portable]].

## Ce qui est fait

- Projet `src/Vitalis.App` : WinUI 3, net8.0-windows10.0.19041.0, portable non empaquetée, `Program.cs` avec `Bootstrap.Initialize`.
- Dix pages définies dans `Vitalis.Core` (`CataloguePages`), testées.
- Menu latéral en boutons, Ctrl+1 à Ctrl+0, page active en gras.
- Page générique `PagePlaceholder` (titre + étape de remplissage).
- Palette clair et sombre (`Themes/Couleurs.xaml`), vérifiée à 4,5:1 par test.
- Dix fichiers `04-Fonctions/` (un par page).
- ADR-002 et ADR-003.
- CI : `-p:Platform=x64` retiré de la commande de build de la solution.

## Résultats des tests

- `dotnet build Vitalis.sln -c Release` : 0 erreur, 0 avertissement.
- `dotnet test Vitalis.sln -c Release` : 21 réussis, 0 échec.
- Lancement de `Vitalis.App.exe` : la fenêtre s'ouvre, titre « Vitalis », processus vivant après 8 secondes.

## Problèmes rencontrés et résolus

1. **Plantage au démarrage** (`0xC000027B`) avec un `NavigationView`. Testé sur quatre versions du Windows App SDK, sans succès.
2. **Plantage à l'ouverture d'une page** avec des styles Fluent (`TitleLargeTextBlockStyle`). Cause : `XamlControlsResources` absent de `App.xaml`. Ajouté, page avec styles à nouveau fonctionnelle.
3. Le modèle WinUI actuel exige le SDK .NET 10. Installé pour générer le modèle de référence seulement ; le projet reste en .NET 8.
4. `Microsoft.Windows.SDK.BuildTools` retiré (erreur WMC9999 du compilateur XAML).

## Tests des pages (UI Automation, 2026-11-20)

- Les 10 boutons existent et s'ouvrent : titre affiché et étape correcte pour chacune (5, 2, 3, 4, 7, 8, 9, 10, 6, 11).
- **Raccourcis Ctrl+1 à Ctrl+0 : corrigés (commit ab333b0).** Les `KeyboardAccelerator` posés sur les boutons ne se déclenchaient pas ; la gestion est passée dans `KeyDown` de la racine, qui lit l'état de Ctrl. Vérifié avec des codes de touche bruts : Ctrl+3 → Disques, Ctrl+0 → Paramètres, Ctrl+1 → Tableau de bord, Ctrl+9 → Rapport.
- Remarque de test : `SendKeys` envoie un Maj parasite sur clavier AZERTY (le 0 y demande Maj), ce qui faisait croire à un échec ; les codes bruts évitent ce biais.

## Vérification visuelle (captures d'écran, 2026-11-20)

- **Thème clair** : correct. Contenu gris clair, boutons lisibles, barre de titre assortie. Testé en forçant `RequestedTheme=Light` sur la racine (test temporaire, retiré ensuite ; le réglage Windows n'a pas été modifié).
- **Thème sombre** (système actuellement en sombre) : contenu sombre correct, mais **la barre de titre reste claire** : défaut cosmétique à corriger.
- **Mica** : `MicaController.IsSupported()` renvoie vrai sur ce Windows 11 (build 26300) et le code applique `MicaBackdrop`. Le rendu n'est pas confirmé au pixel près : la zone de contenu paraît d'une couleur plate, à comparer avec et sans Mica.

## Corrections finales

- **Barre de titre en thème sombre** : corrigée (`AppWindow.TitleBar.PreferredTheme = UseDefaultAppMode`). Mesure : barre RGB(32,32,32) comme le contenu.
- **Mica** : vérifié. Avec Mica, le contenu est à RGB(32,32,32) ; sans Mica, il est noir (0,0,0). Mica agit donc bien. Mesure faite par `PrintWindow` (la capture d'écran simple captait une autre fenêtre).
- **Secours sans Mica** (Windows 10) : fond de la palette ajouté sur la racine, vérifié en forçant la branche : RGB(32,32,32).
- Fenêtre laissée ouverte sur l'écran secondaire (X=2000, Y=100) pour inspection.

## Non testé

- Windows 10 réel : aucune VM disponible. Le secours est vérifié en forçant la branche, pas sur Windows 10.
- Mise en page en largeur réduite : non testée.
- Windows 10 22H2 : aucune VM.
- Retour au `NavigationView` avec le correctif `XamlControlsResources` : pas réessayé.

## Ce qui reste

- Tester les dix pages et les raccourcis, puis vérifier Mica et le thème sombre à l'écran.
- Valider l'étape 1, puis commiter.
