# CHECKPOINT — HuntsmanLoot (pós-auditoria Codex, pré-compact)

Data: 2026-06-01

---

## 1. Estado atual real

| Item | Status |
|---|---|
| Versão | **v1.1.2** |
| Drop ao matar Huntsman | ✅ Funcional |
| Shotgun nativa como base | ✅ Funcional |
| Ammo aleatória | ✅ Funcional |
| BatteryLife ≤ 100% | ✅ **Corrigido** (`Mathf.Clamp`) |
| Customizer roda (START/DONE no log) | ✅ Confirmado |
| Visual atual | ❌ **Inadequado** — parece shotgun vanilla ou shotgun tunada |
| Objetivo final | ❌ **Não atingido** — deve parecer a **arma do próprio Huntsman** |

**Objetivo final:** Quando o Huntsman morrer, o item dropado deve ter o visual real da arma que o inimigo carregava no jogo — não uma shotgun com acessórios, não primitives falsos.

---

## 2. Arquiteturas descartadas

| Abordagem | Motivo do descarte |
|---|---|
| Unity Editor + AssetBundle | Unity 2022.3.63f1+ = Extended LTS = licença Enterprise paga; 2022.3.62f1 gera bundles incompatíveis com o runtime do jogo (2022.3.67f2) |
| REPOLib custom item registration | Instável sem bundle correto; NullRef em PhysGrabObject e RoomVolumeCheck |
| Prefab / builder em Awake() | Resources.Load falha antes do jogo carregar; cascata de NullRef em componentes nativos |
| Primitives runtime (fake model) | Aprovado temporariamente para teste visual, mas **rejeitado como solução final** — não parece a arma real |
| Dois mods separados | Não recomendado no estado atual; manter tudo em um só mod |

---

## 3. Auditoria do Codex

- **Mods de referência** (`DougHRito-HunterDropsGun`, `DougHRito-HunterGun`) **não encontrados** no ambiente local — não foi possível inspecionar a implementação deles.
- **Recomendação do Codex:** manter um único mod; não dividir.
- **Caminho correto identificado:** capturar o visual real da arma do Huntsman antes do inimigo ser desativado/destruído, cloná-lo e anexá-lo ao item dropado.

### Problemas identificados no seletor atual (`FindWeaponVisual`)

| Problema | Descrição |
|---|---|
| Seletor frágil | Pode capturar nós vazios (helpers, muzzle, effect) sem MeshRenderer real |
| Rejeição de subárvore arm/hand | Ao rejeitar `arm` ou `hand`, **toda a subárvore** é pulada — a arma pode estar parentada a um osso de mão/braço |
| Ausência de validação de mesh | Aceita qualquer nó com keyword de arma, mesmo sem renderer/mesh visível |
| Ocultação incondicional | O customizer atual oculta renderers da shotgun mesmo quando `weaponVisual == null` |

---

## 4. Arquitetura aprovada para a próxima execução

### Base funcional
- Shotgun nativa (`Items/Item Gun Shotgun`) continua como base funcional (física, ItemGun, PhysGrabObject, etc.)
- **Visual original da shotgun só deve ser ocultado se houver visual válido da arma do Huntsman**

### Captura do visual
- Capturar em `[HarmonyPrefix]` de `EnemyParent.Despawn` (inimigo ainda intacto)
- Alternativamente: capturar em patch de `EnemyHunter.Start` ou evento de spawn (mais cedo no ciclo de vida)

### Seletor por score (substituir FindWeaponVisual atual)

```
Para cada nó na hierarquia do Huntsman:
  score = 0
  Se nome contém gun/rifle/shotgun/weapon/firearm/barrel/muzzle → score += 10
  Se tem MeshRenderer ou SkinnedMeshRenderer ativo → score += 5
  Se tem filhos com renderer → score += 2
  Se nome contém body/head/torso/skin → score -= 20 (candidato ruim, mas NÃO pular filhos)
  Se nome contém leg/foot → score -= 10 (candidato ruim, mas NÃO pular filhos)

REGRA CRÍTICA:
  Rejeitar o NÓ como candidato se score < 5, mas CONTINUAR descendo nos filhos.
  arm/hand: rejeitar o nó, mas DESCER nos filhos (a arma PODE estar parentada a um osso de mão).
  
Escolher candidato com maior score > 0 E que tenha renderer real.
```

### Anexação do visual
```
1. HuntsmanGunVisualRoot → SetParent(spawned.transform, false) → localPos=zero, localRot=identity, localScale=one
2. Clone do visual (já capturado no Prefix) → SetParent(visualRoot.transform, false) → localPos=zero, localRot=identity, localScale=one
3. Remover de todos os filhos do clone: Collider, Rigidbody, MonoBehaviour (mantém MeshRenderer, SkinnedMeshRenderer, MeshFilter)
4. Ativar clone
5. SÓ ENTÃO ocultar renderers da shotgun base
```

### Componentes preservados na shotgun base (NUNCA tocar)
```
PhysGrabObject, ItemGun, ItemAttributes, ItemEquippable, RoomVolumeCheck,
Rigidbody, Colliders principais, PhotonView
```

