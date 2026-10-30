# AI Log – Greed Run

A running record of how AI was used to build this prototype. Lila asked for this, so it is kept honestly as work happens rather than written from memory at the end.

**How to use:** add one line per piece of work. Be specific about what *you* decided or changed.

| Date | Tool | What I asked it for | What I kept | What I changed / rejected myself, and why |
|---|---|---|---|---|
| 28 Oct | Claude | Research on Lila Games; 6 game ideas with pros, cons and build time | Recommendation of Greed Run | Chose Unity instead of the suggested Phaser (reason: _add yours_) |
| 28 Oct | Claude | Set up the GitHub repo (README, this log) | Repo structure | — |
| 28 Oct | ChatGPT / image AI | 4 concept images (gameplay, evac moment, Collector, stash) to decide the look | Angled top-down 3D, dark facility, neon + glow | Switched setting from space to a sci-fi facility (Alien Shooter as reference); noticed the player gets lost in the crowd → added a glowing ring under the player |
| 28 Oct | Claude | Step-by-step Unity setup: web build settings, arena, camera, lighting, Bloom | Settings and layout | AI's first camera settings showed no neon in portrait — caught it in testing and fixed the camera distance/lens |
| 28 Oct | Claude | PlayerController (keyboard + touch-drag) and CameraFollow scripts | Both scripts | Built the robot myself from cubes; play-tested and raised move speed from 7 to 8 |
| 30 Oct | Claude | Scripts: auto-shooting, bullets, enemy health, loot drops, loot trail, HUD, evac pad, stash, end screen | All, after play-testing | Tuned fire rate 4→6, range 12→15, loot drop 70%→60%, time between evacs 50→15s; built all the objects, materials and UI myself |
| 30 Oct | Claude | Off-screen evac indicator | Edge glow script | **Rejected the suggested arrow** – asked for the screen edges to glow towards the evac instead (fits the neon style, keeps the screen clean) |
| 30 Oct | Claude | Game feel: code-generated sound effects, debris bursts, screen shake, flashes | Effects and most sounds | Added my own purchased music + success/failure/evac sounds (Ovani Sound); removed the knock-off sound after testing (too noisy in big hordes) |
