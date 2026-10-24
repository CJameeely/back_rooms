#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static WhiteBoxKit.WhiteBoxModules;

namespace WhiteBoxKit
{
    public enum RoomKind
    {
        Start, Displaced, LockedCounter, Repeat, PatternAndSlot, PortableLight,
        Aftermath, MirrorAndCentre, FinalAlignment
    }

    enum Dir { North, East, South, West }

    class RoomDef
    {
        public int gx, gz;
        public RoomKind kind;
        public string label;
        public HashSet<Dir> doors = new HashSet<Dir>();
        public Transform instance;
        public Transform contentGroup;
    }

    public static class WhiteBoxLevelBuilder
    {
        const string ScenePath = "Assets/Scenes/WhiteBox_LastLight.unity";
        const float Grid = 8f; // 6m room + 2m corridor gap, center-to-center

        static Dictionary<string, GameObject> P;
        static Transform roomsGroup, corridorsGroup, lightingGroup, playerGroup;
        static List<RoomDef> allRooms = new List<RoomDef>();
        static Dictionary<(int, int), RoomDef> byCoord = new Dictionary<(int, int), RoomDef>();

        static readonly Dictionary<Dir, Vector2Int> DirVec = new Dictionary<Dir, Vector2Int>
        {
            { Dir.North, new Vector2Int(0, 1) }, { Dir.East, new Vector2Int(1, 0) },
            { Dir.South, new Vector2Int(0, -1) }, { Dir.West, new Vector2Int(-1, 0) }
        };

        static Dir Opposite(Dir d) => d switch
        {
            Dir.North => Dir.South, Dir.South => Dir.North,
            Dir.East => Dir.West, Dir.West => Dir.East, _ => d
        };

        [MenuItem("Tools/WhiteBox/Step 2 - Build Level Scene")]
        public static void Step2_BuildLevel()
        {
            P = LoadPrefabs();
            LoadMaterials();
            if (!P.ContainsKey("Room_6x6"))
            {
                Debug.LogError("[WhiteBox] Room_6x6 prefab missing. Run Tools/WhiteBox/Step 1 first.");
                return;
            }

            allRooms.Clear();
            byCoord.Clear();
            connections.Clear();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupLighting(scene);

            roomsGroup = new GameObject("Rooms").transform;
            corridorsGroup = new GameObject("Corridors").transform;
            lightingGroup = new GameObject("Lighting").transform;
            playerGroup = new GameObject("Player").transform;

            BuildGraph();
            ComputeDoors();
            InstantiateRooms();
            InstantiateCorridors();
            PopulateAllRooms();
            SetupPlayerAndScale();
            BuildCameraTour();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[WhiteBox] Step 2 complete: level scene built with " + allRooms.Count + " rooms -> " + ScenePath);
        }

        static void SetupLighting(Scene scene)
        {
            Color neutral = new Color(0.42f, 0.45f, 0.5f); // cool clinical-lab ambient
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = neutral;
            RenderSettings.ambientSkyColor = neutral;
            RenderSettings.ambientEquatorColor = neutral;
            RenderSettings.ambientGroundColor = neutral;
            RenderSettings.ambientIntensity = 1f;
            // No sky is ever visible in an indoor backrooms level; a default procedural skybox
            // otherwise leaks a blue/cyan tint into every surface's ambient term (most visible
            // on curved or off-axis geometry). Killing it keeps lighting flat and neutral.
            RenderSettings.skybox = null;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = null;
            RenderSettings.fog = false;
        }

        // ---------------- Graph definition ----------------
        // Nine rooms total (half of the original eighteen): related puzzle beats were merged
        // into single, more detailed rooms rather than kept as separate near-empty stops.

        struct Step
        {
            public RoomKind kind; public Dir move; public string label;
            public Step(RoomKind k, Dir m, string l) { kind = k; move = m; label = l; }
        }

