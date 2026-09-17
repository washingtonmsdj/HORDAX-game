# HORDAX

HORDAX é um protótipo de runner/shooter 3D mobile em que o jogador avança por uma pista, enfrenta hordas, quebra gates numerados e troca/evolui armas durante a fase.

O projeto prioriza gameplay e arquitetura antes da arte final. Quando não há assets atribuídos, jogador, inimigos, armas e cenário usam primitivas do Unity.

## Estado atual

O projeto já possui:

- runner automático com controle lateral;
- auto-fire;
- Rifle, SMG, Shotgun e Minigun;
- raridades e modificadores de arma;
- projéteis, inimigos e FX com pooling;
- hordas em batches;
- LOD lógico simples;
- grunt, elite e boss;
- recompensas por inimigo;
- moedas e score da corrida;
- bônus de conclusão;
- save local de wallet, fases concluídas e upgrades permanentes;
- CampaignDefinition para organizar várias fases;
- campanha blockout com 5 fases gerada em runtime;
- front-end blockout com seleção de fase e compra de upgrades;
- LevelDefinition com Horde, Elite, Boss, Gate, Upgrade, Weapon e Finish;
- HUD de blockout;
- validador de dados;
- fallback completo em blocos.

## Requisitos

Unity 6 / 6000.0.x. O projeto aponta para 6000.0.23f1.

Nenhum asset externo é obrigatório para o blockout.

## Como executar quando chegar a hora do playtest

Fluxo completo:
1. clone o repositório;
2. abra no Unity Hub;
3. use HORDAX > Open Front End;
4. escolha uma fase e pressione o botão correspondente.

Para testar só o gameplay, use HORDAX > Open Prototype Scene.

No resultado da fase existem botões de Restart, Menu e Next quando houver próxima fase desbloqueada.

## Desenvolvimento atual sem Unity

A main permanece no checkpoint anterior. O desenvolvimento estrutural mais novo está na branch:

dev/weapon-crowd-systems

Essa branch está pendente de compilação e playtest no Unity antes de ser considerada validada.

## Estrutura

Assets/HORDAX/Editor contém ferramentas de cena e validação.

Assets/HORDAX/Scripts contém Camera, Combat, Core, Data, Enemies, Player, Prototype, UI e World.

Docs contém arquitetura, authoring, playtest, roadmap e base do meta game.

A regra central continua: modelos, animações e VFX devem poder ser substituídos sem reescrever as regras principais.
