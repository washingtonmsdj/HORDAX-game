# HORDAX — Progressão

A progressão atual ainda é de blockout, mas já está separada do visual.

## Fases

A campanha runtime contém cinco fases. A fase 1 começa desbloqueada e cada vitória libera a próxima.

Para cada fase o save mantém:

- número de conclusões;
- melhor score;
- maior número de kills;
- maior recompensa de moedas;
- melhor rating de 1 a 3 estrelas.

O rating é calculado por score. LevelDefinition possui thresholds de 2 e 3 estrelas.

## Economia

Moedas entram na wallet somente ao concluir uma fase. O valor da tentativa inclui moedas dos inimigos e bônus de conclusão.

As moedas podem ser gastas em:

- upgrades permanentes;
- desbloqueio de armas.

## Upgrades permanentes

O blockout possui:

- Vitality: aumenta HP máximo;
- Power: aumenta dano;
- Fire Rate: aumenta cadência.

Cada upgrade possui nível máximo, custo base, crescimento de custo e ganho por nível.

## Armory

Rifle é desbloqueado por padrão. SMG, Shotgun e Minigun usam preços de protótipo.

Uma arma desbloqueada pode ser equipada no front-end. O loadout escolhido é aplicado antes do início da fase, e pickups durante a fase continuam podendo trocar temporariamente a arma.

## Save

ProgressionService grava JSON em PlayerPrefs. A chave atual continua HORDAX_PROGRESS_V1 e os campos novos são inicializados de forma compatível quando faltam.

Antes de produção serão necessários versionamento formal do schema, migração e cloud save.
