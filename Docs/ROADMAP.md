# HORDAX — Roadmap

## Fase 0 — Blockout jogável ✅

- runner automático;
- strafe por touch/mouse/teclado;
- auto-fire;
- hordas de blocos;
- gates numerados destrutíveis;
- upgrades no caminho;
- HUD;
- vitória/derrota;
- cena procedural de protótipo.

## Fase 1 — Feel do jogo 🟡 em andamento

Implementado:
- muzzle flash placeholder;
- hit punch em inimigos e gates;
- camera shake leve em impactos importantes;
- pickup com rotação + bob;
- leitura visual melhor de upgrades;
- cenário blockout com mais profundidade;
- aceleração progressiva do runner;
- restart rápido para playtest;
- curva de hordas 30 → 48 → 72.

Próximo:
- áudio placeholder;
- partículas baratas de impacto/morte;
- recoil visual da arma;
- tuning fino da câmera e velocidade.

## Fase 2 — Horda otimizada 🟡 iniciada

Implementado:
- object pool compartilhado de inimigos;
- prewarm de 96 unidades;
- reciclagem na morte;
- spawn com pequena variação para quebrar o aspecto de grade.

Próximo:
- pool por arquétipo/prefab quando houver múltiplos inimigos;
- LOD lógico por distância;
- animação barata para unidades distantes;
- GPU instancing;
- orçamento de inimigos por aparelho;
- profiling em Android/iOS.

## Fase 3 — Conteúdo autorável 🟡 iniciada

Implementado:
- `WeaponData` ScriptableObject;
- `EnemyData` ScriptableObject;
- `LevelDefinition` ScriptableObject;
- sequência autorável de hordas/gates/upgrades/finish;
- fallback para o nível blockout sem assets externos.

Próximo:
- inspector/editor dedicado para sequência da fase;
- múltiplas armas e raridades;
- chefes e eventos de pista;
- validação automática de level data.

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
