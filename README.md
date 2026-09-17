# HORDAX

Protótipo jogável de um **runner/shooter 3D mobile** focado em correr por uma pista, destruir hordas, quebrar gates numerados e evoluir/trocar de arma durante a fase.

A prioridade do repositório é **gameplay e arquitetura antes da arte final**. Jogador, inimigos, armas, pickups e cenário continuam usando primitivas do Unity quando não há assets atribuídos. Isso permite refazer todo o 3D depois sem reescrever as regras principais.

## Estado atual

O projeto já contém:

- avanço automático do jogador com leve aceleração;
- controle lateral por touch/arraste, mouse e teclado;
- câmera third-person com shake;
- auto-fire e seleção do alvo à frente;
- projéteis com pool;
- Rifle, SMG, Shotgun e Minigun como arquétipos de blockout;
- múltiplos projéteis por disparo e spread;
- recoil visual da arma;
- `WeaponData` para armas autoráveis;
- pickups que trocam a arma durante a corrida;
- upgrades incrementais de dano/cadência;
- hordas usando blocos ou prefabs customizados;
- pool separado por prefab de inimigo;
- spawn de hordas em batches para reduzir pico de ativação;
- LOD lógico simples no steering dos inimigos distantes;
- hit feedback e efeitos placeholder com pool;
- gates numerados destrutíveis;
- HUD de vida, arma, stats, kills e progresso;
- vitória/derrota e restart rápido;
- `LevelDefinition` para sequenciar `Horde`, `Gate`, `Upgrade`, `Weapon` e `Finish`;
- `EnemyData` e `WeaponData` via ScriptableObject;
- validador de dados pelo menu do Editor;
- geração segura da cena de protótipo pelo Editor.

## Requisitos

- Unity 6 / 6000.0.x. O projeto aponta para **6000.0.23f1**.
- Nenhum asset externo é obrigatório para o blockout.

## Como executar

1. Clone o repositório.
2. Abra a pasta no Unity Hub.
3. Aguarde a importação/compilação.
4. Use **HORDAX > Open Prototype Scene**.
5. Pressione **Play**.

A cena `Assets/HORDAX/Scenes/Prototype.unity` é criada automaticamente caso ainda não exista. O gerador preserva outras cenas presentes no Build Settings.

> **Importante:** `HORDAX > Regenerate Prototype Scene (Destructive)` recria o blockout e pede confirmação antes de substituir a cena.

### Controles

- **Mobile:** arraste o dedo horizontalmente.
- **Editor/Desktop:** arraste o mouse ou use `A/D` / setas esquerda-direita.
- O personagem avança e atira automaticamente.
- Após vitória/derrota: toque, clique ou pressione `R` para reiniciar.

## Loop atual

`correr -> eliminar horda -> destruir gate -> pegar upgrade/arma -> enfrentar horda maior -> chegar ao final`

## Estrutura

```text
Assets/HORDAX/
  Editor/          cena, validação e ferramentas de authoring
  Scripts/
    Camera/        câmera e feedback visual
    Combat/        alvos, armas, projéteis e FX pool
    Core/          estado e fluxo global
    Data/          ScriptableObjects e enums de conteúdo
    Enemies/       agentes, hordas e pooling
    Player/        movimento e vida
    Prototype/     bootstrap e visuais placeholder
    UI/            HUD
    World/         gates, upgrades, armas e chegada
  Scenes/          cena gerada pelo Unity
Docs/
  ARCHITECTURE.md
  AUTHORING.md
  PLAYTEST.md
  ROADMAP.md
```

## Desenvolvimento sem Unity aberto

Mudanças maiores de arquitetura podem ser preparadas em branch sem executar o Editor. Quando isso acontece, elas permanecem **pendentes de compilação/playtest em Unity** antes de serem tratadas como validadas para produção. A branch `dev/weapon-crowd-systems` é usada para esse fluxo enquanto a `main` permanece no último checkpoint anterior.

## Filosofia do projeto

Os componentes de gameplay ficam separados dos visuais. Para evoluir do blockout para arte final, a intenção é trocar meshes, prefabs, materiais, animações, VFX e áudio mantendo `RunnerController`, `WeaponController`, `ShootableTarget`, `EnemyAgent`, `EnemyPool`, `DamageGate` e a camada de dados.

Leia também [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md), [Docs/AUTHORING.md](Docs/AUTHORING.md), [Docs/PLAYTEST.md](Docs/PLAYTEST.md) e [Docs/ROADMAP.md](Docs/ROADMAP.md).
