---
id: JOURNAL-04
type: journal
priorite: P1
statut: fait
etape: 4
tags: [journal, windows-update, winget]
cree: 2026-11-20
---

# Étape 4 : Windows Update et logiciels (lecture seule)

Branche : `etape-04-mises-a-jour`. Tâche : [[T-005-windows-update-logiciels]]. Fonctions : [[windows-update]], [[logiciels-winget]]. Décisions : [[ADR-006-windows-update-com]], [[ADR-007-winget-cli]].

## Ce qui est fait

- Windows Update : mises en attente (logiciels et pilotes) et dernière installation réussie, par l'API COM `Microsoft.Update.Session`.
- Logiciels : mises à jour signalées par `winget upgrade` (commande, lecture seule), tableau texte lu à partir de la droite.
- Page « Mises à jour » avec les deux listes.
- Trois services derrière des interfaces (`IServiceWua`, `IServiceWinget`), chacun isolé : une erreur dans l'un n'empêche pas l'autre.
- Vérifié sur la machine : 4 mises à jour Windows (1 logiciel, 3 pilotes), 30 logiciels winget, dernière installation Windows lue.

## Ce qui n'est pas fait

- L'API COM WinGet (`Microsoft.Management.Deployment`) n'a pas été utilisée : introuvable dans la doc Microsoft. Remplacée par la commande, décision validée (ADR-007).
- Lien `ms-settings:windowsupdate` : non vérifié, non ajouté.
- Installation de quoi que ce soit : hors périmètre de l'étape, rien n'est installé.

## Problèmes rencontrés et corrigés

1. **`Concat` en échec à la recherche réelle** : un argument `dynamic` rendait le résultat dynamique. Corrigé (`object` typé). Trouvé par le test réel.
2. **Conflit de noms dans le faux de test** : propriété et méthode homonymes. Renommé.
3. **Noms de logiciels mal lus** : la ligne de Visual C++ n'a pas été lue avec une regex à deux espaces minimum, car le nom est collé à l'identifiant par un seul espace. Remplacé par une lecture à partir de la droite. Trouvé par le test sur la sortie réelle.
4. **Préfixe « < » sur la version actuelle** : nettoyé.
5. **Faute de frappe dans mes scripts PowerShell** : `\n` littéral inséré dans un fichier de test. Corrigé.

## Résultats des tests

- `dotnet test` : 80 réussis, 0 échec (dont les tests réels WUA, historique et winget).
- Build de l'application : 0 erreur, 0 avertissement. Build compilé dans un dossier séparé (l'instance élevée verrouille toujours le dossier normal).
- Vérification visuelle : page « Mises à jour » ouverte, 30 logiciels et 4 mises Windows affichés.

## Non testé

- Machine sans mise à jour ou avec Windows Update arrêté : cas simulés seulement.
- Versions de winget autres que 1.29.380 : le format texte peut changer.
- Temps d'affichage : la recherche Windows Update prend environ 30 secondes avant l'affichage.

## Ce qui reste

- Valider l'étape 4.
- Étape 5 (tableau de bord) : dépend des étapes 2 à 4.
