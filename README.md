# cs2-blackjack
<img width="300" height="150" alt="image" src="https://github.com/user-attachments/assets/8690a73b-9562-4d91-9001-5020171240c8" /><img width="300" height="150" alt="image" src="https://github.com/user-attachments/assets/b146e068-d51d-4d75-8d1e-696f13540dee" /><img width="300" height="150" alt="image" src="https://github.com/user-attachments/assets/9e6f478d-ddb4-4bfe-9523-2e2590c3b278" />

A Counter-Strike 2 plugin that implements a Blackjack game, integrated with the StoreApi for credit-based betting. Players can start a game, hit, or stand using in-game commands, with a visual card display in the game UI.

## Features
- Start a Blackjack game with a specified bet amount using !blackjack or !bj.
- Hit (!hit) to draw a card or stand (!stand) to end your turn.
- Supports minimum and maximum bet limits configured via a JSON file.
- Displays player and dealer hands with card images in the game UI.
- Handles win, lose, and draw outcomes with appropriate credit rewards or deductions.
- Multi-language support with localization files (English, Turkish, Russian, German).

## Requirements
- [Store Plugin & Api](https://github.com/schwarper/cs2-store/)

## Configuration
The plugin uses a JSON configuration file (`config.json`) with the following options:
- **Prefix**: The chat prefix for plugin messages (default: "⌈ Blackjack ⌋").
- **MinimumBet**: The maximum allowed bet (default: 999999 credits).
- **MaximumBet**: The inimum allowed bet (default: 100 credits).

Example `Config File`:
```json
{
  "Prefix": "{blue}⌈ Blackjack ⌋",
  "MaximumBet": 999999,
  "MinimumBet": 100
}
```

## Commands
- `!blackjack <amount>` or `!bj <amount>`: Start a Blackjack game with the specified bet amount.
- `!hit`: Draw an additional card.
- `!stand`: End your turn and let the dealer play.

## Localization
The plugin supports multiple languages via JSON files in the `lang/` directory. Supported languages:
- English (`en.json`)
- Turkish (`tr.json`)
- Russian (`ru.json`)
- German (`de.json`)

Each file contains key-value pairs for in-game messages, such as error prompts, game status, and results.

## Author
- **varkit** (Discord: vulikit)
