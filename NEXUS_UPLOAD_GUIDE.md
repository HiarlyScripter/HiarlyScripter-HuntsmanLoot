# Nexus Mods Upload Guide — HuntsmanLoot v1.1.4

## Pre-upload checklist

- [ ] Nexus ZIP built: `HuntsmanLoot-v1.1.4-NexusMods.zip`
- [ ] DLL is the Release build (not Debug) — verify size ~40KB
- [ ] Description copied from `NEXUS_DESCRIPTION.bbcode`
- [ ] Changelog copied from `NEXUS_CHANGELOG.txt`
- [ ] Header image uploaded: `assets/branding/nexus_header_1280x720.png`
- [ ] Gallery images uploaded (up to 10): gallery_01, 02, 03

---

## 1. Create the mod page (first time) / Update version (subsequent)

URL: https://www.nexusmods.com/repo/mods/add

| Field | Value |
|---|---|
| Mod name | HuntsmanLoot |
| Version | 1.1.4 |
| Category | Gameplay — Enemies |
| Short description | Makes the Huntsman drop his own weapon (Hunter Gun visual) on death. |
| Tags | BepInEx, Harmony, Drop, Loot, Enemy, Weapon, Multiplayer |

---

## 2. Description tab

Paste the full content of `NEXUS_DESCRIPTION.bbcode`.

Nexus supports BBCode. The `[line]`, `[size]`, `[b]`, `[list]`, `[code]`, `[font]` tags all work.

---

## 3. Images tab

**Header image (required):**
- File: `assets/branding/nexus_header_1280x720.png`
- This becomes the main mod thumbnail on Nexus

**Gallery (optional but recommended):**
1. `assets/branding/gallery_1920x1080_01.png` — Features overview
2. `assets/branding/gallery_1920x1080_02.png` — How it works (flow diagram)
3. `assets/branding/gallery_1920x1080_03.png` — Configuration reference

---

## 4. Files tab

Upload `HuntsmanLoot-v1.1.4-NexusMods.zip` with these settings:

| Field | Value |
|---|---|
| File name | HuntsmanLoot |
| Version | 1.1.4 |
| File category | Main files |
| Description | BepInEx plugin DLL — place in BepInEx/plugins/ |
| Is main file | Yes |

**ZIP internal structure:**
```
BepInEx/
└── plugins/
    └── HiarlyScripter-HuntsmanLoot/
        └── HuntsmanLoot.dll
```

---

## 5. Changelog tab

Paste the content of `NEXUS_CHANGELOG.txt`.

---

## 6. Requirements tab

| Requirement | Version | Type |
|---|---|---|
| BepInExPack for R.E.P.O. | 5.4.2305 | Required |

Nexus link for BepInEx: https://www.nexusmods.com/repo/mods/1  
*(or link to the Thunderstore page)*

---

## 7. Permissions

- Mod permissions: **Do not redistribute** (or set as preferred)
- Asset permissions: **Do not use assets from this mod** (uses game assets via runtime ref)
- Credit: HiarlyScripter

---

## Notes

- Do NOT publish until explicitly told to do so.
- The mod page can be saved as draft and reviewed before publishing.
- Nexus API token is stored in environment variable `NEXUS_API_KEY` — never hardcode.
