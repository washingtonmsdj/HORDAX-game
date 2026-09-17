# HORDAX — Authoring de conteúdo

O blockout continua funcionando sem nenhum asset de dados. A camada `Assets/HORDAX/Scripts/Data` existe para permitir que o projeto cresça sem transformar `PrototypeBootstrap` em um arquivo gigante de números hardcoded.

## LevelDefinition

Crie pelo menu do Project window:

`Create > HORDAX > Level Definition`

Um `LevelDefinition` contém:

- id/nome da fase;
- comprimento da pista;
- lista ordenada de passos;
- passos do tipo `Horde`, `Gate`, `Upgrade` e `Finish`.

Para usar uma definição, selecione o objeto `HORDAX Prototype Bootstrap` na cena e arraste o asset para o campo `Level Definition`. Se nenhum asset estiver atribuído, o jogo usa o nível blockout padrão.

### Horde

Cada passo de horda pode definir contagem, colunas, vida, velocidade e dano diretamente. Opcionalmente pode receber um `EnemyData`.

## EnemyData

Crie com:

`Create > HORDAX > Enemy Data`

O asset guarda os stats do inimigo e um `Visual Prefab` opcional. Isso permite trocar os cubos por um personagem final mantendo `EnemyAgent`, pooling, targeting e combate.

> Nesta fase existe um pool compartilhado pensado para um arquétipo visual principal. Quando o projeto tiver vários tipos simultâneos de inimigo, a próxima evolução será um pool por arquétipo/prefab.

## WeaponData

Crie com:

`Create > HORDAX > Weapon Data`

O asset guarda dano, cadência, alcance, velocidade do projétil e prefabs opcionais. `WeaponController.ApplyDefinition()` já aceita esse asset, deixando a progressão futura de armas desacoplada do código de tiro.

## Regra de arquitetura

Dados definem **o que** aparece e com quais números. Componentes runtime definem **como** aquilo se comporta. Prefabs/modelos definem **como** aquilo parece.

Essa separação é intencional para que a arte 3D possa ser refeita sem reescrever o loop do jogo.
