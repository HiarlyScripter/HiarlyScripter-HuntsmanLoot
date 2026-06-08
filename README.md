# 🔫 Huntsman Loot

[![Thunderstore](https://img.shields.io/badge/Thunderstore-v1.1.3-brightgreen?style=flat-square&logo=thunderstore)](https://thunderstore.io/c/repo/p/HiarlyScripter/HuntsmanLoot/)
[![R.E.P.O.](https://img.shields.io/badge/R.E.P.O.-Build%2023250495-blue?style=flat-square)](https://store.steampowered.com/app/3241660/REPO/)
[![BepInEx](https://img.shields.io/badge/BepInEx-5.4.23.5-yellow?style=flat-square)](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/)
[![Licença](https://img.shields.io/badge/licença-crédito%20obrigatório-red?style=flat-square)](LICENSE)

> 🇧🇷 O Huntsman agora **paga um preço** quando você o elimina — ele larga a própria arma no chão.
> 🇺🇸 The Huntsman now **pays the price** when you take him down — his own weapon drops to the floor.

---

## 🇧🇷 Sobre o mod

O **Huntsman Loot** faz o Huntsman dropar a **Huntsman Rifle** ao ser eliminado. A arma usa o visual nativo **Hunter Gun** do próprio jogo via referência em runtime — sem empacotar assets extraídos, sem copiar assets de mods de referência, sem dependências externas além do BepInEx.

## 🇺🇸 About

**Huntsman Loot** makes the Huntsman drop the **Huntsman Rifle** when killed. The weapon uses the game's native **Hunter Gun** visual at runtime — no extracted assets bundled, no third-party mod assets copied, no external dependencies beyond BepInEx.

---

## ✨ Funcionalidades / Features

- 🔫 **Drop ao morrer / Drop on death** — a Huntsman Rifle cai onde o Huntsman morreu / drops where the Huntsman died
- 🎨 **Visual real / Real visual** — mesh nativa Hunter Gun do jogo / game's native Hunter Gun mesh
- 🏷️ **Nome correto / Correct name** — **Huntsman Rifle** no inventário / in inventory
- 🎲 **Chance configurável / Configurable chance** — 1% a 100% / 1% to 100%
- 🎰 **Munição aleatória / Random ammo** — configurável / configurable
- 💀 **Modo Berserk / Berserk Mode** — *(requer BerserkerEnemies, opcional / requires BerserkerEnemies, optional)*
- 👑 **Só o host / Host only** — spawn via rede para todos / network-spawned for everyone
- 🔧 **Zero dependências / Zero dependencies** — apenas BepInEx / only BepInEx
- ⚙️ **REPOConfig** — configs editáveis in-game *(opcional / optional)*

---

## ⚙️ Configurações / Configuration

| Chave / Key | Padrão / Default | Descrição / Description |
|---|---|---|
| `DropChance` | `100` | Chance (%) — 1 a/to 100 |
| `BerserkerOnly` | `false` | Só berserk / Berserk-only drop |
| `MasterClientOnly` | `true` | ⚠️ Manter true / Keep true |
| `RandomizeAmmo` | `true` | Munição aleatória / Random ammo |

---

## 📋 Notas técnicas / Technical notes

- Usa o visual nativo Hunter Gun do jogo por referência runtime — não redistribui assets extraídos.
- Uses the game's native Hunter Gun visual at runtime reference — does not redistribute extracted game assets.
- Não copia assets de mods de referência / Does not copy assets from reference mods.

## ⚠️ Limitações / Known limitations

- Clipping visual leve possível em paredes/cantos por ser arma longa. Não afeta gameplay.
- Minor visual clipping possible against walls/corners. Does not affect gameplay.

---

## 📄 Licença / License

[Licença customizada / Custom license](LICENSE) — uso e estudo permitidos; **crédito ao autor obrigatório** em qualquer redistribuição / use and study permitted; **author credit required** on any redistribution.

---

## 🛠️ Build from source

```bash
dotnet build src/HuntsmanLoot.csproj --configuration Release
```

---

*Mod por / by **[HiarlyScripter](https://discord.com/users/hiarly_ferreira)** · Thunderstore: [HuntsmanLoot](https://thunderstore.io/c/repo/p/HiarlyScripter/HuntsmanLoot/)*
