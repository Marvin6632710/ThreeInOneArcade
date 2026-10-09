#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Arcade;

public class ArcadeIntegrationProbe : MonoBehaviour
{
    [Serializable] public class Check { public string name; public bool passed; public string detail; }
    [Serializable] public class Report {
        public string project; public string mode; public bool passed;
        public List<Check> checks = new List<Check>();
        public List<string> errors = new List<string>();
    }
    public string mode = "combined integration";
    private Report report;
    private Keyboard keyboard;
    private Key[] heldKeys = new Key[0];
    private MonoBehaviour legacyPlayer;
    private bool lift;
    private bool finished;
    private float started;

    private IEnumerator Start()
    {
        started = Time.realtimeSinceStartup;
        Application.runInBackground = true;
        report = new Report { project = Application.productName, mode = mode };
        Application.logMessageReceived += OnLog;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard = InputSystem.AddDevice<Keyboard>();
        keyboard.MakeCurrent();
        yield return null;
        Record("Main camera exists", Camera.main != null);
        int missing = 0;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
        Record("Scene has no missing scripts", missing == 0, missing.ToString());
        IEnumerator test = Integration();
        while (true) {
            bool next;
            object current = null;
            try { next = test.MoveNext(); if (next) current = test.Current; }
            catch (Exception e) { report.errors.Add(e.ToString()); break; }
            if (!next) break;
            yield return current;
        }
        Finish();
    }

    private void Update()
    {
        if (keyboard != null) InputSystem.QueueStateEvent(keyboard, new KeyboardState(heldKeys));
        if (!finished && report != null && Time.realtimeSinceStartup - started > 240) {
            Record("Verification completed within timeout", false); Finish();
        }
    }
    private void LateUpdate()
    {
        if (legacyPlayer != null) {
            var field = legacyPlayer.GetType().GetField("spaceHeld", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) field.SetValue(legacyPlayer, lift);
        }
    }
    private void OnLog(string message, string stack, LogType type)
    {
        if ((type == LogType.Exception || type == LogType.Error || type == LogType.Assert) && report.errors.Count < 20)
            report.errors.Add(message + "\n" + stack);
    }
    private void Record(string name, bool passed, string detail = "")
    {
        report.checks.Add(new Check { name = name, passed = passed, detail = detail });
        Debug.Log("GAMEPLAY CHECK " + (passed ? "PASS " : "FAIL ") + name + " " + detail);
    }
    private MonoBehaviour Script(string objectName, string typeName)
    {
        return GameObject.Find(objectName).GetComponents<MonoBehaviour>().First(c => c.GetType().Name == typeName);
    }
    private T Field<T>(object target, string name)
    {
        return (T)target.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    }
    private void SetField(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }
    private void Keys(params Key[] keys) { heldKeys = keys; }
    private IEnumerator Loaded()
    {
        float deadline = Time.realtimeSinceStartup + 20;
        do { yield return null; } while (ArcadeApp.Instance.IsTransitioning && Time.realtimeSinceStartup < deadline);
        yield return new WaitForSecondsRealtime(.15f);
        Record("Scene transition completed", !ArcadeApp.Instance.IsTransitioning);
    }

    private IEnumerator Escape()
    {
        Keys(Key.Escape);
        yield return new WaitForSecondsRealtime(.1f);
        Keys();
        yield return null;
    }

