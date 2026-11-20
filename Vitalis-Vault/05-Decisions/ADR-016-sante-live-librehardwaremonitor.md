---
id: ADR-016
type: adr
priorite: P1
statut: fait
etape: 10
tags: [sante-live, capteurs, dependance, licence]
cree: 2026-11-20
---

# ADR-016 : santé live avec LibreHardwareMonitorLib 0.9.6 et PawnIO

## Contexte

L'étape 10 attendait une décision (pilote noyau, licence, anti-triche). L'utilisateur a demandé la santé live le 2026-11-20 : décision prise.

## Décision

- Dépendance **LibreHardwareMonitorLib 0.9.6** (NuGet), licence **MPL-2.0** : utilisable dans un logiciel fermé tant que les fichiers de la bibliothèque ne sont pas modifiés (ils ne le sont pas). Mentionner la licence dans la documentation de distribution.
- Depuis la 0.9.5, l'accès bas niveau du processeur passe par **PawnIO** (pilote signé, open source, pawnio.eu), et non plus par WinRing0 (signalé comme pilote vulnérable par Microsoft Defender). PawnIO n'est **pas** embarqué : Vitalis propose de l'installer par winget (`namazso.PawnIO`), après confirmation.
- Mode dégradé : sans PawnIO, les températures du processeur sont « indisponibles » avec une explication ; cartes graphiques, mémoire, disques et charges restent lus.
- Lecture toutes les 2 secondes hors du thread d'interface ; capteurs fermés en quittant la page.

## Risques

- Anti-triches (Riot Vanguard, FACEIT) et « Intégrité de la mémoire » peuvent bloquer un pilote noyau : signalé dans la page.
- Vérifié sur la machine de développement (sans PawnIO, sans administrateur) : NVIDIA RTX 3050 lue (température, charge), processeur i7-10700KF lu sans température, comme prévu.

Liens : [[ADR-015-toujours-administrateur]], [[sante-live]].
