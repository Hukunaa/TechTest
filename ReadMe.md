# Technical Test – Unity Prototype (Archero-inspired)

Hello! First of all, thank you very much for taking the time to review my technical test. I really enjoyed working on it and it was a fun and rewarding experience. With that said, let’s dive in!

---

## First things first, how to play?
### Open the project -> Open the GameScene -> Press Play!
### If, for any reason, some dark magic happened and the project won't work, there is a Gameplay video included with the project

## How I approached this test (development phases)

### Phase 1 — Reference study (≈ 30 minutes)
I started by studying **Archero** for around **30 minutes**, focusing on:
- how the character interacts with the environment
- how movement feels and responds to joystick input
- how and when the character attacks

One key observation is that **Archero feels very snappy**. Movement is essentially *binary* (no gradual acceleration/deceleration): the joystick direction is normalized, resulting in **constant movement speed**, whether the finger is near the joystick center or at the edge.

### Phase 2 — Core foundation: game loop + movement (≈ 1 hour)
With that reference in mind, I first created a simple **GameManager** for state control and observation (Idle / Running / Pause / RoundComplete / Dead).

Then I implemented the **Hero controller**, aiming to replicate Archero’s movement feel as closely as possible. Once movement was working, I added environment bounds so the player had a readable play area. I also recreated:
- the dual-color floor grid
- a simple toon-ish decor style

### Phase 3 — Combat design: weapons + conditions + animation integration (largest time block)
After movement and the environment were in place, I designed the combat system. My initial plan was to implement only what was explicitly requested and add extras afterward, but I noticed that the instructions didn’t mention enemies attacking.

I felt the prototype would be less satisfying if enemies were only “punching bags,” so I decided to support **enemy attacks** to create a more complete combat loop.

I then implemented the **hero weapon system**, which was one of the most time-consuming parts of the test. The main goals were:
- attacking only under the correct conditions (not moving, enemy in range)
- weapon switching and selection
- animation timing matching weapon hit timing
- keeping the system modular and easy to tweak

### Phase 4 — Shared animation controller (quick iteration)
To keep animation logic simple and consistent, I created an **AnimatorController** script that drives transitions via crossfades. Since the hero and enemies share similar animation setups, the same script is used on both.

### Phase 5 — UI weapon selection (quick iteration)
I added a basic weapon selection UI under the joystick to switch between the **three weapons** included in the prototype. This was straightforward: minimal UI wiring + calling the weapon switch methods.

### Phase 6 — Enemy controller + level loop (moderate)
I implemented the enemy controller using the same base class as the hero: **LivingUnit**, which provides shared “living entity” behavior such as:
- health and death conditions
- shared stats (speed/max speed)
- enable/disable logic

Enemies are simpler than the hero (single weapon, no movement, no weapon switching), but they still use the same **Weapon** type for modularity. This allows configuring enemies with different weapons easily later.

Finally, I implemented the **Level** script:
- spawns enemies on random grid cells with spacing constraints
- tracks enemy deaths
- notifies the GameManager when the level is completed

---

## The time I spent on each phase

This is an approximate breakdown:
- **Reference study (Archero):** ~30 minutes  
- **GameManager + player movement + environment bounds:** ~1h–1h30  
- **Hero combat system (weapons + conditions + timing + animation integration):** ~4h–5h  
- **Enemy combat + shared animation behavior:** ~1h–2h  
- **Level spawning + win condition:** ~1h  
- **Game feel / polish (camera follow, shakes, particles, trails, death feedback, health bars):** ~2h–3h  
- **Addressables integration + refactors / cleanup:** ~1h–2h  

Overall, I overshot the **10-hour** guideline by **a few hours**. The main reason is that I wanted the prototype to have a minimum of *game feel* and not just “systems that work.”

---

## The features that were difficult for me and why

### Combat timing + state coordination
The hardest part was making sure combat stayed consistent across multiple weapons:
- correct **attack conditions** (idle-only, enemy in range)
- correct **hit timing** (weapon hit delay aligned with animation)
- cancelling attacks safely when conditions change
- preventing state conflicts (movement vs attack vs hit reaction)

