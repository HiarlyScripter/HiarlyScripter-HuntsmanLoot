# 🔫 Huntsman Loot

[![Thunderstore](https://img.shields.io/badge/Thunderstore-v1.1.4-brightgreen?style=flat-square&logo=thunderstore)](https://thunderstore.io/c/repo/p/HiarlyScripter/HuntsmanLoot/)
[![R.E.P.O.](https://img.shields.io/badge/R.E.P.O.-Build%2023250495-blue?style=flat-square)](https://store.steampowered.com/app/3241660/REPO/)
[![BepInEx](https://img.shields.io/badge/BepInEx-5.4.23.5-yellow?style=flat-square)](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/)
[![Licença](https://img.shields.io/badge/licença-crédito%20obrigatório-red?style=flat-square)](LICENSE)

---

## 🇧🇷 Português

> O Huntsman agora **paga um preço** quando você o elimina — ele larga a própria arma no chão.

O **Huntsman Loot** faz o Huntsman dropar a **Huntsman Rifle** ao ser eliminado. A arma usa o visual nativo **Hunter Gun** do próprio jogo via referência em runtime — sem empacotar assets extraídos, sem copiar assets de mods de referência, sem dependências externas além do BepInEx.

### ✨ Funcionalidades

- 🔫 **Drop ao morrer** — a Huntsman Rifle cai exatamente onde o Huntsman morreu
- 🎨 **Visual real** — mesh nativa Hunter Gun do jogo, cor e material originais
- 🏷️ **Nome correto** — aparece como **Huntsman Rifle** no inventário e na UI
- 🖼️ **Ícone real** — renderizado pelo sistema nativo do jogo (sem sprite procedural)
- 🎲 **Chance configurável** — de 1% (raro) a 100% (sempre)
- 🎰 **Munição aleatória** — arma dropa com ammo randomizada; desative para sempre cair cheia
- 💀 **Modo Berserk** — drop exclusivo de Huntsmans em berserk *(requer BerserkerEnemies, opcional)*
- 👑 **Só o host processa** — spawn via rede; a arma aparece para todos na sala
- 🔧 **Zero dependências extras** — usa apenas assets nativos do jogo
- ⚙️ **REPOConfig** — configs editáveis in-game *(opcional)*

### ⚙️ Configurações

| Chave | Padrão | Descrição |
|---|---|---|
| `DropChance` | `100` | Chance (%) de drop — 1 a 100 |
| `BerserkerOnly` | `false` | `true` = só dropa de Huntsmans berserk |
| `MasterClientOnly` | `true` | ⚠️ Mantenha `true` — evita drops duplicados |
| `RandomizeAmmo` | `true` | `true` = munição aleatória entre 1 e o máximo |

### 📋 Notas técnicas

- Usa o visual nativo **Hunter Gun** do jogo via referência runtime — não redistribui assets extraídos.
- Não copia nem modifica assets de mods de referência.
- Base funcional: shotgun nativa do jogo (`item_gun_shotgun`) com o visual Hunter Gun sobreposto.
- Clipping visual leve possível em paredes/cantos por ser arma longa — não afeta o gameplay.

### 🛠️ Build a partir do fonte

```bash
dotnet build src/HuntsmanLoot.csproj --configuration Release
```

Saída: `build/HuntsmanLoot.dll`

---
---

## 🇺🇸 English

> The Huntsman now **pays the price** when you take him down — his own weapon drops to the floor.

**Huntsman Loot** makes the Huntsman drop the **Huntsman Rifle** when killed. The weapon uses the game's native **Hunter Gun** visual at runtime — no extracted assets bundled, no third-party mod assets copied, no external dependencies beyond BepInEx.

### ✨ Features

- 🔫 **Drop on death** — the Huntsman Rifle falls exactly where the Huntsman died
- 🎨 **Real visual** — game's native Hunter Gun mesh, original color and material
- 🏷️ **Correct name** — shown as **Huntsman Rifle** in inventory and UI
- 🖼️ **Real icon** — rendered by the game's native system (no procedural sprite)
- 🎲 **Configurable chance** — from 1% (rare) to 100% (always)
- 🎰 **Random ammo** — weapon drops with randomized ammo; disable for always-full
- 💀 **Berserk Mode** — berserk-only drop *(requires BerserkerEnemies, optional)*
- 👑 **Host-only processing** — network-spawned; weapon appears for everyone in the room
- 🔧 **Zero extra dependencies** — uses only native game assets
- ⚙️ **REPOConfig** — in-game config editing *(optional)*

### ⚙️ Configuration

| Key | Default | Description |
|---|---|---|
| `DropChance` | `100` | Drop chance (%) — 1 to 100 |
| `BerserkerOnly` | `false` | `true` = only drops from berserk Huntsmans |
| `MasterClientOnly` | `true` | ⚠️ Keep `true` — prevents duplicate drops |
| `RandomizeAmmo` | `true` | `true` = random ammo between 1 and max |

### 📋 Technical notes

- Uses the game's native **Hunter Gun** visual at runtime reference — does not redistribute extracted game assets.
- Does not copy or modify assets from reference mods.
- Functional base: native game shotgun (`item_gun_shotgun`) with Hunter Gun visual overlaid.
- Minor visual clipping possible at walls/corners — does not affect gameplay.

### 🛠️ Build from source

```bash
dotnet build src/HuntsmanLoot.csproj --configuration Release
```

Output: `build/HuntsmanLoot.dll`

### 📄 License

[Custom license](LICENSE) — use and study permitted; **author credit required** on any redistribution.

---

*Mod por / by **[HiarlyScripter](https://discord.com/users/hiarly_ferreira)** · Thunderstore: [HuntsmanLoot](https://thunderstore.io/c/repo/p/HiarlyScripter/HuntsmanLoot/)*
