# HORDAX — Roadmap

## Fase 0 — Blockout jogável ✅

Runner, strafe, auto-fire, hordas, gates, upgrades, HUD, vitória/derrota e cena procedural já estão estruturados.

## Fase 1 — Feel do jogo 🟡

Implementado:

- muzzle flash;
- hit punch;
- camera shake;
- recoil;
- pickups com movimento;
- FX placeholder com pool;
- leitura visual separada para grunt, elite e boss.

Pendente:

- áudio placeholder;
- tuning fino;
- VFX finais depois da direção de arte.

## Fase 2 — Horda otimizada 🟡

Implementado:

- pool de inimigos;
- pools separados por prefab;
- spawn em batches;
- LOD lógico simples;
- reciclagem de unidades.

Pendente:

- orçamento dinâmico por aparelho;
- crowd avoidance simplificado;
- GPU instancing/LOD visual;
- profiling real em Android/iOS.

## Fase 3 — Conteúdo autorável 🟡 avançado

Implementado:

- WeaponData;
- WeaponModifierData;
- WeaponRarity;
- EnemyData;
- EnemyRank;
- LevelDefinition;
- CampaignDefinition;
- Horde, Elite, Boss, Gate, Upgrade, Weapon e Finish;
- Rifle, SMG, Shotgun e Minigun;
- spread e múltiplos projéteis;
- validação de dados.

Pendente:

- editor visual de fase;
- mais arquétipos;
- eventos especiais de pista;
- curvas formais de balanceamento.

## Fase 4 — Arte 3D

- personagem;
- inimigos;
- bosses;
- rig/Animator;
- armas;
- pista modular;
- props;
- VFX;
- iluminação mobile.

## Fase 5 — Meta game 🟡 iniciada

Implementado:

- moedas de corrida;
- score;
- recompensa por inimigo;
- bônus por conclusão;
- wallet persistente;
- registro de fases concluídas;
- ProgressionService;
- CampaignDefinition.

Implementado nesta branch:

- front-end de blockout criado em runtime;
- seleção sequencial de 5 fases de protótipo;
- bloqueio/desbloqueio de fase;
- upgrades permanentes de vida, dano e cadência;
- custos progressivos de upgrade;
- GameSession para transportar a fase escolhida;
- gerador seguro da cena FrontEnd.

Pendente:

- tela de resultados dedicada;
- armory blockout com Rifle, SMG, Shotgun e Minigun;
- compra/desbloqueio persistente de armas;
- arma equipada persistente e aplicada no início da fase.

Pendente:

- loja visual final;
- missões;
- cloud save.

## Fase 6 — Produção mobile

- safe areas;
- quality tiers;
- analytics/crash reporting;
- onboarding;
- balanceamento;
- Android/iOS;
- publicação.

## Estado de verificação

A branch dev/weapon-crowd-systems está sendo desenvolvida sem abrir o Unity. O código permanece pendente de compilação e playtest no Editor antes de merge para main.
