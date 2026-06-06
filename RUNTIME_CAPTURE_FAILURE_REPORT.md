# Runtime Capture Failure Report

Data: 2026-06-05

## Objetivo original

Fazer o item dropado pelo Huntsman usar visualmente a arma real que o inimigo carrega no jogo, sem usar shotgun tunada, primitives fake, AssetBundle, REPOLib custom item, outro mod, ou assets de mods de referencia.

## Arquitetura testada

A rota testada foi captura em runtime:

1. `EnemyParent.Despawn` Prefix, antes do inimigo ser desativado.
2. Busca por score na hierarquia do Huntsman.
3. Selecao do candidato visual real.
4. Clone do candidato antes do despawn.
5. Spawn da shotgun nativa como base funcional.
6. Anexacao do clone visual ao item dropado.
7. Validacao tecnica do clone.
8. Ocultacao dos renderers da shotgun nativa somente apos validacao.

## Evidencia do log

Fonte:

`C:\Users\Hiarly\AppData\Roaming\r2modmanPlus-local\REPO\profiles\REPO - Test\BepInEx\LogOutput.log`

Linhas relevantes observadas no teste real:

```text
[Info   :Huntsman Loot] [HuntsmanLoot] Selected Huntsman weapon visual: Level Generator/Enemies/Enemy - Hunter(Clone)/Enable/[VISUALS]/ANIM BOTTOM/________________________________/code aim vertical/________________________________/ANIM BODY/mesh body/________________________________/ANIM ARMS/[ARM RIGHT]/________________________________/ANIM ARM RIGHT 1/________________________________/ANIM ARM RIGHT 2/________________________________/ANIM HAND RIGHT/________________________________/ANIM GUN/mesh gun (score=48)
[Info   :Huntsman Loot] [HuntsmanLoot] Visual clone validation: PASS reason=mesh-renderer
[Info   :Huntsman Loot] [HuntsmanLoot] Huntsman weapon visual attached: success
[Info   :Huntsman Loot] [HuntsmanLoot] Original shotgun renderers hidden: 18 | Preserved: 0
[Info   :Huntsman Loot] [HuntsmanLoot] Customizer DONE
```

O mesmo padrao apareceu mais de uma vez no log: a selecao e a validacao tecnica passavam, o attach era marcado como sucesso, e a shotgun nativa era ocultada.

## Resultado visual real

Apesar dos logs indicarem sucesso tecnico, no jogo o item dropado ficou invisivel, aparecendo apenas um ponto/luz no chao.

Isso prova que a validacao tecnica do clone (`MeshRenderer`/`MeshFilter`/mesh/material ativos) nao garante renderizacao real do visual no contexto final do item dropado.

## Conclusao

A rota de runtime capture nao esta aprovada para produto final.

O problema restante nao e drop, ammo, chance, deploy, nem deteccao de morte. O problema e que capturar e anexar o mesh visual do Huntsman em runtime nao se mostrou confiavel o suficiente para entregar o objetivo final: um item visivelmente igual a arma real que o inimigo carrega.

Nao e recomendado continuar com hotfix incremental nesta rota, incluindo ajustes de escala, rotacao, bounds, material, ou novas heuristicas de validacao.

## Opcoes restantes

### A) Criar asset/modelo proprio autoral

Criar um modelo original inspirado na fantasia da arma do Huntsman, com autoria propria do HiarlyScripter. Esta e a rota mais controlavel para produto final, mas exige um pipeline de asset/modelo permitido.

### B) Investigar prefab/item nativo real, se existir

Investigar se existe um prefab ou item nativo acessivel que represente a arma real do Huntsman como item renderizavel independente, diferente do mesh preso a hierarquia/animacao do inimigo.

### C) Abandonar visual identico

Aceitar que o drop funcional use uma base nativa com identidade diferente, documentando que o visual identico da arma do Huntsman nao sera entregue nesta arquitetura.

### D) Nao copiar asset de mod de referencia

Nao copiar codigo, modelo, textura, prefab, bundle, ou qualquer asset de mods de referencia. Essa rota permanece descartada por criterio autoral e de seguranca.

## Recomendacao tecnica final

Encerrar a rota runtime capture.

Para continuar buscando o objetivo visual, a melhor proxima investigacao tecnica e a opcao B: verificar se o jogo possui um prefab/item nativo real da arma do Huntsman que possa ser usado como visual independente. Se isso nao existir, a opcao A e a rota de produto mais honesta: asset/modelo proprio autoral.
