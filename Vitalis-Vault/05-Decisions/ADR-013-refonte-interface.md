---
id: ADR-013
type: adr
priorite: P1
statut: fait
etape: 13
tags: [interface, ux, design, chargement]
cree: 2026-11-20
---

# ADR-013 : refonte de l'interface

## Contexte

Après l'étape 12, l'interface affichait surtout du texte brut. Certaines lectures bloquaient la fenêtre (pilotes, démarrage, export PDF), chaque visite relançait l'analyse (30 secondes pour Windows Update), deux pages restaient vides (Matériel, Rapport), et le contenu débordait à droite quand la fenêtre n'était pas agrandie.

## Décision

- **Styles communs** (`Themes/Couleurs.xaml`) : titre de page, description, titre de section, carte Windows 11, largeur maximale du contenu. Couleurs d'état (succès, attention, critique) vérifiées à 4,5:1 par `ThemeContrasteTests`, toujours avec une icône et un mot.
- **Briques partagées** (`Views/Ui.cs`) : carte, badge, statistique, liste, état vide, message, chargement, `GrilleAdaptative` (remplace UniformGrid, absent de WinUI 3).
- **Fenêtre** : barre de titre intégrée qui suit le thème choisi, menu en `ListView` avec icônes (flèches du clavier, infobulles avec le raccourci), Paramètres en bas, transitions de page, taille de départ 1280×840 centrée, taille minimale 900×600.
- **Pas de défilement horizontal** : chaque page s'adapte à la largeur de la fenêtre.
- **Résultats gardés pour la session** (`EtatAnalyse`, en mémoire seulement) : revenir sur une page ne relance pas l'analyse ; quitter une page ne coupe pas l'analyse en cours ; l'analyse du tableau de bord remplit Matériel, Disques et Mises à jour, et ces pages attendent son résultat au lieu d'en lancer une deuxième.
- **Lectures lentes hors du thread d'interface** : pilotes, démarrage, export HTML/PDF, benchmarks (étape par étape, avec progression et annulation).
- **Nouvelles pages** : Matériel (fiches par composant), Rapport (export, ouvrir le fichier, ouvrir le dossier).
- **Démarrage** : les entrées HKLM ne proposent plus « Désactiver » (le registre le refusait après avoir écrit la sauvegarde).

## Conséquences

- Le menu n'est plus fait de boutons mais d'une `ListView` (même principe, sélection visible, navigation au clavier). Le `NavigationView` reste écarté.
- Le tableau de bord ne lance pas l'analyse tout seul : un bouton, avec l'explication de la durée.
- Non vérifié : Windows 10 (pas de Mica, police Segoe MDL2 Assets pour les icônes), thème clair à l'écran, lecteur d'écran.

Liens : [[ADR-002-dix-pages]], [[ADR-008-score]], [[tableau-de-bord]], [[materiel]], [[rapport]].
