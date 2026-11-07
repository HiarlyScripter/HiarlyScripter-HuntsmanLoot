# Checkpoint 2026-11-06 — Revalidação para o update do jogo (Unity 6000.3.17)

**Status: Aprovado in-game pelo autor em 2026-11-06 (v1.1.5).**

- Jogo atualizado em 06/11/2026 (Assembly-CSharp.dll 20:55; Unity 6000.3.17f1, antes 2022.3.67).
- Build do mod (1.1.4, sem alterações em `src/`) compila contra a DLL nova: 0 erros / 0 avisos (netstandard2.1).
- Verificado por decompilação (ilspycmd): `Enemy.HasHealth/Health`, `EnemyParent.Enemy/Despawn`, `EnemyHealth.healthCurrent`, `ItemGun.numberOfBullets`, `ItemBattery.batteryLifeInt/batteryBars/SetBatteryLife/Start/suppressBatteryUI`, `ItemAttributes.item/hasIcon/itemEquippable`, `Item.itemName/itemNameLocalized`, `EnemyHunter.Awake`: todos presentes.
- Não verificável fora do jogo: mesh `Hunter Gun` no prefab, `Items/Item Gun Shotgun`, material `Enemy Hunter`, Photon spawn.
- DLL copiada para o perfil `REPO - Test` (plugins\HiarlyScripter-HuntsmanLoot). `mods.yml` não foi alterado (BepInEx carrega a pasta de plugins direto).
- Docs antigos (junho/2026, fase de captura falha) movidos para `CHECKPOINTS/arquivo-obsoleto/`.
- v1.2.0.zip da raiz = versão REPOLib de 20/05, nunca lançada; mantida como histórico.

## Roteiro de teste
Solo: matar Huntsman → drop; visual Hunter Gun; ícone correto; nome; ammo aleatória e barra no chão; tiro. Multiplayer: drop só do host (MasterClientOnly), cliente vê/pega. Log: sem MissingMethod/NullReference.
