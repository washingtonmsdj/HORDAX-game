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
- eventos de pista autoráveis: heal, reward e hazard;
- objetivos opcionais por fase com bônus de moedas;
- persistência do melhor número de objetivos concluídos.

Pendente:

- editor visual de fase;
- mais arquétipos;
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

- front-end de blockout criado em runtime;
- seleção sequencial de 5 fases de protótipo;
- bloqueio/desbloqueio de fase;
- upgrades permanentes de vida, dano e cadência;
- custos progressivos de upgrade;
- GameSession para transportar a fase escolhida;
- gerador seguro da cena FrontEnd;
- armory blockout com Rifle, SMG, Shotgun e Minigun;
- compra/desbloqueio persistente de armas;
- arma equipada persistente e aplicada no início da fase;
- rating de 1 a 3 estrelas por score;
- best score, best stars e contador de conclusões por fase;
- resultado inline com restart/menu/next.

Pendente:

- tela de resultados com arte final;
- loja visual final;
- missões globais/diárias além dos objetivos de fase;
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
