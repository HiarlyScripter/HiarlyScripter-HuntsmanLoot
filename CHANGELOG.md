# Changelog — Huntsman Loot

## Proxima release (nao publicada) - 2026-06-07
- Corrigido o icone da Huntsman Rifle no inventario usando render real via `SemiIconMaker`.
- Ajustado o enquadramento da camera de icone para mostrar a arma dentro do quadrado, em vez de apenas o cano.
- Corrigida a orientacao do icone invertendo o vetor `upWorld`/roll da camera, sem alterar o enquadramento aprovado.
- Removida a rota de sprite runtime desenhado/procedural para o icone.
- Teste visual aprovado e `LogOutput.log` sem erros relacionados ao HuntsmanLoot.

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
