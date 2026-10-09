# Delivered verification

Verified on 9 October 2026 using Unity **6000.0.79f1** on an Apple M2 Mac.

## Results

| Check | Result |
| --- | --- |
| Separate classroom-game checks before integration | 37/37 passed |
| Combined project integration checks | **109/109 passed** |
| Captured Unity errors, exceptions and assertions during integration checks | **0** |
| Final macOS player build | **Succeeded: 0 errors, 0 warnings** |
| Asset identity/reference audit | 178 unique asset GUIDs; no duplicates or unresolved asset references |
| Source files exceeding 90 MiB | None |
| Native application Exit | `ARCADE_EXIT_REQUESTED` logged; application stopped |

The machine-readable combined results are in [integration-results.json](integration-results.json). The tests cover the actual classroom scenes, assigned prefabs, movement/spawning/collisions, balloon ground bounce and bomb game-over, arena powerup/wave behavior, menu launches, keyboard navigation, Escape pause, frozen physics/input/audio, Resume, fresh Restart, Back to Main Menu, repeated transitions and global-state cleanup.

An initial arena test expected the old wave counter after a wave had already advanced. The assertion was corrected to match the original classroom implementation; arena gameplay was not changed to satisfy that test. The full suite was rerun successfully after the final menu-navigation change.

## Native application smoke checks

The standalone macOS application was launched and visually inspected. All three game choices opened the appropriate rendered classroom scenes. Short keyboard interactions exercised gameplay and menu navigation. The main menu displayed the title, three choices, Exit, and **Zwe Khant Lin · 6632710**. The pause overlay displayed **PAUSED**, Resume, Restart and Back to Main Menu. Native Resume, Restart, Back and Exit were exercised in addition to the automated scene checks. The final build was checked in a large 2560×1596 game window; the default window is 1280×720.

The final native player log contained no Unity exception/error stack traces. macOS/Mono emitted lifecycle notices about restorable-state secure coding and thread finalization on shutdown; these are distinct from the build's zero-warning result and did not prevent the tested interactions or application exit.

These are short smoke checks and deterministic integration tests, not exhaustive long-session playtesting on every device or resolution. Automated collision cases reposition existing scene objects to make outcomes repeatable. The balloon lift test sets the classroom controller's held-Space state; separate native interaction checks exercise the real keyboard path. No test-only controller is included in player builds.

## Changes made for compatibility

- Challenge 2: repaired the legacy-input/new-Input-System mismatch in the copied classroom controller.
- Challenge 3: restored the missing Ground tag and assigned it to the floor so the original bounce behavior works.
- Integration: isolated class scripts and asset IDs; added shared menus, pause/input suspension, scene-state resets, author credit and build configuration.
- Original classroom project folders were left unchanged.

## Remaining submission and grading boundaries

Challenge 2 is the student's selected dogs-and-balls substitution for the driving game named in the paper. Technical test success does **not** guarantee that the lecturer accepts this genre substitution.

The student confirmed that continuous screen recording was running. This report does not verify the complete recording or claim that it has been submitted. Follow [DEMONSTRATION.md](DEMONSTRATION.md), retain the actual continuous recording, and submit it together with the published repository link through the required class channel. Use the lecturer-confirmed deadline; the printed paper and the student-reported updated deadline differ.
