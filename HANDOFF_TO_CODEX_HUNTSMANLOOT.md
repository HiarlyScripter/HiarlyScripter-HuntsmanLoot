# HANDOFF — HuntsmanLoot → Codex

Data: 2026-06-04

---

## 1. Estado atual real

| Item | Status |
|---|---|
| Versão | **v1.1.2** |
| Drop ao matar Huntsman | ✅ Funcional |
| Shotgun nativa como base funcional | ✅ Funcional |
| Ammo aleatória | ✅ Funcional |
| BatteryLife ≤ 100% | ✅ Corrigido (`Mathf.Clamp`) |
| Multiplayer guard (`IsMasterClientOrSingleplayer`) | ✅ Funcional |
| Visual aprovado | ❌ **NÃO** — objetivo final não atingido |
| Captura do visual real do Huntsman | ⚠️ Implementada mas **não testada** |

---

## 2. Objetivo final

Quando o Huntsman morrer, o item dropado deve ter **o visual real da arma que o inimigo carregava** — o mesh que aparece na mão/corpo do Huntsman durante o jogo.

Não é:
- shotgun vanilla sem modificação
- shotgun com acessórios primitivos ("shotgun tunada")
- nenhum modelo fabricado em runtime

É: o `GameObject` da arma clonado da hierarquia do próprio inimigo antes do despawn.

---

## 3. Arquitetura atual aprovada

```
[HarmonyPrefix] EnemyParent.Despawn
  → Verificar HP = 0 (morte real)
  → FindWeaponVisual(__instance.transform) — seletor por score
  → Object.Instantiate(weaponGo) → DontDestroyOnLoad
  → _pendingWeaponVisual = clone

[HarmonyPostfix] EnemyParent.Despawn
  → Verificar HP = 0, masterClient, roll de chance
  → DropRifle(enemy):
      → Spawnar "Items/Item Gun Shotgun" (base funcional)
      → pendingVisual = _pendingWeaponVisual; _pendingWeaponVisual = null
      → HuntsmanRifleCustomizer.Apply(spawned, pendingVisual, Log)
      → ApplyAmmoSync(spawned)

HuntsmanRifleCustomizer.Apply(spawned, weaponVisual, log):
  → HasValidRenderer(weaponVisual)
    → SE válido: HideOriginalRenderers + AttachWeaponVisual + ApplyLight + ApplyIdentity
    → SE inválido: log "keeping native shotgun visible" + ApplyLight + ApplyIdentity apenas
```

### Componentes NUNCA tocados na shotgun base
`PhysGrabObject, ItemGun, ItemAttributes, ItemEquippable, RoomVolumeCheck, Rigidbody, Colliders, PhotonView`

---

## 4. Seletor por score — `FindWeaponVisual` (Core.cs)

Implementado com BFS que percorre **toda** a hierarquia sem pular subárvores.

| Condição | Score |
|---|---|
| Nome contém `gun`, `rifle`, `weapon`, `firearm` | +10 |
| Nome contém `shotgun` | +8 |
| Nome contém `barrel` | +5 |
| Nome contém `muzzle` | +2 |
| Tem renderer/mesh na subárvore | +5 |
| > 2 renderers na subárvore | +2 |
| Nome contém `body`, `torso`, `skin` | -20 |
| Nome contém `head` | -15 |
| Nome contém `arm`, `hand` | -15 (nó penalizado, filhos explorados) |
| Nome contém `leg`, `foot` | -10 |

**Regra crítica:** `arm`/`hand` penalizam o NÓ como candidato mas a subárvore é **sempre** explorada — a arma pode estar parentada a um osso de mão.

**Critério de vitória:** `score > 0` E `rendererCount > 0`.

**Log de diagnose produzido:**
```
[HuntsmanLoot] Candidate: path=<path> score=<n> renderers=<n> meshes=<n> accepted|rejected reason=<reason>
[HuntsmanLoot] Selected Huntsman weapon visual: <path> (score=<n>)
[HuntsmanLoot] No valid Huntsman weapon visual found
```

---

## 5. Estado visual — `HuntsmanRifleCustomizer.cs`

- `HideOriginalRenderers` só é chamado se `weaponVisual != null` E `HasValidRenderer(weaponVisual) == true`
- Se não houver visual válido: loga `keeping native shotgun visible` e mantém a shotgun intacta
- `AttachWeaponVisual`: cria `HuntsmanGunVisualRoot` como filho do item, remove `Collider`/`Rigidbody`/`MonoBehaviour` do clone, parenta o clone com `localPos=zero, localRot=identity, localScale=one`
- Preserva renderers de UI/battery/ammo via `_preserveKeywords`

---

## 6. Arquiteturas descartadas

