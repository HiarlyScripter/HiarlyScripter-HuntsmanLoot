# Changelog — Huntsman Loot

---

## v1.1.3 — 2026-06-07
**Compatibilidade:** R.E.P.O. Build `23250495` · BepInEx `5.4.23.5`

### Visual real da Huntsman Rifle
- A arma dropada agora usa a mesh nativa `Hunter Gun` do próprio jogo — sem empacotar asset extraído do jogo
- Nome do item exibido como **Huntsman Rifle** no inventário
- Visual aprovado com cor e material nativos; transform calibrado para orientação correta no drop
- Ícone do inventário corrigido com render real da arma via sistema nativo do jogo (sem sprite procedural)

### Corrigido
- Ammo corrigida: o valor real de munição é `ItemBattery.batteryLifeInt`/`batteryBars`, não `numberOfBullets` (que é pellets por disparo)
- Collision envelope corrigido: BoxCollider único baseado nos bounds reais da mesh, sem MeshCollider
- Barra de bateria verde suprimida corretamente no item dropado

### Limitação conhecida
- Por usar a base funcional da shotgun nativa, pode haver clipping visual leve em parede/canto; não afeta o gameplay

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
