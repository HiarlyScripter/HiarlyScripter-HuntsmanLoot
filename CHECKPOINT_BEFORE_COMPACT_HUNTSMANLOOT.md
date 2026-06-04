# CHECKPOINT — HuntsmanLoot (pré-compact)

Data: 2026-05-30

---

## 1. Estado atual do mod

O mod está funcional no fluxo principal (drop + ammo + multiplayer guard), mas a
customização visual ainda não atingiu o nível de qualidade visual exigido pelo usuário.
O deploy mais recente está no perfil **REPO - Test** aguardando validação.

---

## 2. Versão atual

**v1.1.2** — sem bump ainda. Bump para v1.2.0 depende de aprovação explícita do usuário.

---

## 3. Objetivo do mod

Ao matar o Huntsman no R.E.P.O., dropar uma arma exclusiva chamada **"Huntsman Rifle"**
com visual próprio (escuro, metálico, teal glow), munição aleatória e suporte a multiplayer.

---

## 4. Caminhos importantes

| Descrição | Caminho |
|---|---|
| Raiz do projeto | `C:\Users\Hiarly\.claude\PROJETOS\REPO\HiarlyScripter-HuntsmanLoot\` |
| Source | `src\Core.cs`, `src\HuntsmanRifleCustomizer.cs` |
| Build output | `build\HuntsmanLoot.dll` |
| Package DLL | `package\plugins\HiarlyScripter-HuntsmanLoot\HuntsmanLoot.dll` |
| Perfil de TESTE | `C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test\BepInEx\plugins\HiarlyScripter-HuntsmanLoot\` |
| Perfil Default (não usar p/ este mod) | `...\profiles\Default\BepInEx\plugins\HiarlyScripter-HuntsmanLoot\` |
| Jogo | `E:\SteamLibrary\steamapps\common\REPO\` |
| Unity Hub | `C:\Program Files\Unity Hub\Unity Hub.exe` |

---

## 5. Arquivos principais

| Arquivo | Papel |
|---|---|
| `src\Core.cs` | Plugin principal: configs, reflexão, Awake, ApplyAmmo, patches Harmony |
| `src\HuntsmanRifleCustomizer.cs` | Customização visual runtime (materiais, geometria, luz, partículas, identidade) |
| `src\HuntsmanLoot.csproj` | Referências: BepInEx, Harmony, Assembly-CSharp, UnityEngine, Photon, PhysicsModule, ParticleSystemModule |
| `build\HuntsmanLoot.dll` | 23.040 bytes — build atual |
| `package\manifest.json` | Metadados Thunderstore |
| `package\CHANGELOG.md` | Histórico de versões |

---

## 6. Arquivos alterados nesta fase

- `src\Core.cs` — removido LoadCustomBundle/REPOLib, simplificado Awake, corrigido BatteryLife clamp
- `src\HuntsmanRifleCustomizer.cs` — criado; customiza objeto spawnado (materiais, geometria, luz, partículas, identidade)
- `src\HuntsmanLoot.csproj` — adicionado PhysicsModule + ParticleSystemModule; removido AssetBundleModule
- DELETADO: `src\HuntsmanRifleBuilder.cs` — construía prefab em Awake, causava NullRef

---

## 7. Arquiteturas tentadas e descartadas

### REPOLib custom item registration
- Tentativa de registrar item próprio via `REPOLib.Modules.Items.RegisterItem(ItemAttributes)`
- Descartada: instável, causava NullRef em PhysGrabObject e RoomVolumeCheck durante inicialização

### AssetBundle + Unity Editor
- Plano original: bundle próprio com prefab `HuntsmanRifle`
- Unity 2022.3.62f1 → bundles incompatíveis com runtime do jogo (2022.3.67f2)
- Unity 2022.3.63f1+ → Extended LTS, exige licença Enterprise/Industry paga
- **Resultado: AssetBundle descartado definitivamente. Sem alternativa gratuita.**

### HuntsmanRifleBuilder em Awake()
- Tentativa de instanciar prefab customizado no carregamento do plugin
- `Resources.Load("Items/Item Gun Shotgun")` durante Awake falhava (game não carregado)
- Gerava cascata de NullRef em componentes do jogo (PhysGrabObject, RoomVolumeCheck)
- **Descartado. Arquivo deletado.**

### Abordagem aprovada (atual)
Spawn da shotgun nativa primeiro → customização visual runtime no objeto já spawnado:
1. `EnemyParent.Despawn` patch detecta morte do Huntsman
2. `DropRifle()` spawna `Items/Item Gun Shotgun` (SP: Instantiate, MP: PhotonNetwork)
3. `HuntsmanRifleCustomizer.Apply()` é chamado sobre o objeto spawnado:
   - `ApplyMaterials()` — clona material nativo (mantém shader, usa HasProperty)
   - `AddGeometry()` — adiciona 8 filhos visuais (scope, rail, suppressor, foregrip, etc.)
   - `ApplyLight()` — Point Light teal como filho
   - `ApplyParticles()` — ember idle como filho
   - `ApplyIdentity()` — tenta renomear, loga campos string para diagnose

---

## 8. Estado técnico atual

| Item | Status |
|---|---|
| Drop ao matar Huntsman | ✅ Funcional |
| Multiplayer guard (MasterClientOnly) | ✅ Funcional |
| Chance de drop configurável | ✅ Funcional |
| Ammo aleatória | ✅ Funcional |
| BatteryLife > 100% | ✅ **Corrigido** (`Mathf.Clamp`) |
| Erro `_FresnelScale` no shader | ✅ **Corrigido** (`mr.material` + `HasProperty`) |
| Visual escuro/metálico | ⚠️ Aplicado, mas **ficou parecendo shotgun preta** (insuficiente) |
| Geometria filha (scope, etc.) | ⚠️ Código pronto, **não validado visualmente** após correção |
| Luz teal (glow) | ⚠️ Código pronto, não validado |
| Partículas ember | ⚠️ Código pronto, não validado |
| Nome visível "Huntsman Rifle" | ❌ **Log diz "aplicado" mas UI continua mostrando shotgun** |
| Barra amarela no inventário | ❌ Known issue visual — **não é bloqueante, não perseguir agora** |
| BepInEx sem erro HuntsmanLoot | ⚠️ **A validar no Repo-Test** |

---

## 9. Regras rígidas daqui para frente

- **Sem Unity** — licença paga requerida para versões compatíveis
- **Sem AssetBundle** — descartado definitivamente
- **Sem prefab em Awake** — causa NullRef nos componentes do jogo
- **Sem REPOLib custom item** — instável sem AssetBundle correto
- **Sem nova arquitetura sem aprovação explícita**
- **Sem publicação no Thunderstore**
- **Sem push para GitHub**
- **Sem PR**
- **Sem buscas amplas em disco** (não usar -Recurse em drives inteiros)
- **Testar sempre no perfil REPO - Test**
- **Aguardar "ok" explícito antes de deploy**

---

## 10. Próximos passos após /compact

### Usuário faz:
1. Abrir r2modman → perfil **REPO - Test**
2. Iniciar jogo
3. Matar Huntsman
4. Verificar:
   - BepInEx log: sem erro do HuntsmanLoot
   - Visual da arma (scope? suppressor? diferente da shotgun vanilla?)
   - Nome ao pegar (ainda "Item Gun Shotgun" ou mudou?)
   - Ammo/barra (≤ 100%?)
   - Glow teal visível?
5. **Enviar log BepInEx e print da arma no chão**

### Claude faz (após ver log):
- Ler campos `[AUDIT] ItemAttributes campos string:` no log
- Identificar o campo correto para o nome visível
- Uma única correção cirúrgica, se tecnicamente possível
- Se impossível sem registrar item próprio → declarar como limitação e parar

---

## 11. Checklist de qualidade final (antes de publicar)

- [ ] BepInEx sem erro do HuntsmanLoot
- [ ] Drop confiável ao matar Huntsman
- [ ] Sem duplicação de drop em multiplayer
- [ ] Singleplayer testado e aprovado
- [ ] Multiplayer host/client testado
- [ ] Visual suficientemente diferente da shotgun vanilla
- [ ] Nome coerente (ou limitação declarada)
- [ ] README/CHANGELOG/manifest.json atualizados
- [ ] Versão bumped para v1.2.0 com aprovação do usuário
- [ ] ZIP gerado limpo (sem bundle, sem editor files)
- [ ] DLL não ofuscada (saída direta do `dotnet build`)
- [ ] Thunderstore token via variável de ambiente (nunca hardcode)

---

## Notas técnicas rápidas

### Reflexão confirmada (AccessTools)
```
Enemy.HasHealth         — bool
Enemy.Health            — EnemyHealth
EnemyHealth.healthCurrent — int
ItemGun.numberOfBullets — int
EnemyParent.Enemy       — Enemy
ItemAttributes.item     — Item (ScriptableObject)
Item.itemName           — string (campo a confirmar para UI)
```

### Path de spawn nativo
```
"Items/Item Gun Shotgun"  ← Resources.Load + PhotonNetwork.InstantiateRoomObject
```

### Versão Unity do jogo
```
2022.3.67f2 (Build ID 23250495, confirmado via globalgamemanagers)
```

### Versão Unity free mais recente compatível
```
2022.3.62f1 — Personal license OK, mas bundles INCOMPATÍVEIS com runtime do jogo
2022.3.63f1+ — Extended LTS, exige licença paga
```
