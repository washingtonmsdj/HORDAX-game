# HORDAX — Roadmap

## Fase 0 — Blockout jogável (feito nesta base)

- runner automático;
- strafe por touch/mouse/teclado;
- auto-fire;
- hordas de blocos;
- gates numerados destrutíveis;
- upgrades no caminho;
- HUD;
- vitória/derrota;
- cena procedural de protótipo.

## Fase 1 — Feel do jogo

- recoil e spread visual;
- hit flash e floating damage;
- muzzle flash/tracer;
- feedback ao destruir gate;
- câmera com shake leve;
- curvas de velocidade/dificuldade;
- áudio placeholder;
- melhor leitura de pickup e perigo.

## Fase 2 — Horda otimizada

- object pool de inimigos;
- LOD lógico por distância;
- animação barata para unidades distantes;
- GPU instancing;
- orçamento de inimigos por aparelho;
- profiling em Android/iOS.

## Fase 3 — Conteúdo autorável

- `WeaponData` ScriptableObject;
- `EnemyData` ScriptableObject;
- `LevelDefinition` ScriptableObject;
- editor de sequência de waves/gates/upgrades;
- múltiplas armas e raridades;
- chefes e eventos de pista.

## Fase 4 — Arte 3D

- personagem final;
- inimigos finais;
- rig/Animator;
- armas;
- pista modular;
- props/fundo;
- VFX;
- iluminação e pós-processamento mobile.

## Fase 5 — Meta game

- menu;
- seleção de fase;
- moedas;
- upgrades permanentes;
- unlock de armas;
- save local/cloud;
- missões e recompensas.

## Fase 6 — Produção mobile

- UI responsiva e safe areas;
- quality tiers;
- analytics/crash reporting;
- onboarding/tutorial;
- balanceamento;
- builds Android/iOS;
- store assets e publicação.

## Critério para sair do blockout

Antes de investir em modelos 3D finais, o loop deve ser divertido usando apenas blocos. Isso reduz retrabalho: arte melhora um jogo que já funciona, em vez de mascarar mecânicas ainda indefinidas.
