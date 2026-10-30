# Greed Run

A survivor-like with an extraction twist, built as a prototype for the Lila Games Game Designer written test (Oct–Nov 2026).

**The idea in one line:** your character auto-shoots while hordes grow. Every 60 seconds an exit portal opens for a few seconds — leave now and keep your loot, or stay for more and risk losing everything.

- **Engine:** Unity 6.6 (6000.6.5f1), Web build
- **Play in browser:** https://greed-run-game.netlify.app
- **Status:** in development

## Project layout

| Folder | What's in it |
|---|---|
| `Assets/` | Everything in the game: scenes, scripts, prefabs |
| `Packages/`, `ProjectSettings/` | Unity settings (needed to open the project) |
| `docs/` | Design notes and the AI log |

## How AI was used

See [`docs/ai-log.md`](docs/ai-log.md) for a running log of which AI tools were used, for what, and what was changed by hand.

## Credits

- **Music and some sound effects** (background music, success, failure and evac sounds): [Ovani Sound](https://ovanisound.com), royalty-free licence (purchased). These files aren't included in this repository, because the licence covers using them in games, not redistributing the raw audio. A fresh copy of the project falls back to the built-in sounds below.
- **Other sound effects** (shoot, hit, pickup, knock-off, hurt and others in `Assets/Resources/Audio`): generated with code for this project.
- **Everything else** (models, materials, scripts): made for this project.
