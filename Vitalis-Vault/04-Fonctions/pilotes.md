---
id: F-pilotes
type: fonction
priorite: P1
statut: fait
etape: 14
tags: [fonction, pilotes, liens-officiels, windows-update]
cree: 2026-11-20
---

# Pilotes

## Couverture (étape 14, esprit DriversCloud)

1. **Mises à jour de pilotes** : chaque pilote plus récent du catalogue Windows Update est mis en face du pilote installé du même matériel (« Installé : version X du … → Proposé : pilote du … par … »), avec boutons « Installer » et « Tout installer » (confirmation, puis Windows Update Agent).
2. **Carte graphique, carte mère et BIOS** : version et date du pilote graphique, âge du BIOS, bouton direct vers la page pilotes officielle du fabricant et vers son outil de détection (NVIDIA App, Assistant Intel…).
3. **Tous les pilotes** : recherche, filtre « anciens », badge Windows / Ancien / Récent, lien fabricant.

Code : `src/Vitalis.Core/Pilotes/` (`AnalysePilotes`, `CataloguePilotes`, `LiensFabricants`), page `src/Vitalis.App/Views/PilotesPage.xaml`. Tâche : [[T-008-pilotes]].

## Sources

- `Win32_PnPSignedDriver` : DeviceName, Manufacturer, DriverVersion, DriverDate, DeviceClass, HardWareID.
- Windows Update Agent : `Search("IsInstalled=0 and Type='Driver'")`, `IWindowsDriverUpdate` (DriverHardwareID, DriverModel, DriverProvider, DriverClass, DriverVerDate). Installation ciblée par UpdateID. Doc Microsoft consultée le 2026-11-20.
- Association : l'identifiant matériel installé commence par celui ciblé par Windows Update (insensible à la casse). Pas de correspondance : « Installé : non identifié ».

Pourquoi pas une base de pilotes comme DriversCloud : elle demande un serveur et un catalogue propriétaire. Le catalogue Windows Update est la source officielle (pilotes signés et validés par les fabricants) pour le matériel exact du PC. Pour la carte graphique, le site du fabricant reste souvent plus récent : d'où les liens directs.

## Règle des deux ans et pilotes génériques

« Ancien » : pilote **de fabricant** daté de plus de deux ans. Les pilotes génériques Windows (fabricant vide, « Microsoft », « (Standard …) », « Generic … », « Standard … », « Compatible … », « Xbox ») ne sont jamais « anciens » : Windows Update les gère. Correction du défaut 0.1.0 (214 pilotes sur 256 signalés à tort).

## Table des liens fabricants (contrôle du 2026-11-20)

Méthode : curl avec un agent navigateur ; pour les sites qui bloquent les robots (HTTP 403), ouverture dans un vrai navigateur et contrôle du titre de la page.

| Fabricant | Page | Contrôle |
|---|---|---|
| NVIDIA | nvidia.com/fr-fr/drivers/ (+ NVIDIA App) | HTTP 200 |
| AMD | amd.com/fr/support/download/drivers.html | HTTP 200 |
| Intel | intel.fr/…/support/detect.html (Assistant pilotes et support) | navigateur : « Assistant pilotes et support Intel® » |
| Realtek | realtek.com/Download/Index | HTTP 200 |
| ASUS | asus.com/fr/support/download-center/ | HTTP 200 |
| ASRock | asrock.com/support/index.asp | HTTP 200 |
| MSI (« Micro-Star ») | msi.com/support/download | navigateur : « Download \| MSI Global » |
| Gigabyte | gigabyte.com/fr/Support | navigateur : « Support Services Center - GIGABYTE France » |
| Dell | dell.com/support/home/fr-fr | navigateur : « Accueil du support \| Dell France » |
| HP | support.hp.com/fr-fr/drivers | HTTP 200 |
| Lenovo | pcsupport.lenovo.com/fr/fr | HTTP 200 |
| Acer | acer.com/fr-fr/support/drivers-and-manuals | navigateur : titre Acer France |
| Microsoft Surface | support.microsoft.com/…/download-drivers-and-firmware-for-surface | navigateur |
| Logitech, Razer, SteelSeries, Corsair, Creative, Brother | pages support / logiciels officiels | HTTP 200 |
| Toshiba (stockage), Samsung, Western Digital, Seagate, Crucial, Broadcom | pages support | HTTP 200 (Seagate : navigateur) |
| Kingston, Qualcomm, Synaptics | — | page introuvable (404) : pas de lien |

## Tests associés

- `AnalysePilotesTests` : règle des deux ans, formats de date, liens, fabricant non vérifié, adresses HTTPS.
- `PilotesDriversTests` : pilotes génériques, liens par carte graphique, association catalogue ↔ pilote installé.

Liens : [[ADR-010-pilotes]], [[ADR-015-toujours-administrateur]].
