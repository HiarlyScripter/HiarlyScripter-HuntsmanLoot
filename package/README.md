# 🔫 Huntsman Loot

[![Versão](https://img.shields.io/badge/versão-1.1.3-brightgreen?style=flat-square)](https://thunderstore.io/c/repo/p/HiarlyScripter/HuntsmanLoot/)
[![R.E.P.O.](https://img.shields.io/badge/R.E.P.O.-Build%2023250495-blue?style=flat-square)](https://store.steampowered.com/app/3241660/REPO/)
[![BepInEx](https://img.shields.io/badge/BepInEx-5.4.23.5-yellow?style=flat-square)](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/)
[![Multiplayer](https://img.shields.io/badge/Multiplayer-Host%20Only-9b59b6?style=flat-square)]()
[![REPOConfig](https://img.shields.io/badge/REPOConfig-compatível-orange?style=flat-square)](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/)

> O Huntsman agora **paga um preço** quando você o elimina — ele larga a própria arma no chão!

---

## ✨ O que faz

O **Huntsman Loot** faz o Huntsman dropar a **Huntsman Rifle** ao ser eliminado. A arma usa a mesh nativa `Hunter Gun` do próprio jogo via referência runtime — **sem empacotar assets, sem dependência de outros mods**.

---

## 🎯 Funcionalidades

- 🔫 **Drop ao morrer** — a Huntsman Rifle cai exatamente onde o Huntsman morreu
- 🎨 **Visual real** — a arma usa a mesh nativa do Hunter Gun do jogo, com cor e material nativos
- 🏷️ **Nome correto** — aparece como **Huntsman Rifle** no inventário, com ícone real gerado pelo jogo
- 🎲 **Chance configurável** — de **1%** (raridade extrema) a **100%** (sempre dropa)
- 💀 **Modo Berserk** — drop exclusivo de Huntsmans em modo berserk *(requer BerserkerEnemies, opcional)*
- 👑 **Processado pelo host** — só o host processa o drop; a arma aparece para **todos na sala**
- 🔧 **Zero dependências extras** — usa apenas assets nativos do jogo
- 🎰 **Munição aleatória** — a arma dropa com quantidade de balas aleatória entre 1 e o máximo; desative nas configs para sempre cair cheia
- 🟢 **Sem barra verde falsa** — a barra de bateria desnecessária foi removida; a barra de munição (amarela) funciona normalmente
- ⚙️ **REPOConfig** — todos os configs editáveis dentro do jogo *(opcional)*

---

## 👥 Quem precisa instalar?

> **Somente o host (dono da sala)** precisa ter o mod. A espingarda aparece para **todos os jogadores**, mesmo os que não têm o mod instalado.

| Cenário | Drop funciona? |
|---|---|
| ✅ Só o host tem o mod | Sim — host spawna para todos via rede do jogo |
| ✅ Todos têm o mod | Sim |
| ❌ Ninguém tem o mod | Não |

---

## ⚙️ Configurações

Edite em `BepInEx/config/com.hiarlyscripter.huntsmanloot.cfg` ou use o **REPOConfig** in-game.

| Seção | Chave | Padrão | Descrição |
|---|---|---|---|
| `Drop` | `DropChance` | `100` | Chance (%) de a espingarda cair — aceita valores de 1 a 100 |
| `Drop` | `BerserkerOnly` | `false` | `true` = espingarda só dropa de Huntsmans no modo berserk |
| `Drop` | `MasterClientOnly` | `true` | ⚠️ Recomendado manter `true` — evita múltiplos drops simultâneos em multiplayer |
| `Drop` | `RandomizeAmmo` | `true` | `true` = espingarda cai com balas aleatórias (1 até o máximo); `false` = sempre cai cheia |

---

## 📦 Dependências

### Obrigatórias
| Mod | Versão testada | Link |
|---|---|---|
| BepInExPack | `5.4.23.5` | [Thunderstore](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/) |

### Opcionais
| Mod | Versão testada | Link | Para que serve |
|---|---|---|---|
| REPOConfig | `1.2.6` | [Thunderstore](https://thunderstore.io/c/repo/p/nickklmao/REPOConfig/) | Editar configs dentro do jogo sem abrir arquivos |
| BerserkerEnemies | — | [Thunderstore](https://thunderstore.io/c/repo/p/Zehs/BerserkerEnemies/) | Necessário **apenas** se `BerserkerOnly = true` |

---

## 🖼️ Screenshots

*Screenshots em breve. Quer contribuir com prints do mod em ação? Entre em contato!*

<!-- SCREENSHOTS_PLACEHOLDER -->

---

## 🛠️ Instalação rápida

**Via r2modman (recomendado)**
1. Instale o **BepInExPack**
2. Procure e instale o **Huntsman Loot**
3. *(Opcional)* Instale o **REPOConfig**
4. Clique em **Start modded** — pronto!

**Via manual**
1. Instale o BepInExPack primeiro
2. Copie `plugins/HiarlyScripter-HuntsmanLoot/` para `BepInEx/plugins/`
3. Inicie o jogo — o arquivo de config é gerado automaticamente

---

## ❓ Problemas comuns

| Problema | Solução |
|---|---|
| A arma não cai quando o Huntsman morre | Certifique-se de que o **host** tem o mod instalado |
| A arma aparece várias vezes de uma vez | Mantenha `MasterClientOnly = true` (padrão) |
| Barra verde aparece sobre a arma (versão antiga) | Atualize para v1.1.1 — o bug foi corrigido |
| Quero drop só de Huntsman berserk | Instale **BerserkerEnemies** e ative `BerserkerOnly = true` |

---

*Mod criado por **[HiarlyScripter](https://discord.com/users/hiarly_ferreira)** — Testado com R.E.P.O. Build `23250495` · BepInEx `5.4.23.5`*
