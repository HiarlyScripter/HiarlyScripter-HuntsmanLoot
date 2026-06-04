# DEVLOG — Huntsman Loot (HiarlyScripter)

## Contexto

Este mod foi desenvolvido como substituto 100% original ao `DougHRito-HunterDropsGun` v1.0.7, que parou de funcionar com REPO v0.4.x. O código foi escrito do zero, sem reaproveitamento de código-fonte do mod original.

---

## O que estava quebrado no mod de referência

### Problema 1 — Detecção de morte pelo timer (`DespawnedTimer > 310f`)

**O que o mod original fazia:**
Verificava se o Huntsman havia morrido testando se `DespawnedTimer > 310f`. A lógica era indireta: quando o inimigo morria, o código original do jogo multiplicava o timer por 3, fazendo-o ultrapassar 310.

```csharp
// Código antigo (frágil)
if (!(___DespawnedTimer > 310f)) return;
```

**Por que quebrou:**
Em REPO v0.4.x, os multiplicadores e limites do `DespawnedTimer` foram ajustados. A multiplicação ainda acontece em caso de morte, mas os valores resultantes deixaram de ultrapassar consistentemente o limiar de 310. Em casos de morte legítima, o mod simplesmente não disparava o drop.

**Como foi resolvido:**
Verificação direta da saúde do inimigo via reflexão:

```csharp
// Código novo (direto e confiável)
bool hasHealth = (bool)_hasHealthField.GetValue(enemy);
EnemyHealth health = (EnemyHealth)_healthField.GetValue(enemy);
int hp = (int)_hpCurrentField.GetValue(health);
if (hp > 0) return; // despawn normal, não morte
```

Os campos `Enemy.HasHealth`, `Enemy.Health` e `EnemyHealth.healthCurrent` são `internal` no assembly do jogo, portanto acessados via `AccessTools.Field` com cache estático para eficiência.

---

### Problema 2 — Todos os arquivos desabilitados pelo r2modman (extensão `.old`)

**O que aconteceu:**
O r2modman detectou uma incompatibilidade e desabilitou todos os arquivos do mod adicionando `.old` ao final de cada nome. Isso indica que o mod causava exceções não tratadas no BepInEx ao inicializar.

**Como foi resolvido:**
Com a verificação de morte corrigida e os campos internos acessados com segurança, o mod carrega sem exceções.

---

### Problema 3 — Campo `Enemy` em `EnemyParent` é privado/interno

**O que o mod original fazia:**
Acessava o campo `Enemy` dentro de `EnemyParent` via `AccessTools.Field` chamado a cada patch — sem cache.

**Como foi resolvido:**
O campo `_enemyField` é cacheado como `static readonly` na inicialização do plugin, evitando lookups repetidos de reflexão durante o jogo.

---

## Resumo técnico das mudanças

| Componente | Antes (frágil) | Depois (confiável) |
|---|---|---|
| Detecção de morte | `DespawnedTimer > 310f` | `EnemyHealth.healthCurrent <= 0` |
| Acesso a campos internos | `AccessTools.Field` inline | `static readonly FieldInfo` cacheados |
| Compatibilidade multiplayer | `PhotonNetwork.InstantiateRoomObject` | Mantido (ainda funciona em v4.x) |

---

## v1.1.0 — Remoção de dependência externa (DougHRito-HunterGun)

**Motivação:** O mod v1.0.0 dependia do `DougHRito-HunterGun` para o modelo da espingarda. Isso tornava o mod dependente de um terceiro para funcionar — se o HunterGun fosse descontinuado ou quebrado, o HuntsmanLoot pararia de funcionar junto.

**Descoberta:** O jogo R.E.P.O. possui nativamente o item `item_gun_shotgun` registrado no `StatsManager`, com prefab próprio. Este é o mesmo modelo visual de espingarda usado pelo Huntsman e pelo shopkeeper no jogo base.

**O que mudou:**

| Componente | Antes (v1.0.0) | Depois (v1.1.0) |
|---|---|---|
| Arma dropada | `"Gun Hunter"` (DougHRito) | `item_gun_shotgun` (nativo do jogo) |
| Dependência | BepInEx + HunterGun | Apenas BepInEx |
| Busca do prefab | `Resources.FindObjectsOfTypeAll` por nome | `StatsManager.itemDictionary` via reflexão |
| Fallback | Nenhum | `ShopManager.GetAllItemsFromStatsManager` (hook existente) |
| Spawn singleplayer | `Object.Instantiate(RiflePrefab, ...)` | `Object.Instantiate(item.prefab, ...)` |
| Spawn multiplayer | `PhotonNetwork.InstantiateRoomObject("Items/Gun Hunter", ...)` | `PhotonNetwork.InstantiateRoomObject(item.prefabName, ...)` |
| Config ShopAvailable | Filtrava `"Gun Hunter"` | Removida — item nativo aparece normalmente na loja |