        static void BuildGraph()
        {
            int x = 0, z = 0;
            AddRoom(x, z, RoomKind.Start, "Start");

            var main = new List<Step>
            {
                new Step(RoomKind.Displaced,       Dir.North, "R1 Displaced Object"),
                new Step(RoomKind.LockedCounter,   Dir.East,  "R2 Locked Counter"),
                new Step(RoomKind.Repeat,          Dir.North, "E1 Repeat (holds KEY)"),
                new Step(RoomKind.PatternAndSlot,  Dir.East,  "R3 Pattern + Empty Slot"),
                new Step(RoomKind.PortableLight,   Dir.North, "R4 Portable Light (both parts)"),
                new Step(RoomKind.Aftermath,       Dir.East,  "R5 Aftermath (writing + silent room)"),
                new Step(RoomKind.MirrorAndCentre, Dir.North, "R6 Mirror + False Centre"),
                new Step(RoomKind.FinalAlignment,  Dir.East,  "R7 Final Alignment"),
            };

            RoomDef prev = byCoord[(0, 0)];
            foreach (var step in main)
            {
                Vector2Int d = DirVec[step.move];
                x += d.x; z += d.y;
                var room = AddRoom(x, z, step.kind, step.label);
                Connect(prev, room, step.move);
                prev = room;
            }
        }

        static RoomDef AddRoom(int gx, int gz, RoomKind kind, string label)
        {
            var r = new RoomDef { gx = gx, gz = gz, kind = kind, label = label };
            allRooms.Add(r);
            byCoord[(gx, gz)] = r;
            return r;
        }

        static readonly List<(RoomDef a, RoomDef b, Dir dirFromA)> connections = new();

        static void Connect(RoomDef a, RoomDef b, Dir dirFromA)
        {
            connections.Add((a, b, dirFromA));
        }

        static void ComputeDoors()
        {
            foreach (var (a, b, dir) in connections)
            {
                a.doors.Add(dir);
                b.doors.Add(Opposite(dir));
            }
        }

        // ---------------- Instantiation ----------------

        static Vector3 WorldPos(RoomDef r) => new Vector3(r.gx * Grid, 0f, r.gz * Grid);

        static string KindShort(RoomKind k) => k switch
        {
            RoomKind.Start => "Start",
            RoomKind.Displaced => "R1_Displaced",
            RoomKind.LockedCounter => "R2_LockedCounter",
            RoomKind.Repeat => "E1_Repeat",
            RoomKind.PatternAndSlot => "R3_PatternAndSlot",
            RoomKind.PortableLight => "R4_PortableLight",
            RoomKind.Aftermath => "R5_Aftermath",
            RoomKind.MirrorAndCentre => "R6_MirrorAndCentre",
            RoomKind.FinalAlignment => "R7_FinalAlignment",
            _ => "Room"
        };

        static void InstantiateRooms()
        {
            int i = 0;
            foreach (var r in allRooms)
            {
                i++;
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(P["Room_6x6"], roomsGroup);
                inst.name = $"Room_{i:D2}_{KindShort(r.kind)}";
                inst.transform.position = WorldPos(r);
                r.instance = inst.transform;

                foreach (Dir d in r.doors)
                    SwapInDoor(inst.transform, d);

                GameObject content = new GameObject("Content");
                content.transform.SetParent(inst.transform, false);
                r.contentGroup = content.transform;
            }
        }

        static readonly Dictionary<Dir, string> WallDirName = new()
        {
            { Dir.North, "North" }, { Dir.South, "South" }, { Dir.East, "East" }, { Dir.West, "West" }
        };

        static void SwapInDoor(Transform room, Dir dir)
        {
            string dirName = WallDirName[dir];
            Transform walls = room.Find("Walls");
            Transform center = walls.Find("Wall_" + dirName + "_Center");
            if (center == null) return;

            Vector3 pos = center.position;
            Quaternion rot = center.rotation;
            UnityEngine.Object.DestroyImmediate(center.gameObject);

            GameObject door = (GameObject)PrefabUtility.InstantiatePrefab(P["Wall_Door"], walls);
            door.name = "Wall_" + dirName + "_Center";
            door.transform.position = pos;
            door.transform.rotation = rot;
        }

        static void InstantiateCorridors()
        {
            foreach (var (a, b, dir) in connections)
            {
                Vector3 pa = WorldPos(a), pb = WorldPos(b);
                Vector3 center = (pa + pb) / 2f;
                BuildCorridor(center, dir);
            }
        }

