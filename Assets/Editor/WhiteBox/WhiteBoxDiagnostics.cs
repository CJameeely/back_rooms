#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WhiteBoxKit
{
    public static class WhiteBoxDiagnostics
    {
        [MenuItem("Tools/WhiteBox/Add Scene To Build Settings")]
        public static void AddSceneToBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            const string path = "Assets/Scenes/WhiteBox_LastLight.unity";
            if (!scenes.Exists(s => s.path == path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log("[WhiteBox] Added scene to Build Settings: " + path);
            }
            else
            {
                Debug.Log("[WhiteBox] Scene already in Build Settings.");
            }
        }

        [MenuItem("Tools/WhiteBox/Diagnose Materials")]
        public static void DiagnoseMaterials()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/WhiteBox_LastLight.unity", OpenSceneMode.Single);

            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                foreach (Transform child in player.transform)
                {
                    var rend = child.GetComponent<Renderer>();
                    if (rend != null)
                        Debug.Log($"[DIAG] Player/{child.name} material={rend.sharedMaterial?.name} color={rend.sharedMaterial?.color} shader={rend.sharedMaterial?.shader?.name}");
                }
            }

            GameObject rooms = GameObject.Find("Rooms");
            if (rooms != null)
            {
                foreach (Transform room in rooms.transform)
                {
                    if (!room.name.Contains("Start")) continue;
                    Transform walls = room.Find("Walls");
                    foreach (Transform wall in walls)
                    {
                        foreach (var rend in wall.GetComponentsInChildren<Renderer>())
                        {
                            Debug.Log($"[DIAG] {room.name}/{wall.name}/{rend.name} material={rend.sharedMaterial?.name} color={rend.sharedMaterial?.color} shader={rend.sharedMaterial?.shader?.name}");
                        }
                    }
                }
            }
        }
    }
}
#endif
