# Politique de confidentialité de Vitalis

Date : 20 novembre 2026. Version 0.2.0.

## Ce que Vitalis fait avec vos données

Vitalis lit des informations sur votre PC pour l'afficher. Il ne les envoie à aucun serveur : **Vitalis ne se connecte à aucun service distant** pour analyser votre machine. Les seules connexions réseau sont :

- l'ouverture d'un lien vers le site officiel d'un fabricant, à votre demande, dans votre navigateur ;
- la recherche et, à votre demande, l'installation des mises à jour Windows et des pilotes, faites par Windows Update ;
- la liste et, à votre demande, la mise à jour des logiciels, faites par winget depuis sa source officielle (Microsoft).

## Données lues

Matériel (processeur, carte mère, BIOS, mémoire, cartes graphiques, écrans, réseau, audio), disques et espace libre, mises à jour, pilotes, programmes de démarrage, résultats de benchmarks.

## Données jamais lues ni écrites dans un rapport

- nom d'utilisateur et nom de la machine (masqués s'ils apparaissent dans un texte) ;
- adresse MAC (jamais lue) ;
- numéros de série (jamais lus) ;
- noms de volumes choisis par l'utilisateur (exclus des rapports) ;
- contenu de vos fichiers.

## Données stockées sur votre PC

Dans `%LOCALAPPDATA%\Vitalis` : préférences, date de la dernière analyse, historique des benchmarks, sauvegarde des programmes de démarrage désactivés, journaux (14 jours). Dans `Documents\Vitalis` : rapports exportés, que vous créez vous-même. Vous pouvez tout supprimer à tout moment.

## Droits d'administrateur

Vitalis se lance en administrateur (confirmation de Windows, UAC, à chaque lancement) : c'est nécessaire pour lire les compteurs SMART et les capteurs, et pour installer les mises à jour. **Rien n'est installé ni modifié sans votre confirmation** dans une boîte de dialogue.

## Capteurs (santé live)

Les températures du processeur passent par PawnIO, un pilote signé installé à part. Vitalis ne l'installe que si vous cliquez sur « Installer PawnIO » et confirmez. Il se désinstalle depuis Paramètres › Applications.

## Analyse planifiée

Si vous l'activez, une tâche « Vitalis - analyse hebdomadaire » est créée dans le Planificateur de tâches. Elle lit l'état du PC et affiche une notification ; elle n'installe rien. La désactiver supprime la tâche.

## Démarrage des programmes

Désactiver un programme au démarrage le retire de la clé Run de votre compte, après votre confirmation ; sa commande est sauvegardée et restaurable. Vitalis ne modifie que votre compte (HKCU).

## Contact

Ajoutez ici l'adresse de contact du responsable du projet avant publication.
