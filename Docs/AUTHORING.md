# HORDAX — Authoring de conteúdo

O blockout continua funcionando sem nenhum asset de dados. A camada `Assets/HORDAX/Scripts/Data` permite crescer o projeto sem transformar o bootstrap em uma lista rígida de números e sem acoplar gameplay à arte 3D.

## LevelDefinition

Crie pelo menu do Project window:

`Create > HORDAX > Level Definition`

Um `LevelDefinition` contém id/nome da fase, comprimento da pista e uma lista ordenada de passos. Os passos disponíveis são `Horde`, `Gate`, `Upgrade`, `Weapon` e `Finish`.

Para usar uma definição, selecione o objeto `HORDAX Prototype Bootstrap` na cena e arraste o asset para o campo `Level Definition`. Se nenhum asset estiver atribuído, o jogo usa o nível blockout padrão criado por código.

### Horde

Cada passo de horda pode definir contagem, colunas, vida, velocidade e dano diretamente. Opcionalmente pode receber um `EnemyData`.

O `EnemyPool` agora mantém filas separadas por prefab. Isso significa que diferentes arquétipos visuais podem coexistir sem um inimigo reciclado voltar usando o mesh de outro arquétipo.

### Gate

Define a posição e o HP do bloco numerado. O gate recebe feedback visual, camera shake e efeito placeholder quando quebra.

### Upgrade

Aplica bônus incremental à arma atual: dano adicional e multiplicador de cadência. Esse passo não troca o arquétipo da arma.

### Weapon

Troca a arma do jogador. Há duas formas de configurar:

1. atribuir um `WeaponData` completo; ou
2. deixar o asset vazio e escolher um `Prototype Weapon` (`Rifle`, `SMG`, `Shotgun` ou `Minigun`).

A segunda opção é útil para continuar desenvolvendo o loop sem depender de assets finais.

## EnemyData

Crie com:

`Create > HORDAX > Enemy Data`

O asset guarda vida, velocidade, dano de contato e um `Visual Prefab` opcional. Assim, cubos podem ser substituídos por personagens finais mantendo `EnemyAgent`, pooling, targeting e combate.

## WeaponData

Crie com:

`Create > HORDAX > Weapon Data`

O asset guarda:

- id e nome de exibição;
- arquétipo;
- dano;
- cadência;
- alcance;
- velocidade do projétil;
- projéteis por disparo;
- spread;
- recoil visual;
- escala do projétil;
- prefab visual opcional;
- prefab de projétil opcional.

`WeaponController.ApplyDefinition()` aplica o asset completo em runtime. `ApplyPrototype()` fornece presets de blockout para Rifle, SMG, Shotgun e Minigun.

## Validação

Use:

`HORDAX > Validate Project Data`

O validador procura configurações impossíveis e também alerta sobre números que provavelmente precisam de profiling mobile, como hordas muito grandes e armas emitindo projéteis demais por segundo.

## Regra de arquitetura

Dados definem **o que** aparece e com quais números. Componentes runtime definem **como** aquilo se comporta. Prefabs/modelos definem **como** aquilo parece.

Essa separação é intencional: a arte 3D pode ser refeita sem reescrever corrida, combate, progressão ou lógica de horda.