        static void BuildCorridor(Vector3 center, Dir travelDir)
        {
            GameObject group = new GameObject("Corridor_" + WallDirName[travelDir] + "_" + center.x + "_" + center.z);
            group.transform.SetParent(corridorsGroup, false);

            Spawn(P["Floor_2x2"], group.transform, center, Quaternion.identity);
            Spawn(P["Ceiling_Light_2x2"], group.transform, center + new Vector3(0, WallHeight, 0), Quaternion.identity);

            bool alongZ = travelDir == Dir.North || travelDir == Dir.South;
            if (alongZ)
            {
                Spawn(P["Wall_2m"], group.transform, center + new Vector3(1f, 0, 0), Quaternion.Euler(0, 90, 0));
                Spawn(P["Wall_2m"], group.transform, center + new Vector3(-1f, 0, 0), Quaternion.Euler(0, -90, 0));
            }
            else
            {
                Spawn(P["Wall_2m"], group.transform, center + new Vector3(0, 0, 1f), Quaternion.identity);
                Spawn(P["Wall_2m"], group.transform, center + new Vector3(0, 0, -1f), Quaternion.Euler(0, 180, 0));
            }
        }

        static GameObject Spawn(GameObject prefab, Transform parent, Vector3 worldPos, Quaternion worldRot)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.position = worldPos;
            inst.transform.rotation = worldRot;
            return inst;
        }