| Abordagem | Motivo |
|---|---|
| Unity Editor + AssetBundle | Unity 2022.3.63f1+ = Extended LTS = licença Enterprise paga; 2022.3.62f1 = bundles incompatíveis com runtime do jogo (2022.3.67f2) |
| REPOLib custom item | Instável — NullRef em PhysGrabObject e RoomVolumeCheck; API incompatível com versão instalada |
| Prefab / builder em Awake | `Resources.Load` falha antes do jogo carregar; cascata de NullRef |
| Primitives runtime (scope, supressor, rail, etc.) | Rejeitado pelo usuário como "shotgun tunada" — não parece a arma real |
| Dois mods separados | Não recomendado no estado atual; manter em um mod só |
| StartCoroutine de Harmony postfix | NullRef quando `Instance` ainda não está pronto |

---

## 7. Caminhos importantes

| Descrição | Caminho |
|---|---|
| **Raiz do projeto** | `C:\Users\Hiarly\.claude\PROJETOS\REPO\HiarlyScripter-HuntsmanLoot\` |
| Source principal | `src\Core.cs` |
| Source customizer | `src\HuntsmanRifleCustomizer.cs` |
| Projeto C# | `src\HuntsmanLoot.csproj` |
| Build output | `build\HuntsmanLoot.dll` |
| Package DLL | `package\plugins\HiarlyScripter-HuntsmanLoot\HuntsmanLoot.dll` |
| **Perfil de teste** | `C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test\` |
| **DLL no perfil (FLAT)** | `...\BepInEx\plugins\HiarlyScripter-HuntsmanLoot\HuntsmanLoot.dll` |
| **NÃO criar** | `...\HiarlyScripter-HuntsmanLoot\HiarlyScripter-HuntsmanLoot\` ← subpasta duplicada |
| Log BepInEx | `...\REPO - Test\BepInEx\LogOutput.log` |

---

## 8. Regras rígidas — NÃO violar

- **Sem Unity** — licença paga para versões compatíveis
- **Sem AssetBundle** — descartado definitivamente
- **Sem REPOLib custom item** — instável
- **Sem prefab em Awake** — NullRef
- **Sem StartCoroutine** — NullRef de Harmony postfix
- **Sem coroutine**
- **Sem primitives fake como solução final**
- **Sem nova arquitetura sem aprovação explícita do usuário**
- **Sem push para GitHub**
- **Sem publicação no Thunderstore**
- **Sem PR**
- **Testar sempre no perfil REPO - Test**
- **Aguardar "ok" explícito antes de qualquer deploy**
- **Confirmar antes de bump de versão para v1.2.0**

---

## 9. DLL atual no perfil de teste

```
Arquivo: HuntsmanLoot.dll
Tamanho: 21.504 bytes
Data:    01/06/2026 10:11:58
Build:   0 erros, 0 avisos
Estado:  mods.yml enabled=true ✅
```

---

## 10. Próximos passos recomendados para Codex

### Prioridade: verificar se o seletor encontra o visual

1. **Auditar `Core.cs` — `FindWeaponVisual`**
   Verificar se a lógica de score está correta para a hierarquia real do Huntsman.
   O log do primeiro teste real vai revelar os nomes dos nós — usar isso para calibrar.

2. **Auditar `HuntsmanRifleCustomizer.cs` — `AttachWeaponVisual`**
   Verificar se a remoção de componentes (Collider/Rigidbody/MonoBehaviour) não quebra
   algum componente visual do jogo. Em caso de dúvida, usar `Destroy` em vez de `DestroyImmediate`.

3. **Se seletor encontrar visual válido:**
   - Confirmar que o clone é visualmente correto no item dropado
   - Ajustar `localPosition`/`localRotation`/`localScale` se necessário (dependendo da escala do modelo)
   - Considerar se algum componente do clone precisa ser preservado (ex: `LODGroup`)

4. **Se seletor NÃO encontrar visual válido:**
   - Declarar bloqueio técnico com evidência de log
   - Registrar os nomes reais dos nós na hierarquia do Huntsman
   - Propor ajuste no scoring baseado nos nomes reais encontrados

5. **Manter deploy sempre no perfil REPO - Test**
   Nunca fazer deploy automático. Sempre mostrar resultado do build e aguardar ok explícito.

### O que NÃO fazer
- Não criar nova arquitetura sem aprovação
- Não usar primitives como fallback visual
- Não subir versão para 1.2.0 sem aprovação explícita do usuário

---

## 11. Reflexão de campos (confirmada)

```csharp
AccessTools.Field(typeof(Enemy),       "HasHealth")       // bool
AccessTools.Field(typeof(Enemy),       "Health")          // EnemyHealth
AccessTools.Field(typeof(EnemyHealth), "healthCurrent")   // int
AccessTools.Field(typeof(ItemGun),     "numberOfBullets") // int
AccessTools.Field(typeof(EnemyParent), "Enemy")           // Enemy
```

---

## 12. Checklist de qualidade (antes de publicar)

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
- [ ] DLL não ofuscada (saída direta de `dotnet build`)
