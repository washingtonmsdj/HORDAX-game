# HORDAX — Arquitetura

## Objetivo

O projeto separa **regra**, **spawn**, **input**, **combate**, **dados** e **visual placeholder** para permitir que o protótipo em blocos vire um jogo 3D completo sem reconstruir o gameplay.

## Fluxo principal

`PrototypeBootstrap` monta o blockout da fase e injeta referências principais. Ele existe para prototipagem rápida; em produção pode ser substituído por cenas/prefabs autorados e `LevelDefinition`, mantendo os componentes runtime.

### Core

- `GameManager`: estado global mínimo (`Playing`, `Won`, `Lost`), kills e progresso.
- `GameState`: enum de fluxo.

### Player

- `RunnerController`: movimento automático no eixo Z e deslocamento lateral.
- `PlayerHealth`: vida e derrota.

O visual do player é filho do objeto raiz de gameplay. Mesh, Animator e rig podem mudar sem alterar movimento ou combate.

### Data

- `WeaponArchetype`: arquétipos básicos de arma.
- `WeaponData`: stats e prefabs de arma.
- `EnemyData`: stats e prefab visual de inimigo.
- `LevelDefinition`: sequência autorável de passos da fase.

A camada de dados define **o que** existe; componentes runtime definem **como** se comporta.

### Combat

- `ShootableTarget`: contrato base para qualquer alvo que recebe tiro.
- `WeaponController`: targeting, cadência, dano, spread, múltiplos projéteis, troca de arma e pools de projéteis separados por prefab.
- `Bullet`: projétil homing de protótipo com offset de spread.
- `CombatFxPool`: pool de efeitos placeholder de impacto/morte/quebra.
- `CombatFx`: partícula simples reciclável.

Inimigos e gates usam o mesmo contrato `ShootableTarget`, mantendo a arma desacoplada de regras específicas de mundo.

### Enemies

- `EnemyAgent`: HP, perseguição, ataque e steering com frequência reduzida quando distante.
- `HordeSpawner`: ativa hordas em batches ao longo de alguns frames para reduzir pico de CPU.
- `EnemyPool`: pools separados por prefab, além do pool de cubos do blockout.

A arquitetura atual já evita `Instantiate/Destroy` no ciclo normal das hordas. GPU instancing/ECS continua opcional para escalas maiores e deve ser decidido com profiling real em aparelho.

### World

- `DamageGate`: gate numerado destruível.
- `UpgradePickup`: melhora a arma atual sem trocar o arquétipo.
- `WeaponPickup`: troca a arma por `WeaponData` ou por preset de blockout.
- `FinishZone`: conclui a fase.

### Camera

- `RunnerCamera`: câmera de seguimento e shake independente do player.

### UI

- `HudController`: HUD runtime de protótipo. Pode ser substituído por Canvas final sem alterar as regras.

### Prototype

- `PrototypeBootstrap`: cria pista, hordas, gates, upgrades, armas, câmera, jogador e pools.
- `PrototypeMaterials`: materiais temporários centralizados.
- `PrototypeWeaponView`: representa visualmente o arquétipo atual e aplica recoil simples.

Nada em `Prototype` deve virar dependência obrigatória da arte final. Dependências de `PrototypeMaterials` existentes em fallbacks runtime são deliberadamente temporárias e só entram quando não há prefab/material final configurado.

### Editor

- `PrototypeSceneGenerator`: cria/abre a cena de protótipo sem apagar outras cenas do Build Settings.
- `ProjectValidator`: valida `WeaponData`, `EnemyData` e `LevelDefinition`, emitindo erros de configuração e alertas de orçamento mobile.

## Próximos pontos de extensão

1. Editor visual dedicado para `LevelDefinition`.
2. Raridades/modificadores de armas sem duplicar ScriptableObjects.
3. Inimigos elite e bosses sobre `ShootableTarget`/`EnemyAgent` ou componentes especializados.
4. Quality tiers com orçamento de horda/FX por aparelho.
5. Áudio por eventos/serviço em vez de chamadas espalhadas.
6. Input System quando o esquema final de controles estiver definido.
7. Smoke test automatizado da cena e testes de regras puras.

## Regra para a futura arte 3D

Mantenha o objeto raiz como gameplay e coloque modelo, Animator, rig e VFX em filhos. Não coloque lógica de dano, spawn, progressão ou save dentro de scripts de visual/animação.
