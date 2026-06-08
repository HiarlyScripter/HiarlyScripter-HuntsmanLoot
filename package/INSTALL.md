# Guia de Instalação — Huntsman Loot / Installation Guide — Huntsman Loot

> ✅ Testado com / Tested with **R.E.P.O. Build `23250495`** · **BepInExPack `5.4.23.5`** · REPOConfig `1.2.6` *(opcional / optional)*
>
> ⚠️ Se o jogo atualizar e o mod parar de funcionar, verifique se há uma versão nova compatível.
> If the game updates and the mod stops working, check for a new compatible version.

---

## 🇧🇷 PT-BR

### Dependências

**Obrigatórias (instale antes do mod):**

| Mod | Versão testada | Link |
|---|---|---|
| BepInExPack | `5.4.23.5` | [Thunderstore](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/) |

> Nenhuma outra dependência obrigatória. O mod usa apenas assets nativos do jogo.

**Opcionais:**

| Mod | Versão testada | Link | Para que serve |
|---|---|---|---|
| REPOConfig | `1.2.6` | [Thunderstore](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/) | Editar configs dentro do jogo |
| BerserkerEnemies | — | [Thunderstore](https://thunderstore.io/c/repo/p/Zehs/BerserkerEnemies/) | Necessário apenas se `BerserkerOnly = true` |

### Como instalar

#### Opção 1 — r2modman (recomendado)
1. Instale o **BepInExPack**
2. Procure e instale o **Huntsman Loot**
3. *(Opcional)* Instale o **REPOConfig** para editar configs in-game
4. Clique em **Start modded** — pronto!

#### Opção 2 — Manual
1. Instale o **BepInExPack** primeiro
2. Copie a pasta `plugins/HiarlyScripter-HuntsmanLoot/` para `BepInEx/plugins/`
3. O caminho final da DLL deve ser: `BepInEx/plugins/HiarlyScripter-HuntsmanLoot/HuntsmanLoot.dll`
4. Inicie o jogo

### Quem precisa instalar?

> **Somente o host (dono da sala)** precisa ter o mod instalado.
> A Huntsman Rifle aparece para **todos os jogadores** via rede, mesmo sem o mod.

| Cenário | Resultado |
|---|---|
| ✅ Só o host tem o mod | Funciona — host spawna para todos |
| ✅ Todos têm o mod | Funciona |
| ❌ Ninguém tem o mod | Huntsman não dropa nada |

### Configurações disponíveis

Edite em `BepInEx/config/com.hiarlyscripter.huntsmanloot.cfg` ou use o **REPOConfig** in-game.

| Chave | Padrão | O que faz |
|---|---|---|
| `DropChance` | `100` | Chance (%) de a arma cair — 1 a 100 |
| `BerserkerOnly` | `false` | `true` = só dropa de Huntsmans berserk |
| `MasterClientOnly` | `true` | ⚠️ Manter `true` — evita drops duplicados em multiplayer |
| `RandomizeAmmo` | `true` | `true` = munição aleatória; `false` = sempre cheia |

### Como verificar se o mod carregou

Abra `BepInEx/LogOutput.log` e procure:
```
[Info   :Huntsman Loot] [HuntsmanLoot] v1.1.3 carregado. Patch Harmony aplicado.
```

### Como desinstalar

**Via r2modman:** desative ou remova o mod. **Manual:** delete `BepInEx/plugins/HiarlyScripter-HuntsmanLoot/`.

### Problemas comuns

| Problema | Solução |
|---|---|
| A arma não dropa | Certifique-se de que o **host** tem o mod instalado e habilitado |
| A arma dropa várias vezes | Mantenha `MasterClientOnly = true` |
| Barra verde na arma (versão antiga) | Atualize para v1.1.3 |
| Drop só de Huntsman berserk | Instale BerserkerEnemies e ative `BerserkerOnly = true` |

---

## 🇺🇸 English

### Dependencies

**Required (install before the mod):**

| Mod | Tested version | Link |
|---|---|---|
| BepInExPack | `5.4.23.5` | [Thunderstore](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/) |

> No other required dependencies. The mod uses only the game's native assets.

**Optional:**

| Mod | Tested version | Link | Purpose |
|---|---|---|---|
| REPOConfig | `1.2.6` | [Thunderstore](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/) | Edit configs in-game |
| BerserkerEnemies | — | [Thunderstore](https://thunderstore.io/c/repo/p/Zehs/BerserkerEnemies/) | Required only if `BerserkerOnly = true` |

### How to install

#### Option 1 — r2modman (recommended)
1. Install **BepInExPack**
2. Search and install **Huntsman Loot**
3. *(Optional)* Install **REPOConfig** for in-game config editing
4. Click **Start modded** — done!

#### Option 2 — Manual
1. Install **BepInExPack** first
2. Copy `plugins/HiarlyScripter-HuntsmanLoot/` to `BepInEx/plugins/`
3. Final DLL path: `BepInEx/plugins/HiarlyScripter-HuntsmanLoot/HuntsmanLoot.dll`
4. Start the game

### Who needs to install it?

> **Only the host (room owner)** needs the mod installed.
> The Huntsman Rifle appears for **all players** via the game's network, even without the mod.

| Scenario | Result |
|---|---|
| ✅ Only host has the mod | Works — host spawns for everyone |
| ✅ Everyone has the mod | Works |
| ❌ Nobody has the mod | Huntsman drops nothing |

### Available configuration

Edit in `BepInEx/config/com.hiarlyscripter.huntsmanloot.cfg` or use **REPOConfig** in-game.

| Key | Default | What it does |
|---|---|---|
| `DropChance` | `100` | Drop chance (%) — 1 to 100 |
| `BerserkerOnly` | `false` | `true` = only drops from berserk Huntsmans |
| `MasterClientOnly` | `true` | ⚠️ Keep `true` — prevents duplicate drops |
| `RandomizeAmmo` | `true` | `true` = random ammo; `false` = always full |

### How to verify the mod loaded

Open `BepInEx/LogOutput.log` and look for:
```
[Info   :Huntsman Loot] [HuntsmanLoot] v1.1.3 carregado. Patch Harmony aplicado.
```

### How to uninstall

**Via r2modman:** disable or remove the mod. **Manual:** delete `BepInEx/plugins/HiarlyScripter-HuntsmanLoot/`.

### Common issues

| Problem | Solution |
|---|---|
| Weapon doesn't drop | Make sure the **host** has the mod installed and enabled |
| Weapon drops multiple times | Keep `MasterClientOnly = true` |
| Green bar on weapon (old version) | Update to v1.1.3 |
| Berserk-only drops | Install BerserkerEnemies and enable `BerserkerOnly = true` |

---

*Mod by **HiarlyScripter** · [Thunderstore](https://thunderstore.io/c/repo/p/HiarlyScripter/HuntsmanLoot/) · [Discord](https://discord.com/users/hiarly_ferreira)*
