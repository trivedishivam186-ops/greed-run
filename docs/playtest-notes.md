# Playtest Notes – Greed Run

Notes from playing the prototype, and what was changed because of them. Used for the Q1 writeup ("what I'd test first", iteration).

## Playtest 1 – Fri 30 Oct (me, in editor)

**Build:** movement, auto-shooting, enemy waves, loot drops + trail, evac pad (extract / skip / die).

| # | What I noticed | Why it matters | What I changed / plan |
|---|---|---|---|
| 1 | Horde keeps growing but my gun stays the same, so runs slowly become unwinnable | Power has to grow with the threat, or there's no reason to stay longer | **Planned:** level-up choices during the run (fire rate, damage, spread…) |
| 2 | Tuned weapon: fire rate 4 → 6, range 12 → 15 | Felt too weak against later waves | Changed |
| 3 | Loot drop chance 70% felt slightly generous | Loot should feel earned, so carrying a lot means something | Drop chance 70% → 60% |
| 4 | Real tension when the evac opens – enemies can overrun you while you stand on the pad | This is the core "greed" moment working as intended | Keep |
| 5 | Hard to survive until the next evac with the basic gun | Same root cause as #1 | Short-term: time between evacs 50s → 15s. Revisit once level-ups exist |
| 6 | From a corner I struggled to find the evac and barely made it | The choice should be "leave or stay", not "where is it?" | **Planned:** on-screen arrow pointing to the evac pad |
| 7 | Banked loot shows a number but has no use yet, so extracting doesn't feel rewarding | The reward has to matter for greed to be a real decision | **Planned:** stash upgrade shop |
| 8 | Fun, but missing sounds, effects and rewards | Game feel makes simple shapes satisfying | **Planned:** game feel pass (shake, flashes, explosions, sounds) |
| 9 | The 1-second hold on the pad is good – you still have to survive the last second | Makes leaving a commitment | Keep |

## Playtest 2 – Fri 30 Oct (me, in editor)

**Build:** + level-up cards, game feel (sounds, debris, shake), evac edge glow.

| # | What I noticed | Why it matters | What I changed |
|---|---|---|---|
| 1 | Level-ups come at a good pace and are useful | Power now grows with the threat | Keep |
| 2 | I can survive much longer than before | Fixes "the horde outgrows my gun" from playtest 1 | Keep |
| 3 | Magnet and Heavy Rounds feel weak | Heavy Rounds +25% (10→12.5) didn't change hits-to-kill on 20 HP enemies, so you couldn't feel it | Heavy Rounds +25% → +50% (one card = 15, two = 22.5 → one-shot kills). Magnet +30% → +60%, and loot now *flies* to you inside the range so the upgrade is visible. Added Piercing Rounds (bullets pass through enemies) |
| 4 | Late waves become one huge blob with no release | Constant pressure with no peaks and valleys gets exhausting | Added a rare **Nuke** pickup (inspired by Call of Duty Zombies) that wipes every enemy – and drops a pile of loot, which feeds the greed decision |
| 5 | Waves blur together with no breathing space | Players need moments to breathe, collect loot and decide whether to evac | Waves now end only when every enemy is dead, followed by a 5-second breather before the next, bigger wave |

## Playtest 3 – Fri 30 Oct (me, in editor)

**Build:** + waves with breathers, nuke, personal bests, stash shop (start screen) with permanent upgrades and Black Box.

| # | What I noticed | Why it matters | What I changed / plan |
|---|---|---|---|
| 1 | Shop works and extracting now feels rewarding | Banked loot finally has a use – the run → bank → upgrade loop is complete | Keep |
| 2 | Difficulty is about right for now, but if I play well, surviving becomes easy | One enemy type means one answer: once your build handles the swarm, nothing new tests you | **Planned:** more enemy types (fast and tanky) and the Collector, an AI-Director enemy that targets greedy players' loot |

## Playtest 4 – Sun 1 Nov (me, in editor)

**Build:** + Runner and Brute enemies, Collector enemy sent by a rule-based "AI Director".

| # | What I noticed | Why it matters | What I changed |
|---|---|---|---|
| 1 | Still too easy, even with Runners and Brutes | Enemy types differed in style, but not in threat – and none of them got tougher over time | Enemies now get +12% health and +5% damage every wave, and each type is a clear step up (Grunt < Runner < Brute < Boss) |
| 2 | The Collector didn't work: fast, small and weak, and losing ~80 loot to it felt like nothing | A threat has to be scary *relative to what you're carrying*; a small thief isn't | **Cut the Collector.** Replaced with a **Titan boss every 5th wave** (reference: Helldivers 2) – big health bar, a telegraphed charge attack, drops a big loot pile when killed |
| 3 | I'd rather see that I'm carrying loot than drag a trail | The trail got long and noisy; what matters is "you have it, you lose it if you die" | Loot now **orbits the robot** (up to 10 cores visible; the orbit spins faster the richer you are). Enemies no longer knock loot off – it's only lost on death, and the Black Box saves part of it |

## Playtest 5 – Sun 1 Nov (me, in editor)

**Build:** + Threat Levels on the start screen.

| # | What I noticed | Why it matters | What I changed |
|---|---|---|---|
| 1 | Shop upgrades cap at level 5 – once maxed, banked loot has nothing left to buy | The greed loop dies when the reward stops meaning anything | Added **Threat Levels** (reference: Helldivers difficulty tiers): +30% enemy health/damage and +50% loot per level, unlocked by extracting after wave 5. Progression moves from buying to mastery |
| 2 | Now the game gets harder after a few waves, and I have to choose carefully which upgrades to take | Choices only matter when you can't have everything – this is the build decision working | Keep |
