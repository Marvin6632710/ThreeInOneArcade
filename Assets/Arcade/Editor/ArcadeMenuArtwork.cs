using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Renders the actual classroom models for the three selection cards.</summary>
public static class ArcadeMenuArtwork
{
    private const string Folder = "Assets/Arcade/Resources/MenuArt";

    [MenuItem("Arcade/Regenerate Menu Artwork")]
    public static void Generate()
    {
        Directory.CreateDirectory(Folder);
        string original = SceneManager.GetActiveScene().path;
        try {
            Render("Dogs", new Color32(39, 70, 66, 255), () => {
                Model("Assets/Games/Dogs/Challenge 2/Prefabs/Dog.prefab", 5.5f, new Vector3(.1f, 1.1f, 0), new Vector3(0, 202, 0));
                Model("Assets/Games/Dogs/Challenge 2/Prefabs/Ball 1.prefab", 1.4f, new Vector3(-2.9f, 3.1f, .1f), Vector3.zero);
                Model("Assets/Games/Dogs/Challenge 2/Prefabs/Ball 2.prefab", 1.1f, new Vector3(2.9f, 3.4f, .8f), Vector3.zero);
            });
            Render("Balloon", new Color32(43, 51, 89, 255), () => {
                Model("Assets/Games/Balloon/Challenge 3/Prefabs/Player.prefab", 5.8f, new Vector3(0, 2.3f, 0), new Vector3(0, -12, -9));
                Model("Assets/Games/Balloon/Challenge 3/Prefabs/Money.prefab", 1.65f, new Vector3(-2.7f, 2.5f, 0), new Vector3(0, 25, 0));
                Model("Assets/Games/Balloon/Challenge 3/Prefabs/Bomb.prefab", 1.65f, new Vector3(2.6f, 1.1f, 0), new Vector3(-12, -25, 0));
            });
            Render("Arena", new Color32(47, 64, 89, 255), () => {
                Model("Assets/Games/Arena/Challenge 4/Prefabs/Enemy.prefab", 3.6f, new Vector3(-.7f, 1.4f, -.7f), new Vector3(-15, 35, -10));
                Model("Assets/Games/Arena/Challenge 4/Prefabs/Enemy.prefab", 2.1f, new Vector3(2.4f, 2.4f, .5f), new Vector3(0, 20, 15));
                Model("Assets/Games/Arena/Challenge 4/Prefabs/Powerup.prefab", 1.6f, new Vector3(-3, 3.1f, .2f), Vector3.zero);
            });
            AssetDatabase.Refresh();
            foreach (string name in new[] { "Dogs", "Balloon", "Arena" }) {
                var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + "/" + name + ".png");
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 1024;
                importer.SaveAndReimport();
            }
        } finally {
            if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        Debug.Log("ARCADE_MENU_ARTWORK_READY: three classroom-model illustrations");
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    private static void Render(string name, Color backdrop, System.Action arrange)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.72f, .78f, .85f);
        RenderSettings.fog = false;
        var camera = new GameObject("Artwork camera", typeof(Camera)).GetComponent<Camera>();
        camera.transform.position = new Vector3(6, 5, -12);
        camera.transform.LookAt(new Vector3(0, 2.1f, 0));
        camera.orthographic = true; camera.orthographicSize = 3.7f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = backdrop;
        camera.allowHDR = false; camera.allowMSAA = true;
        Light("Key", new Vector3(45, -25, 0), 1.15f, new Color(1, .94f, .85f));
        Light("Rim", new Vector3(20, 150, 0), .7f, new Color(.55f, .78f, 1));
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.position = new Vector3(0, -1.1f, 0);
        var material = new Material(Shader.Find("Standard")); material.color = backdrop;
        material.SetFloat("_Glossiness", .15f); ground.GetComponent<Renderer>().sharedMaterial = material;
        arrange();
        // Match the card's 356:180 artwork region without stretching the models.
        var target = new RenderTexture(1024, 518, 24) { antiAliasing = 4 };
        camera.targetTexture = target; camera.Render();
        var previous = RenderTexture.active; RenderTexture.active = target;
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
        File.WriteAllBytes(Folder + "/" + name + ".png", texture.EncodeToPNG());
        RenderTexture.active = previous; camera.targetTexture = null;
        Object.DestroyImmediate(texture); target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(material);
    }

    private static void Light(string name, Vector3 rotation, float intensity, Color color)
    {
        var light = new GameObject(name, typeof(Light)).GetComponent<Light>();
        light.type = LightType.Directional; light.intensity = intensity; light.color = color;
        light.transform.eulerAngles = rotation; light.shadows = LightShadows.Soft;
    }

    private static void Model(string path, float size, Vector3 center, Vector3 rotation)
    {
        var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        model.transform.position = Vector3.zero; model.transform.eulerAngles = rotation;
        foreach (var behaviour in model.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled = false;
        foreach (var body in model.GetComponentsInChildren<Rigidbody>()) body.isKinematic = true;
        foreach (var animator in model.GetComponentsInChildren<Animator>()) animator.enabled = false;
        foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()) skin.updateWhenOffscreen = true;
        var renderers = model.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        float scale = size / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        model.transform.localScale *= scale;
        bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        model.transform.position += center - bounds.center;
    }
}
