# Politique de confidentialité de Vitalis

Date : 20 novembre 2026. Version 0.1.0.

## Ce que Vitalis fait avec vos données

Vitalis lit des informations sur votre PC pour l'afficher. Il ne les envoie à aucun serveur : **Vitalis ne se connecte à aucun service distant** pour analyser votre machine. Les seules connexions réseau sont :

- l'ouverture d'un lien vers le site officiel d'un fabricant, à votre demande, dans votre navigateur ;
- la recherche de mises à jour Windows et la liste des logiciels winget, faites par Windows et winget eux-mêmes.

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

Vitalis ne demande jamais l'élévation au lancement. La relance en administrateur n'a lieu qu'après votre clic et la confirmation de Windows (UAC), pour lire les compteurs SMART des disques. Vitalis ne modifie rien sur le disque.

## Démarrage des programmes

Désactiver un programme au démarrage le retire de la clé Run de votre compte, après votre confirmation ; sa commande est sauvegardée et restaurable. Vitalis ne modifie que votre compte (HKCU).

## Contact

Ajoutez ici l'adresse de contact du responsable du projet avant publication.
