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
- efeitos placeholder de impacto, morte e quebra de gate com pool;
- recoil visual da arma;
- troca visual do blockout conforme o arquétipo da arma.

Próximo:
- áudio placeholder;
- partículas/VFX finais depois da direção de arte;
- tuning fino da câmera, recoil e velocidade;
- feedback de dano crítico/elite quando existirem esses sistemas.

## Fase 2 — Horda otimizada 🟡 avançando

Implementado:
- object pool de inimigos;
- prewarm de 96 unidades;
- reciclagem na morte;
- spawn com pequena variação para quebrar o aspecto de grade;
- pools separados por prefab de inimigo;
- LOD lógico simples: steering distante recalculado com frequência menor.

Próximo:
- orçamento dinâmico de unidades por aparelho;
- animação barata para unidades distantes;
- GPU instancing para arte final compatível;
- profiling real em Android/iOS;
- crowd avoidance/flow simplificado se necessário.

## Fase 3 — Conteúdo autorável 🟡 avançando

Implementado:
- `WeaponData` ScriptableObject;
- `EnemyData` ScriptableObject;
- `LevelDefinition` ScriptableObject;
- sequência autorável de hordas/gates/upgrades/armas/finish;
- fallback para o nível blockout sem assets externos;
- arquétipos Rifle, SMG, Shotgun e Minigun;
- múltiplos projéteis por tiro e spread;
- `WeaponPickup` para troca de arma durante a corrida;
- validador de dados em `HORDAX > Validate Project Data`.

Próximo:
- inspector/editor dedicado para montar fases visualmente;
- raridades e modificadores de armas;
- inimigos elite;
- chefes e eventos de pista;
- curvas de balanceamento por nível.

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

## Estado de verificação desta branch

A branch de desenvolvimento pode avançar sem abrir o Unity, mas alterações runtime feitas dessa forma ficam marcadas como **pendentes de compilação/playtest no Editor**. O objetivo é preservar a `main` jogável enquanto sistemas maiores são estruturados e revisados estaticamente.

## Critério para sair do blockout

Antes de investir em modelos 3D finais, o loop deve ser divertido usando apenas blocos. Isso reduz retrabalho: arte melhora um jogo que já funciona, em vez de mascarar mecânicas ainda indefinidas.