        static GameObject SpawnLocal(GameObject prefab, Transform parent, Vector3 localPos, Quaternion localRot)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.localPosition = localPos;
            inst.transform.localRotation = localRot;
            return inst;
        }

        // ---------------- Per-room content ----------------
        // Every room gets a few extra, unlabeled set-dressing props (a spare pillar, an
        // off-angle chair, a stray decal) on top of its puzzle objects so rooms read as
        // furnished spaces rather than a single prop floating in an empty box, and so no two
        // rooms share the same silhouette even at a glance.

        static void PopulateAllRooms()
        {
            foreach (var r in allRooms)
            {
                switch (r.kind)
                {
                    case RoomKind.Start: Populate_Start(r); break;
                    case RoomKind.Displaced: Populate_Displaced(r); break;
                    case RoomKind.LockedCounter: Populate_LockedCounter(r); break;
                    case RoomKind.Repeat: Populate_Repeat(r); break;
                    case RoomKind.PatternAndSlot: Populate_PatternAndSlot(r); break;
                    case RoomKind.PortableLight: Populate_PortableLight(r); break;
                    case RoomKind.Aftermath: Populate_Aftermath(r); break;
                    case RoomKind.MirrorAndCentre: Populate_MirrorAndCentre(r); break;
                    case RoomKind.FinalAlignment: Populate_FinalAlignment(r); break;
                }
            }
        }

        static void Populate_Start(RoomDef r) { /* player + capsule are placed separately */ }

        static void Populate_Displaced(RoomDef r)
        {
            // Cabinet displaced to a corner, shoved in carelessly; clean rectangular trace left
            // behind at its old spot, with a faint drag mark connecting the two.
            var desk = SpawnLocal(P["Desk"], r.contentGroup, new Vector3(2f, 0, 2f), Quaternion.Euler(0, 15f, 0));
            var decal = SpawnLocal(P["Trace_Decal"], r.contentGroup, new Vector3(0, 0, -0.8f), Quaternion.identity);
            decal.transform.localScale = new Vector3(1.4f, 1f, 0.7f);

            var drag = SpawnLocal(P["Trace_Decal"], r.contentGroup, new Vector3(1f, 0, 0.2f), Quaternion.Euler(0, 35f, 0));
            drag.transform.localScale = new Vector3(0.3f, 1f, 1.6f);

            SpawnLocal(P["Chair"], r.contentGroup, new Vector3(-1.8f, 0, -1.5f), Quaternion.Euler(0, 60f, 0));
            SpawnLocal(P["Pillar"], r.contentGroup, new Vector3(-2f, 0, 2f), Quaternion.identity);
        }

        // Room 2 (sample storage): base set dressing only. Room2GameplayBuilder (Step 3b) finds
        // these by name and wires up the actual puzzle — reader, cabinet lock, ceiling card panel,
        // matrix screen, stool carrying, exit keypad — same division of labor Populate_Displaced
        // / GameplayBuilder use for Room 1. "北墙中间" is nudged off dead-center (-1.2 instead of
        // 0) because x=0 on the north wall is the door opening to Room F — the Wall_Door swap for
        // that connection already claims that segment, so the screen sits just to its side instead.
        static void Populate_LockedCounter(RoomDef r)
        {
            var cabinet = SpawnLocal(P["Counter_Drawer"], r.contentGroup, new Vector3(0, 0, -2.5f), Quaternion.identity);
            cabinet.name = "Gameplay_SampleCabinet";

            var stool = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stool.name = "Gameplay_Stool";
            stool.transform.SetParent(r.contentGroup, false);
            stool.transform.localPosition = new Vector3(-2f, 0.225f, -2f);
            stool.transform.localScale = new Vector3(0.4f, 0.45f, 0.4f);
            stool.GetComponent<Renderer>().sharedMaterial = MatStructure;

            var matrix = GameObject.CreatePrimitive(PrimitiveType.Cube);
            matrix.name = "Gameplay_DataMatrix";
            matrix.transform.SetParent(r.contentGroup, false);
            matrix.transform.localPosition = new Vector3(-1.2f, 1.5f, RoomHalf - 0.03f);
            matrix.transform.localScale = new Vector3(1.2f, 0.6f, 0.05f);
            matrix.GetComponent<Renderer>().sharedMaterial = MatStructure;

            SpawnLocal(P["Pillar"], r.contentGroup, new Vector3(2f, 0, 2f), Quaternion.identity);
        }

        // Room 3 (instrument repair): base set dressing only — Room3GameplayBuilder (Step 3c)
        // finds these by name and wires up the cradle assembly, nameplate reveal and breaker
        // panel, same division of labor as Room 1/2. The cradle sits on the west wall's "Left"
        // segment (away from the west-center door to F); the breaker box sits on the north wall's
        // "Right" segment (away from the north-center door to Room 4).
        static void Populate_PatternAndSlot(RoomDef r)
        {
            // Both "Gameplay_Cradle" and "Gameplay_BreakerBox" are UNSCALED root objects with
            // their visible geometry on a separate "Mesh" child — Room3GameplayBuilder parents
            // several more small interactive pieces under each of these roots, and scale
            // compounds multiplicatively down a transform chain. Putting the non-uniform scale
            // directly on the thing other scripts parent children under would squash those
            // children (the exact bug Room 1's drawer/paper hit earlier).
            var cradle = NewUnscaledRoot("Gameplay_Cradle", r.contentGroup,
                new Vector3(-RoomHalf + 0.05f, 1f, 2f), Quaternion.Euler(0, 90f, 0));
            AddScaledMesh(cradle, new Vector3(0.5f, 0.35f, 0.05f), MatStructure);
            CreateLabel("SIDE-LIGHT INSPECTION", cradle.transform, new Vector3(0, -0.3f, 0), r.contentGroup, r.instance.position);

            var workbench = SpawnLocal(P["Desk"], r.contentGroup, new Vector3(0, 0, 0), Quaternion.identity);
            workbench.name = "Gameplay_Workbench";

            var breakerBox = NewUnscaledRoot("Gameplay_BreakerBox", r.contentGroup,
                new Vector3(2f, 1.1f, RoomHalf - 0.1f), Quaternion.identity);
            AddScaledMesh(breakerBox, new Vector3(0.9f, 0.5f, 0.15f), MatStructure);

            SpawnLocal(P["Pillar"], r.contentGroup, new Vector3(2f, 0, -2f), Quaternion.identity);
        }

        static GameObject NewUnscaledRoot(string name, Transform parent, Vector3 localPos, Quaternion localRot)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            return go;
        }

        static GameObject AddScaledMesh(GameObject root, Vector3 scale, Material mat)
        {
            GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = "Mesh";
            UnityEngine.Object.DestroyImmediate(mesh.GetComponent<Collider>()); // purely visual; interaction colliders live on the specific sub-parts
            mesh.transform.SetParent(root.transform, false);
            mesh.transform.localScale = scale;
            mesh.GetComponent<Renderer>().sharedMaterial = mat;
            return mesh;
        }

        static void Populate_PortableLight(RoomDef r)
        {
            // Both lamp parts live in this one room now, spread to opposite corners so the
            // player still has to search the space rather than see everything from the door.
            var desk = SpawnLocal(P["Desk"], r.contentGroup, new Vector3(-2f, 0, -1.5f), Quaternion.Euler(0, 90f, 0));
            var partA = SpawnLocal(P["Lamp_PartA"], desk.transform, new Vector3(0, 0.375f, 0), Quaternion.identity);
            CreateLabel("LAMP PART A", partA.transform, new Vector3(0, 0.3f, 0), r.contentGroup, r.instance.position);

            var counter = SpawnLocal(P["Counter_Drawer"], r.contentGroup, new Vector3(2f, 0, 2f), Quaternion.Euler(0, 180f, 0));
            var partB = SpawnLocal(P["Lamp_PartB"], counter.transform, new Vector3(0, 0.9f, 0), Quaternion.identity);
            CreateLabel("LAMP PART B", partB.transform, new Vector3(0, 0.3f, 0), r.contentGroup, r.instance.position);

            SpawnLocal(P["Chair"], r.contentGroup, new Vector3(-1.2f, 0, -1.5f), Quaternion.Euler(0, 250f, 0));
            SpawnLocal(P["Pillar"], r.contentGroup, new Vector3(2f, 0, -2f), Quaternion.identity);

            var dust = SpawnLocal(P["Trace_Decal"], r.contentGroup, new Vector3(0, 0, 0.5f), Quaternion.identity);
            dust.transform.localScale = new Vector3(0.4f, 1f, 0.4f);
        }

        static void Populate_Aftermath(RoomDef r)
        {
            // Merges the old "Hidden Writing" and "Silent Room" beats: a note, a hastily
            // pushed chair, a clean ring where a lamp used to sit, a torn page, and a
            // handprint by the door — the richest room in the level.
            var desk = SpawnLocal(P["Desk"], r.contentGroup, new Vector3(-1.5f, 0, 2f), Quaternion.identity);
            var note = SpawnLocal(P["Paper"], desk.transform, new Vector3(0.15f, 0.375f, 0.1f), Quaternion.Euler(0, 12f, 0));
            CreateLabel("NOTE", note.transform, new Vector3(0, 0.25f, 0), r.contentGroup, r.instance.position);

            var chair = SpawnLocal(P["Chair"], r.contentGroup, new Vector3(-1.5f, 0, 0.9f), Quaternion.Euler(0, 140f, 0));
            CreateLabel("PUSHED CHAIR", chair.transform, new Vector3(0, 0.9f, 0), r.contentGroup, r.instance.position);

            var ring = SpawnLocal(P["Trace_Decal"], r.contentGroup, new Vector3(1.6f, 0, 1.6f), Quaternion.identity);
            ring.transform.localScale = new Vector3(0.4f, 1f, 0.4f);
            CreateLabel("LAMP RING", ring.transform, new Vector3(0, 0.3f, 0), r.contentGroup, r.instance.position);

            var page = SpawnLocal(P["Paper"], r.contentGroup, new Vector3(0.5f, 0, 1f), Quaternion.Euler(0, 35f, 0));
            CreateLabel("TORN PAGE", page.transform, new Vector3(0, 0.25f, 0), r.contentGroup, r.instance.position);

            var handprint = SpawnLocal(P["Trace_Decal"], r.contentGroup, new Vector3(RoomHalf - 0.001f, 1.2f, -2f), Quaternion.Euler(0, -90, 90));
            handprint.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
            CreateLabel("HANDPRINT", handprint.transform, new Vector3(0, 0, -0.2f), r.contentGroup, r.instance.position);

            var counter = SpawnLocal(P["Counter_Drawer"], r.contentGroup, new Vector3(2f, 0, -2f), Quaternion.Euler(0, -90f, 0));
            SpawnLocal(P["Pillar"], r.contentGroup, new Vector3(-2f, 0, -2f), Quaternion.identity);
        }

        static void Populate_MirrorAndCentre(RoomDef r)
        {
            // Merges "Reflected Clue" and "False Centre": mirrored marks read via the panel,
            // plus a showy red-herring object with the real clue tucked in a corner.
            string[] marks = { "3", "5", "1" };
            float[] xs = { -1f, 0f, 1f };
            for (int i = 0; i < marks.Length; i++)
            {
                var mark = SpawnLocal(P["Trace_Decal"], r.contentGroup, new Vector3(xs[i], 1.4f, RoomHalf - 0.001f), Quaternion.Euler(90, 0, 0));
                mark.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
                var label = CreateLabel(marks[i], mark.transform, new Vector3(0, 0, -0.15f), r.contentGroup, r.instance.position, doubleSided: false);
                if (label != null)
                    label.transform.localScale = new Vector3(-label.transform.localScale.x, label.transform.localScale.y, label.transform.localScale.z);
            }
            var mirror = SpawnLocal(P["Mirror_Panel"], r.contentGroup, new Vector3(0, 1.6f, -RoomHalf + 0.001f), Quaternion.identity);
            CreateLabel("MIRROR", mirror.transform, new Vector3(0, 1f, 0), r.contentGroup, r.instance.position);

            var big = SpawnLocal(P["Desk"], r.contentGroup, new Vector3(-1f, 0, 0.5f), Quaternion.identity);
            big.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
            foreach (var rend in big.GetComponentsInChildren<Renderer>())
                rend.sharedMaterial = MatInteractive;
            CreateLabel("OBJECT", big.transform, new Vector3(0, 1.6f, 0), r.contentGroup, r.instance.position);

            var trail = SpawnLocal(P["Trace_Decal"], r.contentGroup, new Vector3(2f, 0, 1.8f), Quaternion.identity);
            trail.transform.localScale = new Vector3(1.2f, 1f, 0.3f);
            CreateLabel("WORN TRACE", trail.transform, new Vector3(0, 0.3f, 0), r.contentGroup, r.instance.position);

            var key = SpawnLocal(P["Key"], r.contentGroup, new Vector3(2.5f, 0, 2.5f), Quaternion.Euler(0, 20f, 0));
            CreateLabel("KEY", key.transform, new Vector3(0, 0.3f, 0), r.contentGroup, r.instance.position);

            SpawnLocal(P["Pillar"], r.contentGroup, new Vector3(2f, 0, -2f), Quaternion.identity);
        }

        static void Populate_FinalAlignment(RoomDef r)
        {
            var dial = SpawnLocal(P["Dial_Panel"], r.contentGroup, new Vector3(1.5f, 1.4f, -RoomHalf + 0.001f), Quaternion.identity);
            CreateLabel("DIAL", dial.transform, new Vector3(0, 0.6f, 0), r.contentGroup, r.instance.position);

            var desk = SpawnLocal(P["Desk"], r.contentGroup, new Vector3(-1.8f, 0, -1.8f), Quaternion.Euler(0, 45f, 0));
            var log = SpawnLocal(P["Paper"], desk.transform, new Vector3(0, 0.375f, 0), Quaternion.identity);
            CreateLabel("LOG", log.transform, new Vector3(0, 0.25f, 0), r.contentGroup, r.instance.position);

            SpawnLocal(P["Chair"], r.contentGroup, new Vector3(-1.8f, 0, -0.9f), Quaternion.Euler(0, 200f, 0));
            SpawnLocal(P["Pillar"], r.contentGroup, new Vector3(2f, 0, 2f), Quaternion.identity);

            // Exit door on the free wall (West), temporarily sealed with a plain slab.
            Transform walls = r.instance.Find("Walls");
            Transform westCenter = walls.Find("Wall_West_Center");
            if (westCenter != null)
            {
                Vector3 pos = westCenter.position;
                Quaternion rot = westCenter.rotation;
                UnityEngine.Object.DestroyImmediate(westCenter.gameObject);
                GameObject exitDoor = (GameObject)PrefabUtility.InstantiatePrefab(P["Wall_Door"], walls);
                exitDoor.name = "Wall_West_Center";
                exitDoor.transform.position = pos;
                exitDoor.transform.rotation = rot;

                GameObject seal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seal.name = "Exit_Seal";
                seal.transform.SetParent(r.contentGroup, false);
                seal.transform.position = pos + rot * new Vector3(0, 1.05f, 0.1f);
                seal.transform.rotation = rot;
                seal.transform.localScale = new Vector3(1f, 2.1f, 0.2f);
                seal.GetComponent<Renderer>().sharedMaterial = MatStructure;
                CreateLabel("SEALED EXIT", seal.transform, new Vector3(0, 1.3f, 0), r.contentGroup, r.instance.position);
            }
        }

        // F: the transition room between Room 2 and Room 3. No timer, no lock — bare, same style
        // as the level's other near-identical "repeat" rooms, just a pillar for structural
        // consistency plus the one thing that matters: an inspection lamp base sitting on the
        // east wall shelf (Room3GameplayBuilder wires up the actual pickup). The shelf sits on
        // the east wall's "Left" segment, away from the east-center door to Room 3.
        static void Populate_Repeat(RoomDef r)
        {
            SpawnLocal(P["Pillar"], r.contentGroup, new Vector3(-2f, 0, -2f), Quaternion.identity);

            // Unscaled root + separate scaled "Mesh" child (see Populate_PatternAndSlot's comment
            // on NewUnscaledRoot) since Room3GameplayBuilder parents the actual pickup under this.
            var shelf = NewUnscaledRoot("Gameplay_LampBaseShelf", r.contentGroup,
                new Vector3(RoomHalf - 0.05f, 1f, 2f), Quaternion.Euler(0, -90f, 0));
            AddScaledMesh(shelf, new Vector3(0.5f, 0.04f, 0.3f), MatStructure);
        }

        // ---------------- Player & scale reference ----------------

        static void SetupPlayerAndScale()
        {
            RoomDef start = byCoord[(0, 0)];
            Vector3 origin = WorldPos(start);

            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "ScaleReference_Capsule_1.8m";
            capsule.transform.SetParent(playerGroup, false);
            capsule.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
            capsule.transform.position = origin + new Vector3(0.8f, 0.9f, 0);
            var mr = capsule.GetComponent<Renderer>();
            mr.sharedMaterial = MatStructure;

            GameObject flyCamPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StudentTools/FlyCamera/FlyCamera.prefab");
            if (flyCamPrefab != null)
            {
                GameObject cam = (GameObject)PrefabUtility.InstantiatePrefab(flyCamPrefab, playerGroup);
                cam.transform.position = origin + new Vector3(-0.8f, 1.6f, -1.5f);
                cam.transform.rotation = Quaternion.identity;
            }
            else
            {
                Debug.LogWarning("[WhiteBox] FlyCamera prefab not found under Assets/StudentTools/FlyCamera/. Run Step 1 after placing FlyCamera.unitypackage in <project root>/FlyCameraImport/.");
            }
        }

        // ---------------- Recorded walkthrough camera ----------------
        // A dedicated camera that automatically flies through every room in build order, for
        // screen-recording the level. Left inactive by default so normal Play still uses
        // FlyCamera unchanged; tick "TourCamera" on in the Hierarchy before pressing Play to
        // use it instead (its own Awake() disables FlyCamera automatically).

        static void BuildCameraTour()
        {
            GameObject tourGroup = new GameObject("CameraTour");

            GameObject waypointsGroup = new GameObject("Waypoints");
            waypointsGroup.transform.SetParent(tourGroup.transform, false);

            var waypoints = new List<Transform>();
            foreach (var r in allRooms)
            {
                GameObject wp = new GameObject("WP_" + r.instance.name);
                wp.transform.SetParent(waypointsGroup.transform, false);
                wp.transform.position = r.instance.position + new Vector3(0, 1.6f, 0);
                waypoints.Add(wp.transform);
            }

            GameObject camGO = new GameObject("TourCamera");
            camGO.transform.SetParent(tourGroup.transform, false);
            Camera cam = camGO.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            camGO.AddComponent<AudioListener>();
            var driver = camGO.AddComponent<CameraTourDriver>();
            driver.waypoints = waypoints.ToArray();
            camGO.SetActive(false);
        }
    }
}
#endif
