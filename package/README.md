# 🔫 Huntsman Loot

[![Versão](https://img.shields.io/badge/versão-1.1.3-brightgreen?style=flat-square)](https://thunderstore.io/c/repo/p/HiarlyScripter/HuntsmanLoot/)
[![R.E.P.O.](https://img.shields.io/badge/R.E.P.O.-Build%2023250495-blue?style=flat-square)](https://store.steampowered.com/app/3241660/REPO/)
[![BepInEx](https://img.shields.io/badge/BepInEx-5.4.23.5-yellow?style=flat-square)](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/)
[![Multiplayer](https://img.shields.io/badge/Multiplayer-Host%20Only-9b59b6?style=flat-square)]()
[![REPOConfig](https://img.shields.io/badge/REPOConfig-compatível-orange?style=flat-square)](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/)

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
- 🟢 **Sem barra verde** — barra de bateria suprimida; barra de munição (amarela) funciona normalmente
- 🔧 **Zero dependências extras** — usa apenas assets nativos do jogo
- ⚙️ **REPOConfig** — configs editáveis in-game *(opcional)*

### 👥 Quem precisa instalar?

Somente o **host** precisa ter o mod. A arma aparece para **todos os jogadores**, mesmo os que não têm o mod instalado.

| Cenário | Funciona? |
|---|---|
| Só o host tem o mod | ✅ Sim |
| Todos têm o mod | ✅ Sim |
| Ninguém tem o mod | ❌ Não dropa |

### ⚙️ Configurações

Edite em `BepInEx/config/com.hiarlyscripter.huntsmanloot.cfg` ou use o **REPOConfig** in-game.

| Chave | Padrão | Descrição |
|---|---|---|
| `DropChance` | `100` | Chance (%) de drop — 1 a 100 |
| `BerserkerOnly` | `false` | `true` = só dropa de Huntsmans berserk |
| `MasterClientOnly` | `true` | ⚠️ Mantenha `true` — evita drops duplicados |
| `RandomizeAmmo` | `true` | `true` = munição aleatória entre 1 e o máximo |

### 📦 Dependências

**Obrigatória**

| Mod | Versão | Link |
|---|---|---|
| BepInExPack | `5.4.23.5` | [Thunderstore](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/) |

**Opcionais**

| Mod | Para que serve |
|---|---|
| [REPOConfig](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/) | Editar configs in-game sem abrir o arquivo |
| [BerserkerEnemies](https://thunderstore.io/c/repo/p/Zehs/BerserkerEnemies/) | Necessário se `BerserkerOnly = true` |

### 🛠️ Instalação

**Via r2modman (recomendado)**
1. Instale o **BepInExPack**
2. Procure e instale **Huntsman Loot**
3. *(Opcional)* Instale o **REPOConfig**
4. Clique em **Start modded**

**Via manual**
1. Instale o BepInExPack primeiro
2. Copie a pasta `plugins/HiarlyScripter-HuntsmanLoot/` para dentro de `BepInEx/plugins/`
3. Inicie o jogo

### ❓ Problemas comuns

| Problema | Solução |
|---|---|
| A arma não dropa | Certifique-se de que o **host** tem o mod |
| A arma dropa várias vezes | Mantenha `MasterClientOnly = true` (padrão) |
| Barra verde aparece na arma | Atualize para v1.1.3 |
| Ammo aparece errada | Verifique a configuração `RandomizeAmmo` |

### 📋 Notas técnicas

- O mod usa o visual nativo **Hunter Gun** do jogo via referência em runtime — não redistribui assets extraídos.
- Não copia nem modifica assets de mods de referência.
- A base funcional é a shotgun nativa do jogo (`item_gun_shotgun`) com o visual do Hunter Gun sobreposto.
- Por ser uma arma longa usando a base funcional da shotgun, pode haver clipping visual leve em paredes ou cantos. Não afeta o gameplay.

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
- 🟢 **No green bar** — battery bar suppressed; ammo bar (yellow) works normally
- 🔧 **Zero extra dependencies** — uses only native game assets
- ⚙️ **REPOConfig** — in-game config editing *(optional)*

### 👥 Who needs to install?

Only the **host** needs the mod. The weapon appears for **all players**, even those without the mod installed.

| Scenario | Works? |
|---|---|
| Only host has the mod | ✅ Yes |
| Everyone has the mod | ✅ Yes |
| Nobody has the mod | ❌ No drop |

### ⚙️ Configuration

Edit at `BepInEx/config/com.hiarlyscripter.huntsmanloot.cfg` or use **REPOConfig** in-game.

| Key | Default | Description |
|---|---|---|
| `DropChance` | `100` | Drop chance (%) — 1 to 100 |
| `BerserkerOnly` | `false` | `true` = only drops from berserk Huntsmans |
| `MasterClientOnly` | `true` | ⚠️ Keep `true` — prevents duplicate drops |
| `RandomizeAmmo` | `true` | `true` = random ammo between 1 and max |

### 📦 Dependencies

**Required**

| Mod | Version | Link |
|---|---|---|
| BepInExPack | `5.4.23.5` | [Thunderstore](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/) |

**Optional**

| Mod | Purpose |
|---|---|
| [REPOConfig](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/) | Edit configs in-game without opening the file |
| [BerserkerEnemies](https://thunderstore.io/c/repo/p/Zehs/BerserkerEnemies/) | Required if `BerserkerOnly = true` |

### 🛠️ Installation

**Via r2modman (recommended)**
1. Install **BepInExPack**
2. Search and install **Huntsman Loot**
3. *(Optional)* Install **REPOConfig**
4. Click **Start modded**

**Via manual**
1. Install BepInExPack first
2. Copy `plugins/HiarlyScripter-HuntsmanLoot/` into `BepInEx/plugins/`
3. Start the game

### ❓ Common issues

| Problem | Solution |
|---|---|
| Weapon doesn't drop | Make sure the **host** has the mod |
| Weapon drops multiple times | Keep `MasterClientOnly = true` (default) |
| Green bar on weapon | Update to v1.1.3 |
| Wrong ammo shown | Check the `RandomizeAmmo` setting |

### 📋 Technical notes

- Uses the game's native **Hunter Gun** visual at runtime reference — does not redistribute extracted game assets.
- Does not copy or modify assets from reference mods.
- Functional base is the game's native shotgun (`item_gun_shotgun`) with the Hunter Gun visual overlaid.
- Being a long weapon using the native shotgun's functional base, there may be minor visual clipping against walls or corners. Does not affect gameplay.

---

*Mod por **[HiarlyScripter](https://discord.com/users/hiarly_ferreira)** — Testado com / Tested with R.E.P.O. Build `23250495` · BepInEx `5.4.23.5`*
