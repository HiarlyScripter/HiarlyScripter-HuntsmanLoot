# Changelog — Huntsman Loot

## v1.1.3 — 2026-06-07

### Visual real da Huntsman Rifle
- A arma dropada agora usa a mesh nativa `Hunter Gun` do proprio jogo via referencia runtime — sem empacotar mesh extraida do jogo, sem copiar asset de mod de referencia.
- Visual aprovado: `localPosition=(0,0,0)`, `localEuler=(-8,180,90)`, `localScale=(0.75,0.75,0.75)`.
- Cor e material normalizados via native-clone do prefab nativo.
- Nome do item agora aparece como **Huntsman Rifle** no inventario.

### Icone corrigido
- Icone do inventario corrigido com render real via `SemiIconMaker`.
- Enquadramento da camera de icone ajustado para mostrar a arma inteira dentro do quadrado, em vez de apenas o cano.
- Orientacao corrigida invertendo o vetor `upWorld` da camera, sem alterar zoom ou enquadramento.
- Removida rota de sprite procedural/desenhado em runtime.

### Ammo real corrigida
- `ItemGun.numberOfBullets` nao era ammo — e quantidade de pellets/projeteis por disparo. Corrigido.
- Ammo real e `ItemBattery.batteryLifeInt`; maximo real e `ItemBattery.batteryBars`.
- Correcao aplicada via marker + postfix em `ItemBattery.Start` para evitar reset para cheio.

### Collision envelope
- BoxCollider unico baseado em `mesh.bounds` do visual.
- Sem MeshCollider, sem Rigidbody novo, sem trigger.
- `attachedRigidbody` confirmado presente.

### Qualidade
- Logs de diagnostico limpos; `EnableDebugLogging` desativado no perfil de teste.
- Build Release: 0 erros / 0 warnings.

### Limitacao conhecida
- Por ser uma arma longa usando a base funcional da shotgun nativa, pode haver clipping visual leve em parede/quina. Nao afeta gameplay.

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
- Lançamento inicial.
- Drop da espingarda ao matar o Huntsman.
- Chance de drop configurável (1–100%).
- Modo berserk: drop exclusivo de Huntsmans berserk (requer BerserkerEnemies).
- Suporte a multiplayer via PhotonNetwork.
- Compatível com REPO v0.4.x.
