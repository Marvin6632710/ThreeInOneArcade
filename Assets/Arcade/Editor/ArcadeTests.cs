using Arcade;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ArcadeTests
{
    static ArcadeTests() { EditorApplication.playModeStateChanged += StateChanged; }

    [MenuItem("Arcade/Run Integration Checks")]
    public static void Run()
    {
        ArcadeBuild.Prepare();
        EditorSceneManager.OpenScene(GameCatalog.MenuScene);
        SessionState.SetBool("RunArcadeIntegrationChecks", true);
        EditorApplication.EnterPlaymode();
    }

    private static void StateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("RunArcadeIntegrationChecks", false)) return;
        SessionState.EraseBool("RunArcadeIntegrationChecks");
        var go = new GameObject("Arcade Integration Checks");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<ArcadeIntegrationProbe>();
    }
}
