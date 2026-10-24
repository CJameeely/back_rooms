#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WhiteBoxKit
{
    public static class WhiteBoxScreenshotTool
    {
        const int Width = 1920, Height = 1080;
        const string ScenePath = "Assets/Scenes/WhiteBox_LastLight.unity";

        struct Shot
        {
            public string name; public string findRoomContains; public Vector3 offset; public Vector3 lookOffset; public bool topDown;
            public Shot(string n, string room, Vector3 off, Vector3 look, bool top = false) { name = n; findRoomContains = room; offset = off; lookOffset = look; topDown = top; }
        }

        static readonly Shot[] Shots =
        {
            new Shot("01_Start",           "Start",               new Vector3(0.8f, 1.6f, -1.5f), new Vector3(0, 1.4f, 3f)),
            new Shot("02_R1_Displaced",    "R1_Displaced",        new Vector3(0, 1.6f, -2.3f),    new Vector3(0, 1.2f, 0f)),
            new Shot("02b_R2_LockedCounter","R2_LockedCounter",   new Vector3(1.5f, 1.6f, -1f),   new Vector3(-1.5f, 1.3f, 2.5f)),
            new Shot("02c_F_Repeat",       "E1_Repeat",           new Vector3(-1f, 1.6f, -1f),    new Vector3(2.9f, 1f, 2f)),
            new Shot("03_R3_PatternSlot",  "R3_PatternAndSlot",   new Vector3(-1.5f, 1.6f, -1f),  new Vector3(2f, 1.1f, 2.9f)),
            new Shot("04_R4_PortableLight","R4_PortableLight",    new Vector3(-2.3f, 1.6f, 0f),   new Vector3(2f, 1.0f, 2f)),
            new Shot("05_R5_Aftermath",    "R5_Aftermath",        new Vector3(2.3f, 1.6f, -2.3f), new Vector3(-1.5f, 1.0f, 1.5f)),
            new Shot("06_R6_MirrorCentre", "R6_MirrorAndCentre",  new Vector3(0, 1.6f, -2.3f),    new Vector3(0, 1.2f, 3f)),
            new Shot("07_R7_Final",        "R7_FinalAlignment",   new Vector3(-2.3f, 1.6f, 0f),   new Vector3(1.5f, 1.4f, -3f)),
            new Shot("08_Overview",        null,                  Vector3.zero, Vector3.zero, true),
        };

        [MenuItem("Tools/WhiteBox/Capture Screenshots")]
        public static void CaptureScreenshots()
        {
            if (!EditorSceneManager.GetActiveScene().path.EndsWith("WhiteBox_LastLight.unity"))
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject roomsGroup = GameObject.Find("Rooms");
            if (roomsGroup == null)
            {
                Debug.LogError("[WhiteBox] 'Rooms' group not found. Run Tools/WhiteBox/Step 2 first.");
                return;
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outDir = Path.Combine(projectRoot, "Screenshots");
            Directory.CreateDirectory(outDir);

            Bounds bounds = ComputeBounds(roomsGroup.transform);

            GameObject camGO = new GameObject("__ScreenshotCam");
            Camera cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.06f);
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.fieldOfView = 70f;

            RenderTexture rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            int count = 0;
            foreach (var shot in Shots)
            {
                if (shot.topDown)
                {
                    cam.orthographic = true;
                    cam.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.z) + 4f;
                    camGO.transform.position = new Vector3(bounds.center.x, 60f, bounds.center.z);
                    camGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                }
                else
                {
                    cam.orthographic = false;
                    Transform room = FindRoom(roomsGroup.transform, shot.findRoomContains);
                    if (room == null)
                    {
                        Debug.LogWarning("[WhiteBox] Screenshot skipped, room not found: " + shot.findRoomContains);
                        continue;
                    }
                    Vector3 pos = room.position + shot.offset;
                    Vector3 lookAt = room.position + shot.lookOffset;
                    camGO.transform.position = pos;
                    camGO.transform.rotation = Quaternion.LookRotation((lookAt - pos).normalized, Vector3.up);
                }

                RenderTexture.active = rt;
                cam.Render();
                Texture2D tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;

                byte[] png = tex.EncodeToPNG();
                string filePath = Path.Combine(outDir, shot.name + ".png");
                File.WriteAllBytes(filePath, png);
                Object.DestroyImmediate(tex);
                count++;
                Debug.Log("[WhiteBox] Saved screenshot: " + filePath);
            }

            cam.targetTexture = null;
            RenderTexture.active = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camGO);

            Debug.Log($"[WhiteBox] Capture complete: {count} screenshots written to {outDir}");
        }

        static Transform FindRoom(Transform roomsGroup, string contains)
        {
            foreach (Transform child in roomsGroup)
                if (child.name.Contains(contains))
                    return child;
            return null;
        }

        static Bounds ComputeBounds(Transform roomsGroup)
        {
            Bounds b = new Bounds(roomsGroup.GetChild(0).position, Vector3.zero);
            foreach (Transform child in roomsGroup)
                b.Encapsulate(child.position);
            return b;
        }
    }
}
#endif
