# HORDAX — Authoring de conteúdo

O blockout continua funcionando sem assets externos. A camada de dados existe para manter gameplay, balanceamento e arte 3D separados.

## LevelDefinition

Crie com:

Create > HORDAX > Level Definition

A fase define id, nome, comprimento, recompensa de conclusão e uma sequência de passos.

Tipos atuais:

- Horde: onda comum;
- Elite: pequeno grupo mais forte;
- Boss: encontro de chefe;
- Gate: bloco numerado destrutível;
- Upgrade: melhora a arma atual;
- Weapon: troca a arma;
- Finish: conclui a fase.

Cada encontro pode usar números diretamente ou um EnemyData. Quando EnemyData existe, vida, velocidade, dano, rank, recompensa, escala e prefab vêm dele.

## EnemyData

Crie com:

Create > HORDAX > Enemy Data

Campos principais:

- EnemyRank: Grunt, Elite ou Boss;
- vida, velocidade e dano;
- escala visual;
- recompensa em moedas;
- recompensa em score;
- prefab visual opcional.

Sem prefab o protótipo usa blocos. Grunts, elites e bosses recebem materiais provisórios diferentes para leitura imediata.

## WeaponData

Crie com:

Create > HORDAX > Weapon Data

Além dos stats básicos, cada arma agora possui WeaponRarity:

Common, Uncommon, Rare, Epic e Legendary.

A raridade aplica um multiplicador provisório de dano no runtime. Esses números ainda são de blockout e precisam de playtest.

WeaponData também aceita uma lista de WeaponModifierData.

## WeaponModifierData

Crie com:

Create > HORDAX > Weapon Modifier

Um modificador pode alterar:

- dano aditivo;
- multiplicador de dano;
- cadência;
- alcance;
- quantidade de projéteis;
- spread.

Isso permite criar variações como "Rapid", "Heavy", "Wide Shot" e outras sem duplicar WeaponController.

## CampaignDefinition

Crie com:

Create > HORDAX > Campaign Definition

CampaignDefinition guarda uma lista ordenada de LevelDefinition. Nesta etapa ele é a base de dados para seleção de fases e progressão futura; a tela de seleção ainda não foi criada.

## Recompensas e save

GameManager contabiliza durante a corrida:

- kills;
- elite kills;
- boss kills;
- moedas;
- score.

Ao vencer, a recompensa de conclusão da fase é somada e ProgressionService grava as moedas e o id da fase concluída em PlayerPrefs usando JSON.

Esse save é propositalmente simples. Cloud save, migração de versão e anti-cheat ficam para a etapa de produção.

## Validação

Use:

HORDAX > Validate Project Data

O validador verifica armas, modificadores, inimigos, fases e campanhas, além de emitir alertas para números que merecem profiling em mobile.

## Regra de arquitetura

Dados definem o conteúdo. Componentes runtime definem comportamento. Prefabs/modelos definem aparência.

A meta continua sendo poder substituir todo o 3D sem reescrever corrida, combate, horda, recompensas ou progressão.
