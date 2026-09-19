# HORDAX — conceito principal de pista dupla

Status: **referência visual e mecânica aprovada**.

## Regra central

HORDAX é um **runner em uma pista longa dividida em duas faixas paralelas**.

- **Faixa de arsenal/upgrades:** contém armas, upgrades, gates de valor e escolhas de evolução.
- **Faixa de horda:** contém os inimigos, elites e bosses avançando na direção oposta.
- Os inimigos **não surgem pelas laterais nem por trás**.
- A leitura visual deve deixar as duas faixas imediatamente óbvias.
- O jogador permanece em movimento para frente e ataca a horda que vem pela faixa inimiga.
- A progressão de armas deve acontecer visualmente ao longo da faixa de arsenal, com marcos como 50, 230, 300 etc.
- Bosses ocupam a mesma faixa da horda e funcionam como bloqueios/encontros de progressão, não como inimigos laterais.

## Composição aprovada

A câmera deve enxergar longe na pista para reforçar a escala do desafio:

1. jogador em primeiro plano;
2. faixa de arsenal claramente separada;
3. faixa de horda densamente ocupada;
4. gates/upgrades crescendo em valor ao longo da pista;
5. horda aumentando em densidade e dificuldade na distância;
6. cenário épico apenas como moldura — a pista e suas duas faixas continuam sendo o foco.

## O que evitar

Não transformar o jogo em arena 360º.
Não spawnar inimigos à esquerda/direita da pista ou atrás do jogador.
Não misturar armas e inimigos de forma aleatória nas duas faixas.
Não perder a separação visual entre **arsenal** e **horda**.

## Referência visual

Ver `two-lane-core.svg`.

Esta referência deve orientar gameplay, level design, câmera, HUD e futuras artes do projeto.


## Loop de combate aprovado

A interação entre as duas faixas é intencionalmente assimétrica:

- o jogador corre e coleta progressão apenas na faixa de arsenal;
- a horda ocupa apenas a faixa inimiga e avança em sentido contrário;
- a arma mira transversalmente da faixa de arsenal para a faixa da horda;
- inimigos comuns e elites que alcançam a linha do jogador contam como **breach/leak**, causam dano e saem do campo;
- bosses param o avanço e viram um encontro obrigatório, com barra de HP própria;
- a câmera deve manter as duas faixas legíveis ao mesmo tempo e abrir o enquadramento durante bosses.

Isso evita transformar HORDAX em um shooter de arena ou em um runner de três pistas tradicional. A identidade central é a leitura simultânea de **progressão de arsenal de um lado** e **pressão crescente da horda do outro**.
