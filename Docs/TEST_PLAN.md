# Verification plan

## Coverage target

Exercise every implemented grading action in all three games, rerun the original gameplay checks after import, and test repeated switching for shared-state leaks. UI appearance and actual standalone Exit require a native application check in addition to editor automation.

| Area | Check type | Required cases |
| --- | --- | --- |
| Startup/main menu | Integration + visual | Startup scene, correct title/author, three game buttons, Exit, visible readable layout |
| Game launch | Integration + native smoke | Each button loads the correct class scene with a camera/player and no missing scripts |
| Gameplay | Real-scene physics/input checks | Spawning, movement, collision callbacks, cleanup, balloon limits/game over, arena turbo/powerup/waves |
| Pause | Input + state integration | Escape in every game, PAUSED UI, time scale 0, audio paused, gameplay scripts/input suspended |
| Resume | Button integration | Same run continues, time/audio/input restored, overlay closes |
| Restart | Button + fresh-state integration | New player/scene state, same game, balloon gameOver false, arena first-wave state |
| Return | Button integration | Main menu from every game, no stale pause or audio state |
| Repeated switching | Integration | Balloon → arena/dogs, correct gravity, one controller/event system, Escape toggles correctly |
| Exit | Native UI | Main-menu Exit closes the standalone application |

## Automated implementation

`ArcadeTests.Run` starts the real MainMenu scene. `ArcadeIntegrationProbe` invokes actual menu button callbacks and uses an Input System test keyboard for Escape and the newer gameplay input. It places objects into contact to test Unity's actual collision/trigger callbacks deterministically. The balloon lift test sets its existing held-Space state directly, while its real keyboard path is checked in the native smoke test.

The probe captures Error/Exception/Assert logs, checks completion with a real-time timeout, and writes JSON evidence. Tests and their UnityEditor references are excluded from the standalone player.

## Limits

These are focused integration tests and short native playtests, not exhaustive long-session, every-resolution, every-platform or performance tests. Menu appearance is checked on macOS at the delivered window size. Passing software tests does not guarantee the lecturer accepts the chosen game substitution, video completeness, deadline interpretation or final submission.
