---
id: T-013
type: tache
priorite: P1
statut: en-cours
etape: 12
tags: [distribution, msix, signature, changelog]
cree: 2026-11-19
---

# T-013 : distribution

## Contexte et but

Préparer la sortie v0.1.0 : installeur, signature de code, politique de confidentialité, vérification des mises à jour de l'application, changelog.

## Critères d'acceptation

- [ ] Package MSIX et version portable `.exe`
- [ ] Installeur
- [ ] Signature de code avec certificat vérifié
- [x] Politique de confidentialité publiée (fichier .md)
- [ ] Vérification de mise à jour de l'application
- [x] `08-Releases/v0.1.0.md` rédigé

## Fichiers de code concernés

- `Package.appxmanifest`, scripts de publication
- `08-Releases/v0.1.0.md`
- Politique de confidentialité (.md)

## Risques

- Certificat de signature : coût et délai à prévoir.
- Le MSIX demande une identité d'application et une signature cohérentes.

## Liens

- Décision : [[ADR-001-winui3]]
- Roadmap : [[02-Roadmap]]