---

## 5. Regras rígidas

- **Sem Unity** — licença paga para versões compatíveis
- **Sem AssetBundle** — descartado definitivamente
- **Sem REPOLib custom item** — instável
- **Sem prefab em Awake** — NullRef
- **Sem builder novo**
- **Sem StartCoroutine** — NullRef quando chamado de Harmony postfix
- **Sem coroutine**
- **Sem primitives fake como solução principal**
- **Sem nova arquitetura sem aprovação**
- **Sem publicação no Thunderstore**
- **Sem push para GitHub**
- **Sem PR**
- **Testar sempre no perfil REPO - Test**
- **Aguardar "ok" explícito antes de qualquer deploy**
- **Confirmar antes de bump de versão para v1.2.0**

---

## 6. Caminhos importantes

| Descrição | Caminho |
|---|---|
| Raiz do projeto | `C:\Users\Hiarly\.claude\PROJETOS\REPO\HiarlyScripter-HuntsmanLoot\` |
| Source principal | `src\Core.cs` |
| Source customizer | `src\HuntsmanRifleCustomizer.cs` |
| Build output | `build\HuntsmanLoot.dll` |
| Package DLL | `package\plugins\HiarlyScripter-HuntsmanLoot\HuntsmanLoot.dll` |
| **Perfil de teste (caminho correto)** | `C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test\BepInEx\plugins\HiarlyScripter-HuntsmanLoot\` |
| **DLL correta no perfil** | `...\HiarlyScripter-HuntsmanLoot\HuntsmanLoot.dll` |
| **NÃO criar** | `...\HiarlyScripter-HuntsmanLoot\HiarlyScripter-HuntsmanLoot\` ← subpasta duplicada |

---

## 7. Estado dos arquivos fonte

### `src/Core.cs` — estado atual
- `[HarmonyPrefix]` em `EnemyParent.Despawn` → `OnHuntsmanDespawnPre` → busca visual + clona em `_pendingWeaponVisual`
- `[HarmonyPostfix]` em `EnemyParent.Despawn` → `OnHuntsmanDespawnPost` → drop + passa `_pendingWeaponVisual` para customizer
- `FindWeaponVisual(Transform root)` → BFS com accept/reject keywords simples (**a melhorar**)
- `ApplyAmmoSync` → síncrono, sem coroutine ✅
- `ClearPendingVisual()` → cleanup em early returns ✅

### `src/HuntsmanRifleCustomizer.cs` — estado atual
- `Apply(spawned, weaponVisual, log)` → oculta renderers + anexa visual + luz + identidade
- `HideOriginalRenderers` → desativa MeshRenderer/SkinnedMeshRenderer (preserva keywords UI/battery/bar)
- `AttachWeaponVisual` → cria HuntsmanGunVisualRoot, remove Collider/Rigidbody/MonoBehaviour do clone, ativa visual
- **Problema atual:** oculta renderers mesmo se `weaponVisual == null` → precisa ser condicional

---

## 8. Próxima execução após /compact

### Ordem obrigatória:
1. Ler este checkpoint
2. Resumir estado atual
3. Executar as seguintes correções cirúrgicas (na ordem):

**Correção 1 — `HideOriginalRenderers` condicional:**
- Só ocultar renderers da shotgun se `weaponVisual != null`
- Se `weaponVisual == null`: logar erro e retornar, mantendo a shotgun visível

**Correção 2 — Seletor por score em `FindWeaponVisual`:**
- Substituir BFS simples por scoring
- **CRÍTICO:** não pular filhos de `arm`/`hand` — apenas rejeitar o nó como candidato
- Validar que o candidato vencedor tem renderer real (MeshRenderer ou SkinnedMeshRenderer) com mesh não nulo
- Logar o score de cada candidato sério para diagnose

**Após correções:**
- Build Release
- Mostrar resultado do build
- **Aguardar "ok" explícito antes de deploy**

---

## 9. DLL atual no perfil de teste

```
Arquivo: HuntsmanLoot.dll
Tamanho: 20.480 bytes
Data:    01/06/2026 08:42:41
Build:   v1.1.2 com nova arquitetura de captura visual (seletor BFS simples, não testado ainda)
```

---

## 10. Checklist de qualidade antes de publicar

- [ ] BepInEx sem erro do HuntsmanLoot
- [ ] Drop confiável ao matar Huntsman
- [ ] Visual da arma é o da arma real do Huntsman (não shotgun vanilla, não primitives)
- [ ] Sem duplicação de drop em multiplayer
- [ ] Singleplayer testado e aprovado
- [ ] Multiplayer host/client testado
- [ ] Nome coerente ("Huntsman Rifle" ou similar)
- [ ] README/CHANGELOG/manifest.json atualizados
- [ ] Versão bumped para v1.2.0 com aprovação explícita do usuário
- [ ] ZIP gerado limpo (sem bundle, sem editor files)
- [ ] DLL não ofuscada
- [ ] Thunderstore token via variável de ambiente (nunca hardcode)
