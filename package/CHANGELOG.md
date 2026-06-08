# Changelog — Huntsman Loot

---

## v1.1.3 — 2026-06-07
**Compatibilidade / Compatibility:** R.E.P.O. Build `23250495` · BepInEx `5.4.23.5`

### 🇧🇷 PT-BR

#### Visual real da Huntsman Rifle
- A arma dropada agora usa a mesh nativa `Hunter Gun` do próprio jogo por referência runtime — sem empacotar asset extraído, sem copiar asset de mod de referência
- Nome exibido como **Huntsman Rifle** no inventário
- Visual aprovado com cor e material nativos; transform calibrado para orientação correta
- Ícone do inventário corrigido via render real da arma pelo sistema nativo (sem sprite procedural)

#### Corrigido
- Ammo corrigida: o valor real é `ItemBattery.batteryLifeInt` / `batteryBars`, não `numberOfBullets` (que é pellets por disparo)
- Collision envelope: BoxCollider único baseado nos bounds reais da mesh, sem MeshCollider
- Barra verde suprimida corretamente no item dropado
- Logs de diagnóstico limpos; `EnableDebugLogging` desativado no build de release

#### Limitação conhecida
- Por usar a base funcional da shotgun nativa, pode haver clipping visual leve em paredes/cantos; não afeta o gameplay

---

### 🇺🇸 English

#### Real Huntsman Rifle visual
- Dropped weapon now uses the game's native `Hunter Gun` mesh via runtime reference — no extracted assets bundled, no reference mod assets copied
- Inventory name now shows as **Huntsman Rifle**
- Native color and material; transform calibrated for correct drop orientation
- Inventory icon fixed via native game render system (no procedural sprite)

#### Fixed
- Ammo fixed: real ammo value is `ItemBattery.batteryLifeInt` / `batteryBars`, not `numberOfBullets` (which is pellets per shot)
- Collision envelope: single BoxCollider based on real mesh bounds, no MeshCollider
- Green battery bar correctly suppressed on dropped item
- Diagnostic logs cleaned; `EnableDebugLogging` disabled in release build

#### Known limitation
- Being a long weapon using the native shotgun's functional base, minor visual clipping may occur at walls/corners; does not affect gameplay

---

## v1.1.1 — 2026-05-19
**Compatibilidade:** R.E.P.O. Build `23250495` · BepInEx `5.4.23.5`

### Corrigido
- Removida ofuscação do binário para conformidade com as políticas do Thunderstore

---

## v1.1.0 — 2026-05-19
**Compatibilidade:** R.E.P.O. Build `23250495` · BepInEx `5.4.23.5`

### Adicionado
- Configuração `MasterClientOnly` — controla qual jogador processa o drop em multiplayer (recomendado: `true`)

### Alterado
- A espingarda dropada agora usa o item nativo do jogo — sem dependência de mods externos

### Removido
- Dependência obrigatória do mod `DougHRito-HunterGun`

### Corrigido
- Adicionado fallback via hook `ShopManager.GetAllItemsFromStatsManager` para garantir que o item seja registrado corretamente

---

## v1.0.0
**Compatibilidade:** R.E.P.O. Build `23250495` · BepInEx `5.4.23.5`

### Adicionado
- Lançamento inicial
- Drop da espingarda ao eliminar o Huntsman
- Chance de drop configurável de 1% a 100%
- Suporte a multiplayer via Photon — drop visível para todos os jogadores na sala
- Modo berserk opcional — drop exclusivo de Huntsmans em modo berserk (requer `BerserkerEnemies`)
