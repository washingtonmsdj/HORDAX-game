# HORDAX

Protótipo jogável de um **runner/shooter 3D mobile** inspirado na ideia de correr por uma pista, destruir hordas, quebrar blocos numerados e melhorar a arma durante a fase.

A prioridade deste repositório é **gameplay e arquitetura**. Jogador, inimigos, gates e cenário ainda usam primitivas/blocos do Unity para que toda a arte 3D possa ser substituída depois sem reescrever as regras do jogo.

## Estado atual

O protótipo contém:

- avanço automático com leve aceleração ao longo da fase;
- controle lateral por touch/arraste, mouse e teclado;
- câmera third-person de runner com shake leve;
- tiro automático no alvo mais próximo à frente;
- projéteis com pool;
- hordas de 30, 48 e 72 inimigos;
- **pool compartilhado de inimigos com 96 unidades pré-aquecidas**;
- inimigos que perseguem e atacam o jogador;
- feedback de hit por escala/punch;
- muzzle flash placeholder;
- gates numerados destrutíveis;
- pickups flutuantes que aumentam dano/cadência e nível da arma;
- HUD de vida, arma, kills e progresso;
- restart por toque/clique/tecla após vitória ou derrota;
- linha de chegada e estados de vitória/derrota;
- geração segura da cena de protótipo pelo Editor.

## Requisitos

- Unity 6 / 6000.0.x. O projeto aponta para **6000.0.23f1**.
- Não são necessários assets externos para rodar o blockout.

## Como executar

1. Clone este repositório.
2. Abra a pasta no Unity Hub.
3. Aguarde a primeira importação/compilação.
4. Use **HORDAX > Open Prototype Scene**.
5. Pressione **Play**.

A cena `Assets/HORDAX/Scenes/Prototype.unity` é criada automaticamente caso ainda não exista. O gerador preserva outras cenas já presentes no Build Settings.

> **Importante:** `HORDAX > Regenerate Prototype Scene (Destructive)` existe apenas para recriar o blockout do zero e pede confirmação antes de substituir a cena.

### Controles

- **Mobile:** arraste o dedo horizontalmente.
- **Editor/Desktop:** arraste o mouse ou use `A/D` / setas esquerda-direita.
- O personagem avança e atira automaticamente.
- Após vitória/derrota: toque, clique ou pressione `R` para reiniciar.

## Loop do protótipo

`correr -> eliminar horda -> destruir gate numerado -> coletar upgrade -> enfrentar horda maior -> chegar ao final`

## Estrutura

```text
Assets/HORDAX/
  Editor/          ferramentas seguras de criação/abertura da cena
  Scripts/
    Camera/        câmera e feedback visual
    Combat/        alvos, arma e projéteis
    Core/          estado e fluxo global
    Enemies/       inimigos, hordas e pooling
    Player/        movimento e vida
    Prototype/     bootstrap e visuais placeholder
    UI/            HUD
    World/         gates, upgrades e chegada
  Scenes/          cena gerada pelo Unity
Docs/
  ARCHITECTURE.md
  PLAYTEST.md
  ROADMAP.md
```

## Filosofia do projeto

Os componentes de gameplay ficam separados dos visuais. Para evoluir do blockout para arte final, a intenção é substituir meshes, materiais, animações, VFX e áudio mantendo `RunnerController`, `WeaponController`, `ShootableTarget`, `EnemyAgent`, `DamageGate` e o restante das regras.

O blockout deve permanecer descartável visualmente e estável mecanicamente: modelos 3D melhores entram depois, sem transformar a arte em dependência da lógica.

Leia também [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md), [Docs/PLAYTEST.md](Docs/PLAYTEST.md) e [Docs/ROADMAP.md](Docs/ROADMAP.md).
