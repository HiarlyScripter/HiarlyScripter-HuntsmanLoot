# Checkpoint: Native Hunter Gun Working State

## Git

- Branch atual: `codex/huntsman-authorial-mesh-runtime`
- Commit base anterior: `6033dca checkpoint: failed runtime capture before authorial mesh route`
- Commit de checkpoint planejado: `checkpoint: native Hunter Gun visual working state`

## Estado funcional validado

- A mesh nativa `Hunter Gun` do jogo aparece no drop do Huntsman.
- A arma dropa visivel no chao.
- A cor voltou para aparencia normal/nativa usando material `native-clone`.
- O tamanho no chao esta bom para teste real.
- Pickup funciona.
- Tiro funciona.
- A arma atira nos inimigos.
- O icone voltou para o icone nativo da shotgun temporariamente.
- O nome ainda aparece `SHOTGUN`, tratado como limitacao temporaria.

## Arquitetura aprovada

- Base funcional: shotgun nativa do jogo (`Items/Item Gun Shotgun`).
- Visual: mesh nativa `Hunter Gun` carregada em runtime por referencia de recursos do jogo.
- O mod nao empacota a mesh nativa do jogo.
- O mod nao copia asset do mod de referencia.
- O mod nao depende de AssetBundle, Unity, REPOLib ou prefab custom.

## Material

- Modo atual: `native-clone`.
- O material nativo base e clonado por instancia, preferindo `Enemy Hunter` sem `(Instance)`.
- O clone preserva shader/texturas nativas quando disponiveis.
- Emissao e tint vermelho forte sao neutralizados apenas no clone por instancia.
- Se o material nativo nao estiver disponivel, o fallback e `fallback-neutral`.

## Transform visual atual

- `localPosition = (0, 0, 0)`
- `localEuler = (-8, 180, 90)`
- `localScale = (0.75, 0.75, 0.75)`

## Ammo

- `RandomizeAmmo` continua como config publica.
- A logica atual usa `batteryBars` como capacidade quando disponivel.
- Distribuicao atual para max 5:
  - 1 bala: 25%
  - 2 balas: 25%
  - 3 balas: 22%
  - 4 balas: 18%
  - 5 balas: 10%
- Pendencia: auditar por que em testes recentes a arma parece vir cheia com frequencia. Log observado: `Ammo randomized: current=6 max=5 final=5`.

## Nome e icone

- Nome/UI ainda pode aparecer como `SHOTGUN`.
- O override runtime de icone foi desativado.
- O icone nativo da shotgun fica temporariamente mantido.
- Pendencia futura: se mexer no icone, enquadrar melhor a parte traseira/curvada da Hunter Gun, sem sprite inventado ruim.

## Deploy de teste

- Perfil: `REPO - Test`
- DLL:
  `C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test\BepInEx\plugins\HiarlyScripter-HuntsmanLoot\HuntsmanLoot.dll`
- `mods.yml`: `HiarlyScripter-HuntsmanLoot enabled: true`

## Pendencias futuras

1. Auditar a distribuicao real de ammo nos logs e em multiplos drops.
2. Refinar fisica/colisao para reduzir a chance da arma comprida atravessar parcialmente paredes/objetos no chao.
3. Melhorar o icone do inventario sem usar sprite runtime ruim e sem mutar global.
4. Resolver nome/UI `SHOTGUN` apenas se houver caminho seguro por instancia.
5. Manter logs enxutos; qualquer auditoria extensa deve ficar atras de `EnableDebugLogging`.

## Observacoes de seguranca

- Nao houve push.
- Nao houve PR.
- Nao houve publicacao.
- Nao houve ZIP.
- Nao houve alteracao em Thunderstore.
- `tools/` permanece fora do commit por conter artefatos/experimentos.
- `src/HuntsmanRifleMeshData.cs` permanece fora do commit porque nao faz parte da rota final.