### Addressables usage across multiple entity types
Using Addressables for level, hero, enemies, and weapons adds real production value, but it also introduces:
- handle lifecycle management (release instances properly)
- async sequencing (ensuring game flow doesn’t start before content is loaded)
- clean restarts (destroy/reload level, reset hero)

---

## The features I think I could do better and how

### Improve `LivingUnit` architecture
Right now `LivingUnit` covers health and generic behavior, but movement and state handling still live mostly in specialized scripts.  
If I had more time, I’d push the architecture further so that:
- movement capabilities can optionally live in the base class (or a shared movement module)
- core “living entity” behavior is standardized
- derived classes focus only on entity-specific logic

### Improve state management
The hero currently manages a `HeroCombatState` inside `HeroCombatController`. It works, but long-term I’d prefer:
- a shared state system at the `LivingUnit` level (or a dedicated state machine component)
- cleaner separation between “state decision” and “animation playback”
- more robust transitions and clearer authority over which system can change state

### Enemy/Level modularity
The current Level is straightforward and effective, but I’d make it more modular by:
- having specialized Level variants inherit from a base `Level`
- allowing different spawn rules per level (enemy types, patterns, waves, boss rules)
- supporting progression by referencing the next level via Addressables (e.g., each level contains a reference to the next)

---

## What I would do if I could go a step further on this game

If I had time to expand the prototype, I would add:
- multiple levels and a basic progression system
- more enemy archetypes (e.g., projectile shooters, dashers, AOE attackers)
- dodge pressure (projectiles make the player reposition and increase tension)
- more variety in weapons and effects (status effects, knockback, pierce, etc.)
- basic meta progression (upgrades between rounds) to align even more with Archero-style gameplay

---

## Any additional comments

### Game feel / feedback
Game feel matters a lot to me, so I spent extra time adding feedback elements:
- smooth camera follow in a limited range
- camera shake on taking damage and on enemy death
- movement particles
- weapon trails on attack
- enemy death scale animation instead of instant disappearance
- health bars above units (plug-and-play prefab reading `LivingUnit` values)

### Joystick controller
For the joystick controller, I reused a script I’ve had for a long time and tweaked it slightly to match the needs of this project.

### About the enemy spawning key (bit packing)

In the Level spawning algorithm, I used a small bit-packing trick for fast occupancy checks.

The goal is to uniquely represent a grid cell **(x, z)** as a single **64-bit** value:
- **x** and **z** are **32-bit signed integers** (`int`)
- the packed key is a **64-bit signed integer** (`long`)

How it works:
- **x** is shifted left by **32 bits**, so it occupies the **upper 32 bits** of the 64-bit key  
- **z** is placed into the **lower 32 bits**  
- the two parts are combined with a **bitwise OR** so both halves are preserved  

**Why use `unchecked`?**  
It ensures the bit operations behave consistently even if intermediate values overflow in a checked context (some project settings enable overflow checking). For this kind of packing, overflow is expected/harmless because we only care about the final bit pattern.

**Why cast `z` to `uint`?**  
Because `z` can be negative. If you cast a negative `int` to `long` directly, it will **sign-extend** (fill the upper bits with `1`s), which could overwrite the portion where `x` is stored. Casting `z` to `uint` forces a **zero-extension** instead, meaning only the **lower 32 bits** are used, and the upper 32 bits remain untouched, exactly what we want when packing into the low half of the key.

### Addressables

Since Addressables were already included in the project and is a very powerful tool, I used it to load and instantiate:
- the level  
- the hero  
- enemies  
- weapons  

In a real production context, Addressables help reduce memory pressure, support more flexible loading, and can enable remote content delivery. The actual gains depend on the project scale and content volume, but it’s a solid foundation for production-style asset management.

### ScriptableObject weapons

Using ScriptableObjects for weapons is very convenient because it makes balancing and iteration fast:
- weapon stats can be tweaked without touching code  
- new weapons can be added by creating new assets  
- shared data stays consistent across systems (hero and enemies)

---

## Closing

I really hope I was able to satisfy your expectations with this test. I'm not perfect, no code is perfect, and everything can be improved or done better. I really praise learning as an individual but also as a team and I love to learn from my mistakes, to learn from others, and how I could do things better. I've put all my heart in this test, and I hope to be a future member of the team!

Thank you again for your time and consideration.

**Victor**
