using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;

public static class AudioMixerGenerator
{
    private const string OUTPUT_PATH = "Assets/Plugins/BattleTurn/Generated/AudioManager/GameMixer.mixer";

    [MenuItem("Tools/Audio/Create Game Mixer")]
    public static void CreateMixer()
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioMixer AudioMixer");
        string path = "";
        foreach (var guid in guids)
        {
            string pathL = AssetDatabase.GUIDToAssetPath(guid);
            if (pathL.Contains("Packages/battleturn.audio_manager/Editor/Template"))
            {
                Debug.Log("✅ Found template: " + pathL);
                path = pathL;
                break;
            }
        }
        path = FindAssetAtPath(guids, path);
        // 1. Load template từ package
        var template = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);

        if (template == null)
        {
            Debug.LogError("❌ Cannot find template mixer at: " + path);
            return;
        }

        // 2. Ensure folder tồn tại
        string folder = System.IO.Path.GetDirectoryName(OUTPUT_PATH);
        if (!AssetDatabase.IsValidFolder(folder))
        {
            CreateFolderRecursive(folder);
        }

        // 3. Tránh overwrite
        string generatedAssetPath = AssetDatabase.GenerateUniqueAssetPath(OUTPUT_PATH);

        // 4. Instantiate (QUAN TRỌNG)
        var newMixer = Object.Instantiate(template);

        // 5. Create asset
        AssetDatabase.CreateAsset(newMixer, generatedAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ Created AudioMixer at: {generatedAssetPath}");

        // 6. Optional: focus asset
        Selection.activeObject = newMixer;
        EditorGUIUtility.PingObject(newMixer);
    }

    private static string FindAssetAtPath(string[] guids, string path)
    {
        foreach (var guid in guids)
        {
            string pathL = AssetDatabase.GUIDToAssetPath(guid);
            if (pathL.Contains("Packages/battleturn.audio_manager/Editor/Template"))
            {
                Debug.Log("✅ Found template: " + pathL);
                path = pathL;
                break;
            }
        }

        return path;
    }

    private static void CreateFolderRecursive(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}