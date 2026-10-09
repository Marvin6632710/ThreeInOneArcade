# Arcade Trio

A three-in-one Unity arcade by **Zwe Khant Lin · 6632710**, built for the GDD take-home final. One main menu launches three reused classroom games. Each game shares an Escape pause menu with Resume, Restart and Back to Main Menu.

## Play

Open the separately supplied **Arcade Trio.app** on macOS. Click the window to focus it, then choose a game. Exit on the main menu closes the application.

| Menu choice | Classroom source | Controls |
| --- | --- | --- |
| Dog Patrol | Challenge 2 | W/S or Up/Down: move. Space: send a dog to catch falling balls. |
| Fly Like a Balloon | Challenge 3 | Hold Space: rise. Release: descend. Collect money and avoid bombs. |
| Sumo Ball Arena | Challenge 4 | W/S or Up/Down: move. A/D or Left/Right: steer. Space: turbo. Push enemy balls toward the goal. |

In any game, **Escape** opens the pause menu. The world, gameplay input and audio pause together. Resume continues the same run; Restart reloads that game from a fresh state; Back to Main Menu lets you select a different game. Escape also resumes a paused game. Menu buttons support a mouse or Up/Down and Enter.

After a balloon bomb collision, an on-screen hint directs you to Escape → Restart. The class arena continues into larger, faster enemy waves. The dog game preserves the classroom's simple catch-and-cleanup gameplay.

## Open the source in Unity

1. Install/use **Unity 6000.0.79f1**.
2. In Unity Hub, choose **Add → Add project from disk** and select this repository's root folder.
3. Wait for Unity to import the assets and restore the Input System and Unity UI packages.
4. Open `Assets/Arcade/Scenes/MainMenu.unity` and press Play.

The build scene order is MainMenu, Challenge 2, Challenge 3, Challenge 4. The menu item **Arcade → Prepare Main Menu and Build Settings** restores those settings. **Arcade → Build macOS Game** creates `Build/Arcade Trio.app`; this generated application is not stored in Git.

## Project organization

- `Assets/Arcade/Scripts/`: game catalog, shared menus, pause and scene transitions.
- `Assets/Arcade/Scenes/MainMenu.unity`: startup scene.
- `Assets/Arcade/Editor/`: repeatable setup/build and integration-check entry points.
- `Assets/Games/Dogs/`, `Balloon/`, `Arena/`: reused class assets and scripts.
- `Assets/Tests/ArcadeIntegrationProbe.cs`: editor-only integration checks; excluded from player builds.
- `Docs/`: test plan, demonstration checklist and reuse/AI disclosure.

The class scripts retain their filenames and Unity references, but live in separate namespaces to avoid collisions. Each game's asset GUIDs were remapped consistently so similarly named classroom assets cannot overwrite each other. Global gravity, time scale and audio state are reset on scene transitions. Pause suspends gameplay scripts as well as physics, preventing Space from triggering gameplay through an open menu.

## Verification

Choose **Arcade → Run Integration Checks** in Unity. This enters Play Mode, exercises the menu actions and gameplay, then writes a machine-readable report to `TestResults/integration-results.json` and exits the editor. Run it after saving work. For the delivered verification results, see `Docs/VALIDATION.md`.

## Assignment scope and disclosure

The exam paper names driving, flying and sumo games. The student explicitly selected **Challenge 2 (dogs and balls)** for the first slot instead of a driving game, with Challenge 3 and Challenge 4 for the other slots. This implementation labels the actual games honestly; it does not claim that the first game is a driving game. Acceptance of that substitution is a grading decision, not a technical guarantee.

Classroom assets and gameplay are reused for this coursework; see `Docs/CREDITS.md`. AI assistance was used for integration, menu implementation, debugging, testing and documentation, as permitted by the provided brief. No general open-source license is asserted over the instructor-provided/Unity classroom assets.

The screen recording and repository submission are separate deliverables. Keep the continuous work recording, include the final commit/push process and demonstrate every feature at its end. Do not change the published source after the confirmed exam deadline.
