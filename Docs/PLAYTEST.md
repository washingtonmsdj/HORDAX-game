# HORDAX — Playtest do blockout

## Objetivo desta versão

Validar o loop `correr → eliminar horda → quebrar gate → coletar upgrade → sobreviver até o fim` antes de investir na arte 3D final.

## Como testar

1. Abra o projeto com Unity 6000.0.23f1 ou compatível.
2. Abra `Assets/HORDAX/Scenes/Prototype.unity`.
3. Se a cena ainda não existir, use `HORDAX > Generate Prototype Scene`.
4. Pressione Play.
5. Controle lateralmente com A/D, setas, mouse arrastando ou touch.
6. O tiro é automático e escolhe alvos à frente.

## Checklist rápido

- o player avança sem input;
- arrastar para os lados não atravessa as grades da pista;
- inimigos aparecem antes de o player chegar na wave;
- tiros buscam inimigos/gates;
- inimigos mortos voltam para o pool em vez de serem destruídos;
- gates mostram HP e desaparecem em zero;
- encostar em gate vivo causa derrota;
- upgrades aumentam dano/cadência;
- HUD acompanha HP, kills e progresso;
- chegada encerra a fase em vitória.

## Tuning atual

- Wave 1: 30 inimigos;
- Wave 2: 48 inimigos;
- Wave 3: 72 inimigos;
- pool pré-aquecido: 96 inimigos;
- target: 60 FPS;

Estes valores são deliberadamente fáceis de localizar em `PrototypeBootstrap.cs` e `EnemyPool.cs` para acelerar iterações.

## Arte futura

O gameplay não depende dos cubos. Substitua o visual do inimigo por prefab/modelo posteriormente, mantendo `EnemyAgent`. O mesmo princípio vale para player, armas, gates, pickups e pista.