    private IEnumerator Integration()
    {
        var app = ArcadeApp.Instance;
        Record("Main menu is the starting scene", app != null && app.IsMainMenu && app.MainMenuRoot.activeSelf);
        Record("Main menu has three game choices and Exit", app.GameButtons.All(b => b != null && b.interactable) && app.ExitButton != null && app.ExitButton.interactable);
        var menuText = app.MainMenuRoot.GetComponentsInChildren<Text>().Select(t => t.text).ToArray();
        Record("Menu displays game title and correct author", menuText.Contains(GameCatalog.Title) && menuText.Any(t => t.Contains("Zwe Khant Lin") && t.Contains("6632710")));
        Keys(Key.DownArrow); yield return new WaitForSecondsRealtime(.1f); Keys(); yield return new WaitForSecondsRealtime(.05f);
        Record("Main menu Down selects second game", EventSystem.current.currentSelectedGameObject == app.GameButtons[1].gameObject);
        Keys(Key.UpArrow); yield return new WaitForSecondsRealtime(.1f); Keys(); yield return new WaitForSecondsRealtime(.05f);
        Record("Main menu Up selects first game", EventSystem.current.currentSelectedGameObject == app.GameButtons[0].gameObject);
        for (int i = 0; i < 3; i++) {
            app.GameButtons[i].onClick.Invoke();
            yield return Loaded();
            Record("Game button loads playable scene " + (i + 1), SceneManager.GetActiveScene().path == GameCatalog.Scenes[i] && Camera.main != null && GameObject.Find("Player") != null);
            int missing = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            Record("Merged scene " + (i + 1) + " has no missing scripts", missing == 0);
            Record("One shared controller and event system in game " + (i + 1), FindObjectsByType<ArcadeApp>(FindObjectsSortMode.None).Length == 1 && FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1);
            yield return i == 0 ? Dogs() : i == 1 ? Balloon() : Arena();
            yield return Escape();
            Record("Escape pauses game " + (i + 1), app.IsPaused && app.PauseRoot.activeSelf && Time.timeScale == 0 && AudioListener.pause);
            var pauseText = app.PauseRoot.GetComponentsInChildren<Text>().Select(t => t.text).ToArray();
            Record("Pause menu includes PAUSED and all three actions", pauseText.Contains("PAUSED") && pauseText.Contains("Resume") && pauseText.Contains("Restart") && pauseText.Contains("Back to Main Menu"));
            Keys(Key.DownArrow); yield return new WaitForSecondsRealtime(.1f); Keys(); yield return new WaitForSecondsRealtime(.05f);
            Record("Pause Down selects Restart " + (i + 1), EventSystem.current.currentSelectedGameObject == app.RestartButton.gameObject);
            Keys(Key.UpArrow); yield return new WaitForSecondsRealtime(.1f); Keys(); yield return new WaitForSecondsRealtime(.05f);
            Record("Pause Up selects Resume " + (i + 1), EventSystem.current.currentSelectedGameObject == app.ResumeButton.gameObject);
            var gameplay = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(b => b.GetType().Namespace != null && b.GetType().Namespace.StartsWith("Arcade.Games.")).ToArray();
            Record("Gameplay input scripts suspended during pause " + (i + 1), gameplay.All(b => !b.enabled));
            Vector3 playerPosition = GameObject.Find("Player").transform.position;
            int objects = SceneManager.GetActiveScene().GetRootGameObjects().Length;
            Keys(Key.Space); yield return new WaitForSecondsRealtime(.4f); Keys();
            Record("Pause freezes physics and prevents Space gameplay " + (i + 1), Vector3.Distance(playerPosition, GameObject.Find("Player").transform.position) < .001f && SceneManager.GetActiveScene().GetRootGameObjects().Length == objects);
            app.ResumeButton.onClick.Invoke(); yield return new WaitForSecondsRealtime(.15f);
            Record("Resume button restores time audio and controls " + (i + 1), !app.IsPaused && !app.PauseRoot.activeSelf && Time.timeScale == 1 && !AudioListener.pause && Script("Player", "PlayerControllerX").enabled);
            yield return Escape();
            int oldPlayerId = GameObject.Find("Player").GetInstanceID();
            app.RestartButton.onClick.Invoke(); yield return Loaded();
            Record("Restart reloads current game " + (i + 1), SceneManager.GetActiveScene().path == GameCatalog.Scenes[i] && GameObject.Find("Player").GetInstanceID() != oldPlayerId && !app.IsPaused && Time.timeScale == 1 && !AudioListener.pause);
            if (i == 1) Record("Restart clears balloon game-over state", !Field<bool>(Script("Player", "PlayerControllerX"), "gameOver"));
            // The class spawner increments waveCount immediately after spawning,
            // so a live first wave has one enemy and a next-wave counter of two.
            if (i == 2) Record("Restart resets arena to first wave", GameObject.FindGameObjectsWithTag("Enemy").Length == 1 && Field<int>(Script("Spawn Manager", "SpawnManagerX"), "waveCount") == 2 && Field<float>(Script("Spawn Manager", "SpawnManagerX"), "enemySpeed") == 30);
            yield return Escape();
            app.BackButton.onClick.Invoke(); yield return Loaded();
            Record("Back button returns from game " + (i + 1), app.IsMainMenu && app.MainMenuRoot.activeSelf && !app.PauseRoot.activeSelf && Time.timeScale == 1 && !AudioListener.pause);
            Record("Main menu gravity reset after game " + (i + 1), Vector3.Distance(Physics.gravity, Vector3.down * 9.81f) < .001f);
        }
        // Re-enter in a different order to expose shared-state leaks.
        foreach (int i in new[] { 1, 2, 0, 2 }) {
            app.GameButtons[i].onClick.Invoke(); yield return Loaded();
            float gravity = i == 1 ? 14.715f : 9.81f;
            Record("Repeated entry has correct physics: " + GameCatalog.Titles[i], Mathf.Abs(Physics.gravity.y + gravity) < .001f && Time.timeScale == 1 && !AudioListener.pause);
            Record("Repeated entry has one menu controller", FindObjectsByType<ArcadeApp>(FindObjectsSortMode.None).Length == 1 && FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 1);
            yield return Escape();
            yield return Escape();
            Record("Escape toggles pause and resume", !app.IsPaused && Time.timeScale == 1);
            app.PauseGame(); app.BackButton.onClick.Invoke(); yield return Loaded();
        }
        Record("Integration run ends at working Main Menu", app.IsMainMenu && app.MainMenuRoot.activeSelf);
    }
    private void ClearClones()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name.Contains("(Clone)")) Destroy(root);
    }
    private GameObject Overlap(GameObject prefab, GameObject player)
    {
        var obj = Instantiate(prefab, player.transform.position, prefab.transform.rotation);
        foreach (var script in obj.GetComponentsInChildren<MonoBehaviour>())
            if (script.GetType().Name != "DetectCollisionsX") script.enabled = false;
        var rb = obj.GetComponent<Rigidbody>();
        if (rb != null) { rb.useGravity = false; rb.linearVelocity = Vector3.zero; }
        Physics.SyncTransforms();
        var a = player.GetComponentsInChildren<Collider>().First(c => c.enabled);
        var b = obj.GetComponentsInChildren<Collider>().First(c => c.enabled);
        obj.transform.position += a.bounds.center - b.bounds.center;
        Physics.SyncTransforms();
        return obj;
    }
    private IEnumerator Dogs()
    {
        var player = Script("Player", "PlayerControllerX");
        var spawn = Script("Spawn Manager", "SpawnManagerX");
        var dogPrefab = Field<GameObject>(player, "dogPrefab");
        var balls = Field<GameObject[]>(spawn, "ballPrefabs");
        Record("Dog and all ball prefabs assigned", dogPrefab != null && balls.Length == 3 && Array.TrueForAll(balls, b => b != null));
        yield return new WaitForSecondsRealtime(1.4f);
        var spawnedBall = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .FirstOrDefault(s => s.GetType().Name == "MoveForwardX" && s.gameObject.name.StartsWith("Ball"));
        Record("Balls spawn automatically", spawnedBall != null);
        if (spawnedBall != null) {
            float y = spawnedBall.transform.position.y;
            yield return new WaitForSecondsRealtime(.25f);
            Record("Spawned balls fall toward the play area", spawnedBall != null && spawnedBall.transform.position.y < y);
        }
        Vector3 start = player.transform.position;
        Keys(Key.W); yield return new WaitForSecondsRealtime(.4f); Keys();
        Record("Forward input moves player", player.transform.position.x < start.x - .5f, player.transform.position.ToString());
        Keys(Key.S); yield return new WaitForSecondsRealtime(1.5f); Keys();
        Record("Player stays within upper movement boundary", player.transform.position.x <= 17.01f);
        Keys(Key.W); yield return new WaitForSecondsRealtime(2); Keys();
        Record("Player stays within lower boundary and depth plane", player.transform.position.x >= 7.99f && Mathf.Abs(player.transform.position.z) < .05f);
        int before = GameObject.FindGameObjectsWithTag("Dog").Length;
        Keys(Key.Space); yield return new WaitForSecondsRealtime(.1f); Keys();
        var dogs = GameObject.FindGameObjectsWithTag("Dog");
        Record("Space launches a dog", dogs.Length > before);
        if (dogs.Length > 0) {
            var dog = dogs[dogs.Length - 1]; float x = dog.transform.position.x;
            yield return new WaitForSecondsRealtime(.3f);
            Record("Launched dog runs left", dog != null && dog.transform.position.x < x);
            foreach (var s in dog.GetComponents<MonoBehaviour>()) s.enabled = false;
            var ball = Overlap(balls[0], dog);
            yield return new WaitForSecondsRealtime(.2f);
            Record("Dog catches and removes ball through real trigger collision", ball == null);
        }
        var missed = Instantiate(balls[0], new Vector3(0, -9, 0), balls[0].transform.rotation);
        yield return new WaitForSecondsRealtime(.2f);
        Record("Missed balls are cleaned up", missed == null);
        var runaway = Instantiate(dogPrefab, new Vector3(-40, 0, 0), dogPrefab.transform.rotation);
        yield return new WaitForSecondsRealtime(.2f);
        Record("Offscreen dogs are cleaned up", runaway == null);
    }
    private IEnumerator Balloon()
    {
        var player = Script("Player", "PlayerControllerX");
        var spawn = Script("Spawn Manager", "SpawnManagerX");
        var prefabs = Field<GameObject[]>(spawn, "objectPrefabs");
        var rb = player.GetComponent<Rigidbody>();
        Record("Balloon physics and obstacle prefabs assigned", rb != null && prefabs.Length == 2 && Array.TrueForAll(prefabs, p => p != null));
        yield return new WaitForSecondsRealtime(2.3f);
        Record("Bombs or money spawn automatically", GameObject.FindGameObjectsWithTag("Bomb").Length + GameObject.FindGameObjectsWithTag("Money").Length > 0);
        spawn.CancelInvoke(); ClearClones(); yield return null;
        legacyPlayer = player;
        rb.position = new Vector3(-3, 4, 0); rb.linearVelocity = Vector3.zero;
        lift = true; Keys(Key.Space);
        yield return new WaitForSecondsRealtime(.5f);
        Record("Lift force raises balloon", rb.position.y > 5, rb.position.ToString());
        yield return new WaitForSecondsRealtime(2);
        Record("Balloon obeys height ceiling", rb.position.y <= 13.05f && rb.position.y > 11, rb.position.ToString());
        lift = false; Keys(); float high = rb.position.y;
        yield return new WaitForSecondsRealtime(.5f);
        Record("Balloon falls when lift is released", rb.position.y < high - .4f);
        rb.position = new Vector3(-3, 4, 0); rb.linearVelocity = Vector3.zero;
        yield return new WaitForSecondsRealtime(1.8f);
        Record("Ground bounce keeps balloon in play", rb.position.y > 1, rb.position.ToString());
        rb.position = new Vector3(-3, 8, 0); rb.linearVelocity = Vector3.zero;
        var moneyPrefab = prefabs.First(p => p.CompareTag("Money"));
        var money = Overlap(moneyPrefab, player.gameObject);
        yield return new WaitForSecondsRealtime(.2f);
        Record("Collecting money removes it without ending game", money == null && !Field<bool>(player, "gameOver"));
        var bombPrefab = prefabs.First(p => p.CompareTag("Bomb"));
        var bomb = Overlap(bombPrefab, player.gameObject);
        yield return new WaitForSecondsRealtime(.2f);
        Record("Bomb collision ends game and removes bomb", bomb == null && Field<bool>(player, "gameOver"));
        float bgX = GameObject.Find("Background").transform.position.x;
        spawn.Invoke("SpawnObjects", .1f);
        yield return new WaitForSecondsRealtime(.3f);
        Record("Scrolling and spawning stop after game over", Mathf.Abs(GameObject.Find("Background").transform.position.x - bgX) < .01f && GameObject.FindGameObjectsWithTag("Bomb").Length + GameObject.FindGameObjectsWithTag("Money").Length == 0);
        legacyPlayer = null;
    }
    private IEnumerator Arena()
    {
        var player = Script("Player", "PlayerControllerX");
        var spawn = Script("Spawn Manager", "SpawnManagerX");
        var rb = player.GetComponent<Rigidbody>();
        var focal = GameObject.Find("Focal Point");
        yield return new WaitForSecondsRealtime(.3f);
        Record("First enemy wave and powerup spawn", GameObject.FindGameObjectsWithTag("Enemy").Length == 1 && GameObject.FindGameObjectsWithTag("Powerup").Length == 1);
        var enemy = GameObject.FindGameObjectsWithTag("Enemy")[0];
        float distance = Vector3.Distance(enemy.transform.position, GameObject.Find("Player Goal").transform.position);
        yield return new WaitForSecondsRealtime(.5f);
        Record("Enemy advances toward player goal", Vector3.Distance(enemy.transform.position, GameObject.Find("Player Goal").transform.position) < distance);
        enemy.GetComponents<MonoBehaviour>().First(s => s.GetType().Name == "EnemyX").enabled = false;
        enemy.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        Vector3 p = rb.position;
        Keys(Key.W); yield return new WaitForSecondsRealtime(.5f); Keys();
        Record("Forward input moves arena player", rb.position.z > p.z + .5f, rb.position.ToString());
        Quaternion q = focal.transform.rotation;
        Keys(Key.D); yield return new WaitForSecondsRealtime(.3f); Keys();
        Record("Steering rotates camera focal point", Quaternion.Angle(q, focal.transform.rotation) > 15);
        rb.linearVelocity = Vector3.zero;
        Keys(Key.Space); yield return new WaitForSecondsRealtime(.08f); Keys();
        Record("Space turbo applies forward impulse", rb.linearVelocity.magnitude > 5, rb.linearVelocity.ToString());
        // Make the pickup deterministic but keep Unity's actual trigger callback.
        var powerup = GameObject.FindGameObjectsWithTag("Powerup")[0];
        rb.position = new Vector3(0, .6f, 0); rb.linearVelocity = Vector3.zero;
        Physics.SyncTransforms();
        var targetCollider = player.GetComponent<Collider>();
        var pickupCollider = powerup.GetComponent<Collider>();
        powerup.transform.position += targetCollider.bounds.center - pickupCollider.bounds.center;
        Physics.SyncTransforms();
        yield return new WaitForSecondsRealtime(.2f);
        Record("Powerup pickup activates stronger knockback and indicator", Field<bool>(player, "hasPowerup") && Field<GameObject>(player, "powerupIndicator").activeSelf && powerup == null);
        yield return new WaitForSecondsRealtime(5.2f);
        Record("Powerup expires and hides indicator", !Field<bool>(player, "hasPowerup") && !Field<GameObject>(player, "powerupIndicator").activeSelf);
        foreach (var e in GameObject.FindGameObjectsWithTag("Enemy")) Destroy(e);
        yield return new WaitForSecondsRealtime(.2f);
        Record("Clearing wave spawns two enemies", GameObject.FindGameObjectsWithTag("Enemy").Length == 2, GameObject.FindGameObjectsWithTag("Enemy").Length.ToString());
        Record("Later waves increase enemy speed", Field<float>(spawn, "enemySpeed") > 30);
        rb.position = new Vector3(0, .6f, 5); rb.linearVelocity = Vector3.zero;
        var testEnemy = GameObject.FindGameObjectsWithTag("Enemy")[0];
        var enemyScript = testEnemy.GetComponents<MonoBehaviour>().First(s => s.GetType().Name == "EnemyX");
        enemyScript.enabled = false;
        var enemyRb = testEnemy.GetComponent<Rigidbody>();
        enemyRb.position = rb.position + new Vector3(.8f, 0, 0); enemyRb.linearVelocity = Vector3.zero;
        Physics.SyncTransforms();
        yield return new WaitForSecondsRealtime(.15f);
        Record("Player collision knocks enemy away", enemyRb.linearVelocity.magnitude > 1, enemyRb.linearVelocity.ToString());
        var goal = GameObject.Find("Enemy Goal").GetComponent<Collider>();
        enemyRb.position = goal.bounds.center; enemyRb.linearVelocity = Vector3.zero;
        Physics.SyncTransforms();
        yield return new WaitForSecondsRealtime(.2f);
        Record("Enemy reaching goal is removed", testEnemy == null);
    }
    private void Finish()
    {
        if (finished) return;
        finished = true;
        Keys();
        Application.logMessageReceived -= OnLog;
        report.passed = report.errors.Count == 0 && report.checks.TrueForAll(c => c.passed);
        string directory = Path.Combine(Application.dataPath, "..", "TestResults");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "integration-results.json");
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        Debug.Log("GAMEPLAY VERIFICATION " + (report.passed ? "PASSED " : "FAILED ") + path);
        if (keyboard != null) { InputSystem.RemoveDevice(keyboard); keyboard = null; }
        EditorApplication.Exit(report.passed ? 0 : 1);
    }
}
#endif
