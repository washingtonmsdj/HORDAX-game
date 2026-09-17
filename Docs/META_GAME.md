# HORDAX — Meta game foundation

Esta camada foi preparada antes da interface final para evitar que moedas e desbloqueios fiquem acoplados ao HUD ou à cena do protótipo.

## Runtime

GameManager controla os valores da tentativa atual:

- RunCoins;
- Score;
- EnemyKills;
- EliteKills;
- BossKills.

Inimigos entregam recompensa ao morrer. LevelDefinition define o bônus de conclusão.

## Persistência

ProgressionService é um singleton persistente entre cenas.

No momento ele grava:

- walletCoins;
- lista de levelIds concluídos.

Formato: JSON salvo em PlayerPrefs sob uma chave versionada.

## Campanha

CampaignDefinition organiza uma lista ordenada de LevelDefinition.

A futura tela de seleção deve consultar CampaignDefinition e ProgressionService, sem colocar regras de desbloqueio dentro de botões de UI.

## Próxima evolução

1. definir regra de desbloqueio por campanha;
2. adicionar estrelas/objetivos opcionais;
3. criar resultado de fase;
4. separar moedas soft/premium somente se o design realmente exigir;
5. criar migração de versão do save antes de produção.
