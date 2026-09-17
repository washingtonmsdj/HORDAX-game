# HORDAX — Level design do blockout

O objetivo desta camada é permitir testar ritmo e decisões laterais antes da arte final.

## Sequência

LevelDefinition aceita os seguintes passos:

- Horde;
- Elite;
- Boss;
- Gate;
- Upgrade;
- Weapon;
- Heal;
- Reward;
- Hazard;
- Finish.

Heal, Reward e Hazard possuem laneX. Isso permite colocar escolhas à esquerda/direita sem mudar o RunnerController.

## Eventos de pista

Heal restaura vida.

Reward concede moedas e score somente para a tentativa atual. As moedas entram na wallet persistente quando a fase é concluída.

Hazard causa dano uma vez e desaparece no blockout.

Os três usam primitivas e materiais provisórios. A futura arte pode trocar os visuais sem mudar as regras.

## Objetivos opcionais

Cada LevelDefinition pode ter objetivos extras.

Tipos atuais:

- matar inimigos;
- matar elites;
- coletar moedas durante a tentativa;
- alcançar score;
- terminar acima de uma porcentagem de vida.

Cada objetivo pode conceder moedas bônus.

O resultado da fase mostra quantos objetivos foram cumpridos. ProgressionService guarda o melhor número de objetivos concluídos por fase.

## Campanha blockout

As cinco fases runtime usam uma combinação de:

horda -> gate -> arma -> recompensa -> horda -> perigo -> elites -> cura -> upgrade -> horda -> arma -> boss -> finish

A quantidade e os números escalam por distrito. Isso ainda precisa de playtest real no Unity antes de qualquer decisão de balanceamento final.
