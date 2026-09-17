# HORDAX — Arquitetura

## Objetivo

O projeto separa **regra**, **spawn**, **input**, **combate** e **visual placeholder** para permitir que o protótipo em blocos vire um jogo 3D completo sem reconstruir o gameplay.

## Fluxo principal

`PrototypeBootstrap` monta o blockout da fase e injeta as referências principais. Em uma produção maior, ele deve ser substituído por cenas/prefabs autorados e por dados de fase, mas os componentes abaixo podem permanecer.

### Core

- `GameManager`: estado global mínimo (`Playing`, `Won`, `Lost`), kills e progresso.
- `GameState`: enum de fluxo.

### Player

- `RunnerController`: movimento automático no eixo Z e deslocamento lateral.
- `PlayerHealth`: vida e derrota.

O visual do player é filho do objeto que contém esses componentes. Assim, trocar a cápsula por personagem, Animator e rig não exige alterar o movimento.

### Combat

- `ShootableTarget`: contrato base para qualquer coisa que possa receber tiro.
- `WeaponController`: seleção automática de alvo, cadência, dano e pool de projéteis.
- `Bullet`: projétil homing simples de protótipo.

Inimigos e portais compartilham o mesmo contrato de alvo. Isso evita colocar regras especiais de portal dentro da arma.

### Enemies

- `EnemyAgent`: HP, perseguição e ataque.
- `HordeSpawner`: gera ondas. Aceita prefab opcional; sem prefab cria cubos automaticamente.

Para centenas/milhares de inimigos na versão final, o próximo passo é substituir a criação individual por pool, GPU instancing/ECS ou uma simulação híbrida de horda.

### World

- `DamageGate`: bloco numerado destruível.
- `UpgradePickup`: aplica melhoria à arma.
- `FinishZone`: conclui a fase.

### Camera

- `RunnerCamera`: câmera de seguimento independente do player.

### UI

- `HudController`: HUD criado em runtime para o protótipo. Na arte final, pode ser substituído por Canvas autorado sem alterar o jogo.

### Prototype

- `PrototypeBootstrap`: cria pista, waves, portais, upgrades, câmera e jogador.
- `PrototypeMaterials`: materiais temporários centralizados.

Nada em `Prototype` deve virar dependência inevitável do conteúdo final. A única dependência temporária fora da pasta é o fallback visual do `WeaponController`/`HordeSpawner`, usado quando não há prefab configurado.

## Pontos de extensão recomendados

1. Transformar stats de armas/inimigos em `ScriptableObject`.
2. Criar `LevelDefinition` para autorar ondas e gates sem editar código.
3. Adicionar `EnemyPool` e separar simulação de horda do visual.
4. Trocar input legado por Input System quando os controles finais estiverem definidos.
5. Criar prefabs de jogador, arma, inimigo, gate, upgrade e VFX.
6. Adicionar áudio através de um serviço/event bus, evitando chamadas diretas espalhadas.
7. Adicionar testes de regras puras e smoke test da cena.

## Regra para a futura arte 3D

Mantenha o objeto raiz como gameplay e coloque modelo/Animator/VFX em filhos. Não coloque lógica de dano, spawn ou progressão dentro de scripts de animação/modelo.