**Por que é 100% do HiarlyScripter:** A espingarda pertence ao jogo base, não a nenhum outro modder. O código de busca, detecção de morte e spawn é 100% original.

---

## v1.1.1 — 2026-05-19 — Correção de barra verde (ItemBattery)

**Causa:** A espingarda spawnada via `Resources.Load` / `PhotonNetwork.InstantiateRoomObject` nasce com `batteryLife = 1.0` (100% cheio) no componente `ItemEquippable`. Essa barra verde fica visível sobre o item no chão e nunca depleta, pois a espingarda não usa o sistema de bateria de forma alguma. A barra sobrepunha visualmente a barra amarela de munição, ocultando a animação de recarga com cristal.

**Correção:** Patch Harmony `Postfix` em `ItemEquippable.Start()`. Quando o item pai é `item_gun_shotgun`, o campo `suppressBatteryUI` é setado para `true` via reflexão, ocultando a barra para todos os clientes (host e guests) sem afetar a mecânica de munição.

**Campo utilizado:** `ItemEquippable.suppressBatteryUI` (detectado via strings no `Assembly-CSharp.dll`).

**Escopo:** Afeta toda instância da espingarda — tanto dropada pelo Huntsman quanto comprada na loja. Como a barra nunca depletura de qualquer forma, sumir com ela é correto em ambos os casos.

---

## v1.1.2 — 2026-05-20 — Remoção da abordagem REPOLib (drop não funcionava)

**Causa:** A versão anterior (v1.2.0, não publicada) substituiu a abordagem direta de spawn por uma cadeia complexa usando REPOLib. Essa cadeia falhava silenciosamente em múltiplos pontos:

1. `[BepInDependency("REPOLib")]` usava GUID genérico — potencialmente não reconhecido pelo BepInEx.
2. `REPOLib.Modules.Items.RegisterItem(ItemAttributes)` — assinatura incompatível com a API real do REPOLib (que espera `Item`, não `ItemAttributes`) → `ItemRegistered` nunca virava `true`.
3. O fallback nativo dependia de `_resourcePathField?.GetValue(nativeItem.prefab)` que retornava `null` silenciosamente se o campo `PrefabRef.resourcePath` não existisse na versão do jogo → `return` sem log, sem drop.
4. `NativeShotgunItem` dependia de `WaitAndSetupItem` (coroutine com loop de 2s) ou da loja abrir — race condition: Huntsman podia morrer antes de qualquer um dos dois ocorrer.

**Solução aplicada:**
- Removido REPOLib por completo (usando, BepInDependency, código).
- `DropRifle()` agora usa path hard-coded `"Items/Item Gun Shotgun"` confirmado como correto em v1.1.3.
- Singleplayer: `Resources.Load<GameObject>(SHOTGUN_PATH)` + `Object.Instantiate`.
- Multiplayer: `PhotonNetwork.InstantiateRoomObject(SHOTGUN_PATH, ...)`.
- `EnemyParent.Enemy` acessado via reflexão (`_enemyParentField`) — campo não é público (skill doc estava incorreto).
- Logs adicionados em cada etapa crítica do drop para diagnóstico.

**Nota:** `manifest.json` e `thunderstore.toml` não precisaram ser alterados para a remoção do REPOLib — ele nunca foi declarado como dependência nesses arquivos.

---

## v1.1.2-hotfix — 2026-05-20 — Fix barra de munição no chão (SetBatteryLife)

**Causa:** `ApplyAmmo()` setava `numberOfBullets` via reflexão mas não chamava `ItemBattery.SetBatteryLife()`. A barra amarela de munição só aparecia quando o jogador pegava a arma (porque `ItemBattery` sincronizava ao equip), não enquanto o item estava no chão.

**Correção:** Após definir o valor final de `finalAmmo`, obtém `ItemBattery` via `GetComponentInChildren<ItemBattery>()` e chama:
```csharp
int bars = battery.batteryBars > 0 ? battery.batteryBars : 4;
int pct  = (int)Math.Round((float)finalAmmo / bars * 100f);
battery.SetBatteryLife(pct);  // public — sem reflexão
```
`batteryBars` é o número de segmentos da barra (4 para a shotgun). O percentual é calculado proporcionalmente ao máximo de munição.

**Refatoração:** Variável `finalAmmo` unifica todos os code paths (ammo zerada, randomizada, mantida) para que o SetBatteryLife seja chamado uma única vez ao final, independente do branch tomado.

---

## Arquitetura do mod

- **`Core.cs`** — entrada BepInEx, configs, 3 patches Harmony: `EnemyHunter.Awake` (mesh), `EnemyParent.Despawn` (drop), `Debug.LogWarning` (supressão)
