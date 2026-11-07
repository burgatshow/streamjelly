# Changelog

**[English](#english) · [Magyar](#magyar)**

Format: [Keep a Changelog](https://keepachangelog.com/). Versions follow the plugin assembly version.

---

## English

### [1.7.0] – 2026-11-07
#### Added
- Approximate preview of the playback position in the expanded view: the trickplay thumbnail (the picture Jellyfin shows when seeking) closest to the position the client last reported, with its time. Off by default; turn it on with *Show approximate preview* in the settings.
- Needs trickplay images for the video (library setting *Enable trickplay image extraction*); without them the view says there is no preview.
- API: `showPreview` in the response, `preview` per session (trickplay resolution, tile sheet index, column and row).

#### Fixed
- The error text of a session that could not be inspected was partly Hungarian in the API.

### [1.6.0] – 2026-11-05
#### Added
- English user interface. The dashboard page follows the Jellyfin display language: Hungarian for Hungarian, English for every other language. All labels, states, transcode reasons, messages and number formats (decimal separator, time) are translated.

#### Changed
- Plugin description (`meta.json`, plugin list) is now in English.
- API: the descriptive texts of the per-stream decisions (`to`, `note`) are now in English; the API is language independent.
- `build.sh` comments in English.
- `meta.json`: `autoUpdate` is now `true`, so Jellyfin's *Update Plugins* task installs new versions from the repository.

### [1.5.0] – 2026-11-05
#### Changed
- Plugin renamed from *Stream Inspector* to **StreamJelly** (display name, namespace, assembly, project file, dashboard page).
- API route is now `GET /StreamJelly/Sessions`.
- Build output folder: `dist/StreamJelly_<version>`.

#### Added
- Logo and icons (`assets/`); plugin card image in the dashboard's plugin list (`thumb.png`, via `imagePath` in `meta.json`). The card image keeps its content in the centre, so it is not cut off at any card width.

#### Added (distribution)
- Jellyfin plugin repository (`manifest.json`): install and update StreamJelly from the Dashboard's catalog.
- `release.sh`: builds the release zip and release notes and adds the version to `manifest.json`; the GitHub Release is created by hand.

#### Notes
- Plugin GUID unchanged. The settings file name follows the assembly name, so settings reset to defaults after upgrading. Remove the old `StreamInspector_*` folder from the plugins directory.

### [1.4.0] – 2026-11-05
#### Added
- Playback mode badge (Direct play / Remux / Transcode) on the collapsed cards.

### [1.3.0] – 2026-11-05
#### Added
- Client report line in the expanded view: the audio and subtitle track the client last reported, and how long ago. Highlighted when older than 30 seconds.
- API: `lastReportUtc`, `hasProgressReport` per session, `deliveryMethod` per stream.

#### Fixed
- When a session has several ffmpeg jobs (e.g. after a track switch), the newest one is shown.

### [1.2.0] – 2026-11-05
#### Added
- Active playbacks shown as compact cards (cover, title, user, client, progress); click or press Enter to expand to the detailed view, *Összecsukás* to collapse.
- Expanded cards and opened details stay open across refreshes.

### [1.1.0] – 2026-11-05
#### Changed
- Redesigned dashboard page: backdrop header with poster, Source → Server → Client lanes per stream with colour-coded decisions, live numbers row, transcode reasons as tags, collapsible details, ffmpeg command with copy button, settings behind a button, totals in the header, mobile layout.

#### Added
- API: `backdropItemId`, `posterItemId`, `itemId` per session.
- Live bandwidth to the client, encoding speed (×), transcode buffer ahead of playback.

### [1.0.0] – 2026-11-05
#### Added
- First version for Jellyfin 12.4 (.NET 10).
- Admin dashboard page listing every active playback: source media and streams, output parameters, per-stream decisions, transcode reasons, ffmpeg job details and command line.
- `GET /StreamInspector/Sessions` JSON endpoint (admin only).
- ffmpeg jobs read from `TranscodeManager` via reflection, with graceful fallback.
- Settings: refresh interval, show paths, show ffmpeg command.
- Docker build script and plugin manifest.

---

## Magyar

### [1.7.0] – 2026-11-07
#### Új
- Megközelítő előnézet a lejátszás helyéről a kibontott nézetben: a kliens által utoljára jelentett pozícióhoz legközelebbi trickplay kép (amit a Jellyfin tekeréskor mutat), az időpontjával. Alapból ki van kapcsolva; a beállításokban a *Megközelítő előnézet mutatása* kapcsolja be.
- A videóhoz trickplay képek kellenek (könyvtárbeállítás: *Trickplay képek kinyerésének engedélyezése*); ezek nélkül a nézet jelzi, hogy nincs előnézet.
- API: `showPreview` a válaszban, munkamenetenként `preview` (trickplay felbontás, tile sheet sorszáma, oszlop és sor).

#### Javítva
- A nem elemezhető munkamenet hibaszövege az API-ban részben magyar volt.

### [1.6.0] – 2026-11-05
#### Új
- Angol nyelvű felület. Az oldal a Jellyfin megjelenítési nyelvét követi: magyar nyelvnél magyarul, minden más nyelvnél angolul jelenik meg. Minden felirat, állapot, átkódolási ok, üzenet és számformátum (tizedesjel, időpont) le van fordítva.

#### Változott
- A plugin leírása (`meta.json`, bővítménylista) angol lett.
- API: a sávonkénti döntések leíró szövegei (`to`, `note`) angolok lettek; az API nyelvfüggetlen.
- A `build.sh` megjegyzései angolul.
- `meta.json`: az `autoUpdate` mostantól `true`, így a Jellyfin *Bővítmények frissítése* ütemezett feladata telepíti az új verziókat az adattárból.

### [1.5.0] – 2026-11-05
#### Változott
- A plugin új neve **StreamJelly** (korábban *Stream Inspector*): megjelenített név, névtér, DLL, projektfájl, vezérlőpult oldal.
- Az API végpont: `GET /StreamJelly/Sessions`.
- Fordítási kimenet: `dist/StreamJelly_<verzió>`.

#### Új
- Logó és ikonok (`assets/`); kép a vezérlőpult bővítménylistájában (`thumb.png`, a `meta.json` `imagePath` mezőjével). A kép tartalma középen van, így semmilyen kártyaszélességnél nem vágódik le.

#### Új (terjesztés)
- Jellyfin plugin-adattár (`manifest.json`): a StreamJelly a Vezérlőpult katalógusából telepíthető és frissíthető.
- `release.sh`: elkészíti a kiadási zip-et és a kiadási megjegyzést, és felveszi a verziót a `manifest.json`-ba; a GitHub Release kézzel készül.

#### Megjegyzés
- A GUID nem változott. A beállításfájl neve a DLL nevét követi, ezért frissítés után a beállítások alapértékre állnak. A régi `StreamInspector_*` mappát töröld a plugins könyvtárból.

### [1.4.0] – 2026-11-05
#### Új
- Lejátszási mód jelvény (Közvetlen lejátszás / Remux / Átkódolás) az összecsukott kártyákon.

### [1.3.0] – 2026-11-05
#### Új
- Kliens jelentés sor a kibontott nézetben: melyik hang- és feliratsávot jelentette utoljára a kliens, és mikor. 30 másodpercnél régebbi jelentésnél kiemelve.
- API: munkamenetenként `lastReportUtc`, `hasProgressReport`, stream-enként `deliveryMethod`.

#### Javítva
- Ha egy munkamenethez több ffmpeg folyamat tartozik (pl. sávváltás után), a legújabb látszik.

### [1.2.0] – 2026-11-05
#### Új
- Az aktív lejátszások kompakt kártyákként jelennek meg (borító, cím, felhasználó, kliens, haladás); kattintásra vagy Enterrel kibonthatók, az *Összecsukás* gombbal bezárhatók.
- A kibontott kártyák és a lenyitott részletek frissítéskor nyitva maradnak.

### [1.1.0] – 2026-11-05
#### Változott
- Újratervezett felület: háttérképes fejléc poszterrel, Forrás → Szerver → Kliens sávok színkódolt döntésekkel, élő számok sora, átkódolási okok címkékként, lenyitható részletek, ffmpeg parancs másolás gombbal, beállítások gomb mögött, összesítő a fejlécben, mobil nézet.

#### Új
- API: munkamenetenként `backdropItemId`, `posterItemId`, `itemId`.
- Élő sávszélesség a kliens felé, kódolási sebesség (×), előre átkódolt rész.

### [1.0.0] – 2026-11-05
#### Új
- Első verzió Jellyfin 12.4-hez (.NET 10).
- Rendszergazdai vezérlőpult oldal minden aktív lejátszással: forrás és stream-ek, kimeneti paraméterek, sávonkénti döntések, átkódolás okai, ffmpeg folyamat adatai és parancssora.
- `GET /StreamInspector/Sessions` JSON végpont (csak rendszergazdának).
- ffmpeg folyamatok olvasása a `TranscodeManager`-ből reflexióval, hiba esetén nélküle is működik.
- Beállítások: frissítési gyakoriság, útvonalak és ffmpeg parancs megjelenítése.
- Docker fordító szkript és plugin manifest.
