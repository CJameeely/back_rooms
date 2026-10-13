#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace WhiteBoxKit
{
    // Shared constants, materials, module-prefab creation and the generic Room_6x6 shell.
    public static class WhiteBoxModules
    {
        public const string RootFolder = "Assets/WhiteBox";
        public const string PrefabFolder = RootFolder + "/Prefabs";
        public const string MaterialFolder = RootFolder + "/Materials";

        public const float WallHeight = 2.7f;
        public const float WallThickness = 0.2f;
        public const float RoomHalf = 3f; // 6m room -> +-3
        public const float TileCenter = 2f; // 3 tiles of 2m across 6m -> centers at -2,0,2
        public static readonly float[] TileOffsets = { -2f, 0f, 2f };

        public static Material MatStructure, MatInteractive, MatClue, MatLightPanel, MatWall, MatFloor, MatCeiling;

        public static readonly string[] ModulePrefabNames =
        {
            "Floor_2x2", "Wall_2m", "Wall_Door", "Ceiling_Light_2x2", "Pillar",
            "Desk", "Counter_Drawer", "Chair", "Paper", "Mirror_Panel", "Dial_Panel",
            "Lamp_PartA", "Lamp_PartB", "Key", "Trace_Decal"
        };

        [MenuItem("Tools/WhiteBox/Step 1 - Import FlyCam + Generate Prefabs")]
        public static void Step1_ImportAndGeneratePrefabs()
        {
            EnsureFolders();
            ImportFlyCameraPackage();
            CreateMaterials();
            CreateModulePrefabs();
            CreateRoomPrefab(LoadPrefabs());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[WhiteBox] Step 1 complete: materials, module prefabs and Room_6x6 generated.");
        }

        public static void ImportFlyCameraPackage()
        {
            // Note: AssetDatabase.ImportPackage() is asynchronous and does not reliably finish
            // before a batch-mode process quits, so the FlyCamera package's files are placed
            // directly on disk (preserving their original .meta GUIDs) rather than imported
            // through this API. This method just verifies the result.
            string existing = "Assets/StudentTools/FlyCamera/FlyCamera.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(existing) != null)
                Debug.Log("[WhiteBox] FlyCamera prefab present at " + existing);
            else
                Debug.LogWarning("[WhiteBox] FlyCamera prefab not found at " + existing + ".");
        }

        public static Dictionary<string, GameObject> LoadPrefabs()
        {
            var dict = new Dictionary<string, GameObject>();
            foreach (string name in ModulePrefabNames)
            {
                string path = PrefabFolder + "/" + name + ".prefab";
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null) dict[name] = go;
            }
            string roomPath = PrefabFolder + "/Room_6x6.prefab";
            GameObject room = AssetDatabase.LoadAssetAtPath<GameObject>(roomPath);
            if (room != null) dict["Room_6x6"] = room;
            return dict;
        }

        public static void EnsureFolders()
        {
            EnsureFolder("Assets", "WhiteBox");
            EnsureFolder(RootFolder, "Prefabs");
            EnsureFolder(RootFolder, "Materials");
            EnsureFolder("Assets", "Scenes");
        }

        static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        // ---------- Materials ----------

        const string TextureFolder = "Assets/KenneyAssets/Textures";
        static Texture2D GridFine => AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/Grid_Fine.png");
        static Texture2D GridPanel => AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/Grid_Panel.png");
        static Texture2D GridFineDark => AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/Grid_Fine_Dark.png");
        static Texture2D GridPanelDark => AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/Grid_Panel_Dark.png");

        public static void CreateMaterials()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = Shader.Find("Standard");

            // Cold clinical-lab palette: pale cool whites with a real tiling panel/grid texture
            // (Kenney's CC0 prototype set) so surfaces read as machined panels instead of flat
            // dead color. Interactive (amber) and clue (ice blue) use the darker grid variant,
            // tinted, so they look like lit signage panels rather than a flat neon plane.
            MatStructure = CreateOrLoadMaterial("Mat_Structure", lit, new Color(0.78f, 0.8f, 0.84f), GridFine, new Vector2(2f, 2f));
            MatInteractive = CreateOrLoadMaterial("Mat_Interactive", lit, new Color(0.95f, 0.55f, 0.2f), GridFineDark, new Vector2(2f, 2f));
            MatClue = CreateOrLoadMaterial("Mat_Clue", lit, new Color(0.25f, 0.6f, 0.85f), GridFineDark, new Vector2(2f, 2f));

            MatWall = CreateOrLoadMaterial("Mat_Wall", lit, new Color(0.82f, 0.85f, 0.9f), GridFine, new Vector2(1.5f, 2f));
            MatWall.EnableKeyword("_EMISSION");
            MatWall.SetColor("_EmissionColor", new Color(0.55f, 0.6f, 0.68f) * 0.15f); // faint backlit-glass glow
            MatWall.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            MatFloor = CreateOrLoadMaterial("Mat_Floor", lit, new Color(0.62f, 0.65f, 0.7f), GridPanel, new Vector2(1f, 1f));
            MatCeiling = CreateOrLoadMaterial("Mat_Ceiling", lit, new Color(0.88f, 0.9f, 0.94f), GridPanel, new Vector2(1f, 1f));

            MatLightPanel = CreateOrLoadMaterial("Mat_LightPanel", lit, Color.white);
            MatLightPanel.EnableKeyword("_EMISSION");
            MatLightPanel.SetColor("_EmissionColor", Color.white * 2.4f);
            MatLightPanel.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        public static void LoadMaterials()
        {
            MatStructure = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Mat_Structure.mat");
            MatInteractive = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Mat_Interactive.mat");
            MatClue = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Mat_Clue.mat");
            MatLightPanel = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Mat_LightPanel.mat");
            MatWall = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Mat_Wall.mat");
            MatFloor = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Mat_Floor.mat");
            MatCeiling = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Mat_Ceiling.mat");
        }

        static Material CreateOrLoadMaterial(string name, Shader shader, Color color, Texture2D tex = null, Vector2? tiling = null)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }
            mat.color = color;
            // A little smoothness gives the frosted-glass/metal panels a faint highlight instead
            // of the completely matte, "dead" look flat Smoothness=0 produces under direct light.
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.25f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.25f);
            if (tex != null)
            {
                mat.mainTexture = tex;
                mat.mainTextureScale = tiling ?? Vector2.one;
            }
            // Imported Kenney models aren't guaranteed to have outward-facing normals the way
            // our own primitives do (e.g. the floor model's visible face pointed down, which
            // backface culling silently turned invisible from a normal standing viewpoint).
            // Rendering both sides is harmless for flat/solid whitebox shapes and removes that
            // whole class of "imported mesh renders as a black hole" surprise.
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ---------- Generic primitive helper ----------
        // Root pivot conventions are documented per module below. All parts are children
        // named "Mesh" (or "Mesh_x") of an empty root so the root transform is the clean
        // insertion point used when the module is placed in a room.

        public static GameObject NewRoot(string name)
        {
            return new GameObject(name);
        }

        public static GameObject AddBoxPart(GameObject root, string partName, Vector3 size, Vector3 localCenter, Material mat, Vector3? eulerRot = null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = partName;
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = localCenter;
            go.transform.localRotation = eulerRot.HasValue ? Quaternion.Euler(eulerRot.Value) : Quaternion.identity;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        public static GameObject AddCylinderPart(GameObject root, string partName, float diameter, float thickness, Vector3 localCenter, Material mat, Vector3 eulerRot)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = partName;
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = localCenter;
            go.transform.localRotation = Quaternion.Euler(eulerRot);
            // Unity's cylinder primitive is height 2, diameter 1 at scale 1.
            go.transform.localScale = new Vector3(diameter, thickness / 2f, diameter);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        // ---------- Imported model part helper ----------
        // Kenney's CC0 model packs (Assets/KenneyAssets/Models/) are not authored at the
        // project's 1-unit-=-1m convention, and some of them keep an absolute offset from a
        // shared master scene baked into the mesh instead of being centered at their own
        // origin. This measures each model's actual bounds at import time (rather than relying
        // on hand-measured constants, which would silently go stale if a file changes) and
        // re-pivots/re-scales it to drop into the same "pass a center position" calling
        // convention as AddBoxPart, so callers don't need to care that it's a real mesh
        // underneath. Original materials/textures are discarded in favor of the project's own
        // flat-color material set, both to keep the look consistent and to avoid importing
        // texture assets at all.
        public static GameObject AddModelPart(GameObject root, string partName, string fbxAssetPath,
            Vector3 targetSize, Vector3 localCenter, Material mat, bool uniformScale = true)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxAssetPath);
            if (fbx == null)
            {
                Debug.LogError("[WhiteBox] Model not found, falling back to a box: " + fbxAssetPath);
                return AddBoxPart(root, partName, targetSize, localCenter, mat);
            }

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            inst.name = partName;
            // Some Kenney FBX exports keep each piece's original position from the combined
            // kit layout scene (e.g. floorFull.fbx spawns at ~(-66, 0, 16), not the origin).
            // Zero the root transform first so nativeBounds is measured pivot-relative, which
            // is what the re-pivot math below assumes.
            inst.transform.position = Vector3.zero;
            Bounds nativeBounds = ComputeRendererBounds(inst);

            Vector3 scale;
            if (uniformScale)
            {
                float sx = targetSize.x / Mathf.Max(nativeBounds.size.x, 0.0001f);
                float sy = targetSize.y / Mathf.Max(nativeBounds.size.y, 0.0001f);
                float sz = targetSize.z / Mathf.Max(nativeBounds.size.z, 0.0001f);
                float s = (sx + sy + sz) / 3f;
                scale = new Vector3(s, s, s);
            }
            else
            {
                scale = new Vector3(
                    targetSize.x / Mathf.Max(nativeBounds.size.x, 0.0001f),
                    targetSize.y / Mathf.Max(nativeBounds.size.y, 0.0001f),
                    targetSize.z / Mathf.Max(nativeBounds.size.z, 0.0001f));
            }

            inst.transform.SetParent(root.transform, false);
            inst.transform.localScale = scale;
            // Re-pivot so the model's (scaled) bounds center lands exactly at localCenter,
            // regardless of whatever offset the mesh carried in its own coordinate space.
            inst.transform.localPosition = localCenter - Vector3.Scale(nativeBounds.center, scale);

            // Some of these models have multiple material slots per renderer (e.g. a separate
            // slot for a screen/panel accent); overwriting only .sharedMaterial leaves the
            // others on their original (now textureless) material, which shows up as a stray
            // gray/placeholder patch. Fill every slot with the same material instead.
            foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }

            BoxCollider col = inst.AddComponent<BoxCollider>();
            col.center = nativeBounds.center;
            col.size = nativeBounds.size;

            return inst;
        }

        public static string KenneyModel(string name) => "Assets/KenneyAssets/Models/" + name + ".fbx";

        static Bounds ComputeRendererBounds(GameObject go)
        {
            Bounds? b = null;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                if (b == null) b = r.bounds;
                else { Bounds bb = b.Value; bb.Encapsulate(r.bounds); b = bb; }
            }
            return b ?? new Bounds(Vector3.zero, Vector3.zero);
        }

        static GameObject SaveAsPrefab(GameObject root)
        {
            string path = PrefabFolder + "/" + root.name + ".prefab";
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return asset;
        }

        // ---------- Module prefabs ----------
        // Convention: floor-standing modules have their root pivot at the BOTTOM-CENTER of
        // their footprint, sitting flush on the floor (y=0 at placement). Wall-mounted
        // modules have their root pivot at the mounting point on the wall surface
        // (thickness extends toward +local Z, away from the wall into the room).

        public static Dictionary<string, GameObject> CreateModulePrefabs()
        {
            var prefabs = new Dictionary<string, GameObject>();

            // Floor_2x2 : root pivot = top of floor surface (y=0)
            {
                var root = NewRoot("Floor_2x2");
                AddModelPart(root, "Mesh", KenneyModel("floorFull"), new Vector3(2f, 0.1f, 2f), new Vector3(0, -0.05f, 0), MatFloor);
                prefabs["Floor_2x2"] = SaveAsPrefab(root);
            }

            // Wall_2m : root pivot = bottom-center-front (front face at local z=0, extrudes to z=+0.2)
            {
                var root = NewRoot("Wall_2m");
                AddBoxPart(root, "Mesh", new Vector3(2f, WallHeight, WallThickness), new Vector3(0, WallHeight / 2f, WallThickness / 2f), MatWall);
                prefabs["Wall_2m"] = SaveAsPrefab(root);
            }

            // Wall_Door : same footprint as Wall_2m but with a 1m x 2.1m door opening centered
            {
                var root = NewRoot("Wall_Door");
                float doorW = 1f, doorH = 2.1f;
                float jambW = (2f - doorW) / 2f; // 0.5
                // Left jamb
                AddBoxPart(root, "Mesh_JambL", new Vector3(jambW, WallHeight, WallThickness),
                    new Vector3(-(doorW / 2f + jambW / 2f), WallHeight / 2f, WallThickness / 2f), MatWall);
                // Right jamb
                AddBoxPart(root, "Mesh_JambR", new Vector3(jambW, WallHeight, WallThickness),
                    new Vector3((doorW / 2f + jambW / 2f), WallHeight / 2f, WallThickness / 2f), MatWall);
                // Header above the door
                AddBoxPart(root, "Mesh_Header", new Vector3(doorW, WallHeight - doorH, WallThickness),
                    new Vector3(0, doorH + (WallHeight - doorH) / 2f, WallThickness / 2f), MatWall);
                prefabs["Wall_Door"] = SaveAsPrefab(root);
            }

            // Ceiling_Light_2x2 : root pivot = bottom face of ceiling slab (slab extrudes upward)
            {
                var root = NewRoot("Ceiling_Light_2x2");
                AddBoxPart(root, "Mesh_Slab", new Vector3(2f, 0.1f, 2f), new Vector3(0, 0.05f, 0), MatCeiling);
                AddBoxPart(root, "Mesh_Panel", new Vector3(1.6f, 0.02f, 1.6f), new Vector3(0, -0.01f, 0), MatLightPanel);
                GameObject lightGO = new GameObject("Light");
                lightGO.transform.SetParent(root.transform, false);
                lightGO.transform.localPosition = new Vector3(0, -0.02f, 0);
                Light lt = lightGO.AddComponent<Light>();
                lt.type = LightType.Point;
                lt.range = 4.5f;
                lt.intensity = 0.9f;
                lt.color = new Color(0.95f, 0.97f, 1f); // cool clinical-white light
                lt.shadows = LightShadows.None;
                prefabs["Ceiling_Light_2x2"] = SaveAsPrefab(root);
            }

            // Pillar : root pivot = bottom-center. Space Kit's structural piece isn't
            // slender like a column, so it's scaled non-uniformly to fit the footprint instead
            // of using CreateModelPart's default uniform scale.
            {
                var root = NewRoot("Pillar");
                AddModelPart(root, "Mesh", KenneyModel("metalStructure"),
                    new Vector3(0.5f, WallHeight, 0.5f), new Vector3(0, WallHeight / 2f, 0), MatWall, uniformScale: false);
                prefabs["Pillar"] = SaveAsPrefab(root);
            }

            // Desk : root pivot = bottom-center
            {
                var root = NewRoot("Desk");
                AddModelPart(root, "Mesh", KenneyModel("desk"), new Vector3(1.4f, 0.75f, 0.7f), new Vector3(0, 0.375f, 0), MatStructure);
                prefabs["Desk"] = SaveAsPrefab(root);
            }

            // Counter_Drawer : root pivot = bottom-center. Mesh_DrawerFront stays a plain
            // orange box (not part of the model) because MovableCabinetClue/DrawerClue and
            // every room that uses this prefab find it by that exact name to drive the slide
            // animation — it's the interactive handle, layered in front of the real model body.
            {
                var root = NewRoot("Counter_Drawer");
                AddModelPart(root, "Mesh_Body", KenneyModel("sideTableDrawers"), new Vector3(1.2f, 0.9f, 0.6f), new Vector3(0, 0.45f, 0), MatStructure);
                AddBoxPart(root, "Mesh_DrawerFront", new Vector3(0.4f, 0.15f, 0.05f), new Vector3(0, 0.5f, 0.325f), MatInteractive);
                prefabs["Counter_Drawer"] = SaveAsPrefab(root);
            }

            // Chair : root pivot = bottom-center (leg base on floor)
            {
                var root = NewRoot("Chair");
                AddModelPart(root, "Mesh", KenneyModel("chairDesk"), new Vector3(0.45f, 0.9f, 0.5f), new Vector3(0, 0.45f, 0), MatStructure);
                prefabs["Chair"] = SaveAsPrefab(root);
            }

            // Paper : root pivot = bottom-center (lies flat on a surface). Clue-colored.
            {
                var root = NewRoot("Paper");
                AddBoxPart(root, "Mesh", new Vector3(0.21f, 0.01f, 0.3f), new Vector3(0, 0.005f, 0), MatClue);
                prefabs["Paper"] = SaveAsPrefab(root);
            }

            // Mirror_Panel : root pivot = mounting point on wall surface (thickness toward +z)
            {
                var root = NewRoot("Mirror_Panel");
                AddBoxPart(root, "Mesh", new Vector3(1f, 1.6f, 0.05f), new Vector3(0, 0, 0.025f), MatClue);
                prefabs["Mirror_Panel"] = SaveAsPrefab(root);
            }

            // Dial_Panel : root pivot = mounting point on wall surface, disc faces +z. Interactive.
            {
                var root = NewRoot("Dial_Panel");
                AddCylinderPart(root, "Mesh", 0.5f, 0.06f, new Vector3(0, 0, 0.03f), MatInteractive, new Vector3(90f, 0, 0));
                prefabs["Dial_Panel"] = SaveAsPrefab(root);
            }

            // Lamp_PartA : root pivot = bottom-center. Interactive pickup.
            {
                var root = NewRoot("Lamp_PartA");
                AddBoxPart(root, "Mesh", new Vector3(0.18f, 0.12f, 0.12f), new Vector3(0, 0.06f, 0), MatInteractive);
                prefabs["Lamp_PartA"] = SaveAsPrefab(root);
            }

            // Lamp_PartB : root pivot = bottom-center. Interactive pickup, visually distinct from A.
            {
                var root = NewRoot("Lamp_PartB");
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "Mesh";
                sphere.transform.SetParent(root.transform, false);
                sphere.transform.localPosition = new Vector3(0, 0.07f, 0);
                sphere.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
                sphere.GetComponent<Renderer>().sharedMaterial = MatInteractive;
                prefabs["Lamp_PartB"] = SaveAsPrefab(root);
            }

            // Key : root pivot = bottom-center. Interactive pickup.
            {
                var root = NewRoot("Key");
                AddBoxPart(root, "Mesh", new Vector3(0.15f, 0.02f, 0.05f), new Vector3(0, 0.01f, 0), MatInteractive);
                prefabs["Key"] = SaveAsPrefab(root);
            }

            // Trace_Decal : root pivot = bottom-center. Thin clue marking; scale per-instance
            // via the root transform to represent clean patches, empty outlines, handprints, etc.
            {
                var root = NewRoot("Trace_Decal");
                AddBoxPart(root, "Mesh", new Vector3(1f, 0.01f, 1f), new Vector3(0, 0.005f, 0), MatClue);
                prefabs["Trace_Decal"] = SaveAsPrefab(root);
            }

            AssetDatabase.SaveAssets();
            return prefabs;
        }

        // ---------- Room_6x6 shell prefab ----------
        // Local space: room center = (0,0,0) at floor height. Walls at local x/z = +-3.
        // Wall segment centers (along the wall) at local -2, 0, +2; the CENTER segment
        // (index 1) is the one a level builder swaps for a Wall_Door when a room needs a
        // door on that side. Children are named predictably so the level builder can find them.

        static readonly (string dir, float angleY)[] WallDirs =
        {
            ("North", 0f), ("South", 180f), ("East", 90f), ("West", -90f)
        };

        public static GameObject CreateRoomPrefab(Dictionary<string, GameObject> prefabs)
        {
            GameObject room = new GameObject("Room_6x6");

            GameObject floorGroup = new GameObject("Floor");
            floorGroup.transform.SetParent(room.transform, false);
            GameObject ceilingGroup = new GameObject("Ceiling");
            ceilingGroup.transform.SetParent(room.transform, false);
            GameObject wallsGroup = new GameObject("Walls");
            wallsGroup.transform.SetParent(room.transform, false);

            foreach (float x in TileOffsets)
            {
                foreach (float z in TileOffsets)
                {
                    Instantiate(prefabs["Floor_2x2"], floorGroup.transform, new Vector3(x, 0, z), Quaternion.identity);
                    Instantiate(prefabs["Ceiling_Light_2x2"], ceilingGroup.transform, new Vector3(x, WallHeight, z), Quaternion.identity);
                }
            }

            foreach (var (dir, angleY) in WallDirs)
            {
                Quaternion rot = Quaternion.Euler(0, angleY, 0);
                string[] segNames = { "Left", "Center", "Right" };
                for (int i = 0; i < 3; i++)
                {
                    float segX = TileOffsets[i];
                    Vector3 pos = rot * new Vector3(segX, 0, RoomHalf);
                    var inst = Instantiate(prefabs["Wall_2m"], wallsGroup.transform, pos, rot);
                    inst.name = "Wall_" + dir + "_" + segNames[i];
                }
            }

            string path = PrefabFolder + "/Room_6x6.prefab";
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(room, path);
            UnityEngine.Object.DestroyImmediate(room);
            AssetDatabase.SaveAssets();
            return asset;
        }

        static GameObject Instantiate(GameObject prefab, Transform parent, Vector3 localPos, Quaternion localRot)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.localPosition = localPos;
            inst.transform.localRotation = localRot;
            return inst;
        }

        // ---------- TextMeshPro labels ----------

        static bool _tmpChecked;
        static bool _tmpAvailable;

        public static bool EnsureTMPEssentials()
        {
            if (_tmpChecked) return _tmpAvailable;
            _tmpChecked = true;

            TMP_FontAsset asset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (asset == null)
            {
                string[] guids = AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset");
                if (guids.Length > 0)
                    asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (asset == null)
            {
                EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
                AssetDatabase.Refresh();
                string[] guids = AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset");
                asset = guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
            }

            _tmpAvailable = asset != null;
            if (!_tmpAvailable)
                Debug.LogWarning("[WhiteBox] TMP Essential Resources not found after import attempt. Labels will be skipped; run Window > TextMeshPro > Import TMP Essential Resources manually, then re-run Tools/WhiteBox/Build Level Scene.");
            return _tmpAvailable;
        }

        // positionRef places the label (its localPos is relative to that prop), but the label
        // is actually parented under safeParent (always identity rotation / uniform scale --
        // typically the room's Content group). Parenting a label directly under a prop that
        // has non-uniform scale (many Trace_Decal instances do) combined with an explicit
        // world-space rotation produces a mirrored/flipped result, since Unity's rotation
        // composition doesn't account for a non-uniformly-scaled ancestor.
        // roomCenterWorld lets the label face away from the room's middle (toward wherever a
        // player standing in the open floor would be), independent of the prop's own tilt.
        public static GameObject CreateLabel(string text, Transform positionRef, Vector3 localPos, Transform safeParent, Vector3 roomCenterWorld, bool doubleSided = true)
        {
            if (!EnsureTMPEssentials()) return null;

            Vector3 worldPos = positionRef.TransformPoint(localPos);

            Vector3 dir = worldPos - roomCenterWorld;
            dir.y = 0;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            // Snap to the nearest cardinal axis: rooms and walls are all axis-aligned, and a
            // raw diagonal (e.g. from a label offset sideways on a wall) tilts the text off the
            // flat wall plane, causing extreme foreshortening from an angle instead of a clean face.
            dir = Mathf.Abs(dir.x) > Mathf.Abs(dir.z) ? new Vector3(Mathf.Sign(dir.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(dir.z));
            Quaternion facing = Quaternion.LookRotation(dir, Vector3.up);

            GameObject go = new GameObject("Label_" + text.Replace(" ", "_"));
            go.transform.SetParent(safeParent, false);
            go.transform.position = worldPos;
            go.transform.rotation = facing;

            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 3f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;

            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
                rt.sizeDelta = new Vector2(2f, 0.5f);

            return go;
        }
    }
}
#endif
