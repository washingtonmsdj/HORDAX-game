# HORDAX

Protótipo jogável de um **runner/shooter 3D mobile** inspirado na ideia de correr por uma pista, destruir hordas, quebrar blocos numerados e melhorar a arma durante a fase.

A prioridade deste repositório é **gameplay e arquitetura**. Nesta primeira versão, jogador, inimigos, portais e cenário são feitos com primitivas/blocos do Unity. Isso deixa o projeto pronto para trocar toda a arte 3D depois sem reescrever as regras do jogo.

## Estado atual

O protótipo já contém:

- avanço automático do jogador;
- controle lateral por toque/arraste e teclado;
- câmera third-person de runner;
- tiro automático no alvo mais próximo à frente;
- sistema de projéteis com pool;
- inimigos em hordas usando blocos como placeholder;
- inimigos que perseguem e atacam o jogador;
- blocos/portais numerados que recebem dano;
- pickups que aumentam dano e cadência;
- HUD de vida, arma, kills e progresso;
- sequência de ondas com dificuldade crescente;
- linha de chegada e estados de vitória/derrota;
- geração automática da cena de protótipo pelo Editor.

## Requisitos

- Unity 6 / 6000.0.x (o projeto aponta para 6000.0.23f1, mas pode ser aberto e migrado para outro patch 6000.0 LTS instalado).
- Não são necessários assets externos para rodar o protótipo.

## Como executar

1. Clone este repositório.
2. Abra a pasta no Unity Hub.
3. Aguarde a primeira importação/compilação.
4. A cena `Assets/HORDAX/Scenes/Prototype.unity` é criada automaticamente.
5. Abra essa cena caso ela não esteja aberta e pressione **Play**.

Se a cena não for criada automaticamente, use o menu **HORDAX > Generate Prototype Scene**.

### Controles

- **Mobile:** arraste o dedo horizontalmente.
- **Editor/Desktop:** arraste o mouse ou use `A/D` / setas esquerda-direita.
- O personagem avança e atira automaticamente.

## Loop do protótipo

`correr -> eliminar horda -> destruir bloco numerado -> coletar upgrade -> enfrentar horda maior -> chegar ao final`

## Estrutura

```text
Assets/HORDAX/
  Editor/          geração da cena de protótipo
  Scripts/
    Camera/        câmera do runner
    Combat/        alvos, arma e projéteis
    Core/          estado e fluxo global
    Enemies/       inimigos e hordas
    Player/        movimento e vida
    Prototype/     bootstrap e visuais placeholder
    UI/            HUD
    World/         portais, upgrades e chegada
  Scenes/          gerada pelo Unity na primeira abertura
Docs/
  ARCHITECTURE.md
  ROADMAP.md
```

## Filosofia do projeto

Os componentes de gameplay ficam separados dos visuais. Para evoluir do blockout para arte final, a intenção é substituir meshes, materiais, animações, VFX e áudio mantendo `RunnerController`, `WeaponController`, `ShootableTarget`, `EnemyAgent`, `DamageGate` e o restante das regras.

Leia também [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md) e [Docs/ROADMAP.md](Docs/ROADMAP.md).
