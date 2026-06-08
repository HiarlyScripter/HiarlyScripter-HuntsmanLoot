# Changelog — Huntsman Loot

## v1.1.3 — 2026-06-07

### 🇧🇷 PT-BR

#### Visual real da Huntsman Rifle
- Arma dropada usa a mesh nativa `Hunter Gun` do jogo por referência runtime — sem empacotar asset extraído, sem copiar asset de mod de referência
- Nome: **Huntsman Rifle** no inventário
- Cor e material nativos; transform calibrado: `localEuler=(-8,180,90)`, `localScale=(0.75,0.75,0.75)`
- Ícone corrigido via render real por `SemiIconMaker` (sem sprite procedural)

#### Ammo real corrigida
- `ItemGun.numberOfBullets` é pellets por disparo, não ammo — corrigido
- Ammo real: `ItemBattery.batteryLifeInt` / `batteryBars`
- Correção via marker + postfix em `ItemBattery.Start`

#### Collision
- BoxCollider único baseado em `mesh.bounds`; sem MeshCollider, sem trigger

#### Qualidade
- Logs limpos; build 0 erros / 0 warnings

#### Limitação conhecida
- Clipping visual leve possível em paredes/cantos — não afeta gameplay

---

### 🇺🇸 English

#### Real Huntsman Rifle visual
- Dropped weapon uses game's native `Hunter Gun` mesh via runtime reference — no extracted assets bundled, no reference mod assets copied
- Inventory name: **Huntsman Rifle**
- Native color and material; transform `localEuler=(-8,180,90)`, `localScale=(0.75,0.75,0.75)`
- Inventory icon fixed via native `SemiIconMaker` render (no procedural sprite)

#### Real ammo fixed
- `ItemGun.numberOfBullets` is pellets per shot, not ammo — fixed
- Real ammo: `ItemBattery.batteryLifeInt` / `batteryBars`
- Fixed via marker + postfix on `ItemBattery.Start`

#### Collision
- Single BoxCollider from `mesh.bounds`; no MeshCollider, no trigger

#### Quality
- Clean logs; build 0 errors / 0 warnings

#### Known limitation
- Minor visual clipping possible at walls/corners — does not affect gameplay

---

## v1.1.1 (2026-05-19)
- Corrigida barra verde (bateria) que aparecia sobre a arma no chão e nunca depleta
- A barra verde sobrepunha a barra amarela de munição, ocultando a animação de recarga com cristal
- Correção via `ItemEquippable.suppressBatteryUI` — afeta todos os clientes em multiplayer

## v1.1.0 (2026-05-19)
- Removida dependência do mod DougHRito-HunterGun
- A espingarda dropada agora usa o item nativo do jogo (`item_gun_shotgun`) — zero dependências extras
- Adicionado fallback via hook `ShopManager.GetAllItemsFromStatsManager`
- Adicionada configuração `MasterClientOnly` para controle de drops em multiplayer
- Dependência do BepInExPack atualizada para `5.4.2305`

## v1.0.0
- Lançamento inicial / Initial release
- Drop da espingarda ao matar o Huntsman / Shotgun drop on Huntsman death
- Chance de drop configurável (1–100%) / Configurable drop chance (1–100%)
- Modo berserk: drop exclusivo de Huntsmans berserk / Berserk mode: berserk-only drop
- Suporte a multiplayer via PhotonNetwork / Multiplayer support via PhotonNetwork
