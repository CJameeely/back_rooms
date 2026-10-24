#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using static WhiteBoxKit.WhiteBoxModules;

namespace WhiteBoxKit
{
    /// Step 3c: layers F's lamp-base pickup and Room 3's (R3_PatternAndSlot, "instrument repair")
    /// gameplay on top of their Populate_Repeat / Populate_PatternAndSlot set dressing. Must run
    /// after Step 3 (shared Inventory/ObjectiveToast/player singletons). Clears its own additions
    /// first, so it's safe to re-run on its own.
    public static class Room3GameplayBuilder
    {
        const string ScenePath = "Assets/Scenes/WhiteBox_LastLight.unity";
        const float DoorGapHeight = 2.1f;
        const float RoomTimerDuration = 270f;

        [MenuItem("Tools/WhiteBox/Step 3c - Build F + Room3 Gameplay")]
        public static void Step3c_BuildFAndRoom3Gameplay()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            LoadMaterials();

            GameObject roomsGroup = GameObject.Find("Rooms");
            if (roomsGroup == null) { Debug.LogError("[WhiteBox] Rooms group not found — run Step 2 first."); return; }
            Transform roomF = FindRoomContaining(roomsGroup.transform, "E1_Repeat");
            Transform room3 = FindRoomContaining(roomsGroup.transform, "R3_PatternAndSlot");
            if (roomF == null || room3 == null) { Debug.LogError("[WhiteBox] Could not find F (E1_Repeat) or Room 3 (R3_PatternAndSlot)."); return; }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null || GameObject.Find("GameplayCanvas") == null)
            {
                Debug.LogError("[WhiteBox] Player/GameplayCanvas not found — run Step 3 (Room 1) first.");
                return;
            }

            Transform contentF = roomF.Find("Content");
            Transform content3 = room3.Find("Content");
            Transform walls3 = room3.Find("Walls");
            Transform ceiling3 = room3.Find("Ceiling");

            ClearPrevious(contentF, content3, player);

            BuildLampBasePickup(contentF);
            InspectionLamp lamp = BuildInspectionLamp(player);
            BuildCradle(content3, lamp);
            BuildLampHeadPickup(content3);

            GameObject managers = new GameObject("Room3GameplayManagers");
            RoomTimer timer = managers.AddComponent<RoomTimer>();
            timer.roomNumber = 3;
            timer.totalDuration = RoomTimerDuration;
            var lights = new System.Collections.Generic.List<Light>();
            var panels = new System.Collections.Generic.List<Renderer>();
            foreach (Transform tile in ceiling3)
            {
                Light l = tile.GetComponentInChildren<Light>();
                if (l == null) continue;
                lights.Add(l);
                panels.Add(tile.Find("Mesh_Panel")?.GetComponent<Renderer>());
            }
            timer.roomLights = lights.ToArray();
            timer.roomPanels = panels.ToArray();

            BuildBreakerPanelAndDoor(walls3, content3, timer);
            BuildEntry(content3, walls3, timer);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[WhiteBox] Step 3c complete: F and Room 3 are now playable.");
        }

        static Transform FindRoomContaining(Transform roomsGroup, string contains)
        {
            foreach (Transform child in roomsGroup)
                if (child.name.Contains(contains))
                    return child;
            return null;
        }

        static void ClearPrevious(Transform contentF, Transform content3, GameObject player)
        {
            foreach (string n in new[] { "Room3GameplayManagers" })
            {
                GameObject go = GameObject.Find(n);
                if (go != null) Object.DestroyImmediate(go);
            }
            foreach (string n in new[] { "Gameplay_LampHead", "Gameplay_ExitDoor3", "Gameplay_RoomExit3", "Gameplay_Entry3West", "Gameplay_Nameplate" })
            {
                Transform t = content3.Find(n);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }
            Transform lampBaseShelf = contentF.Find("Gameplay_LampBaseShelf");
            if (lampBaseShelf != null)
            {
                Transform pickup = lampBaseShelf.Find("Pickup");
                if (pickup != null) Object.DestroyImmediate(pickup.gameObject);
            }
            // "Gameplay_Cradle" and "Gameplay_BreakerBox" (with their "Mesh" child) are base
            // furniture from Step 2/Populate_PatternAndSlot and must persist across a Step 3c-only
            // rerun — only the gameplay-added children and components get cleared here, by name,
            // never the whole root or its "Mesh".
            Transform cradle = content3.Find("Gameplay_Cradle");
            if (cradle != null)
            {
                var cc = cradle.GetComponent<InspectionLampCradle>();
                if (cc != null) Object.DestroyImmediate(cc);
                var cradleCol = cradle.GetComponent<BoxCollider>();
                if (cradleCol != null) Object.DestroyImmediate(cradleCol);
                foreach (string n in new[] { "Outline", "BaseOnly", "HeadOnly", "Assembled" })
                {
                    Transform child = cradle.Find(n);
                    if (child != null) Object.DestroyImmediate(child.gameObject);
                }
            }
            Transform breakerBox = content3.Find("Gameplay_BreakerBox");
            if (breakerBox != null)
            {
                foreach (string n in new[] { "Breaker_0", "Breaker_1", "Breaker_2", "Breaker_3", "MainBreakerLever", "StatusLight" })
                {
                    Transform child = breakerBox.Find(n);
                    if (child != null) Object.DestroyImmediate(child.gameObject);
                }
            }
            var oldLamp = player.GetComponent<InspectionLamp>();
            if (oldLamp != null) Object.DestroyImmediate(oldLamp);
            Transform oldSpot = player.transform.Find("Camera/InspectionSpot");
            if (oldSpot != null) Object.DestroyImmediate(oldSpot.gameObject);
        }

        // ---------------- F: lamp base pickup ----------------

        static void BuildLampBasePickup(Transform contentF)
        {
            Transform shelf = contentF.Find("Gameplay_LampBaseShelf");
            if (shelf == null) { Debug.LogWarning("[WhiteBox] F's lamp base shelf not found."); return; }

            // shelf is an unscaled root (see NewUnscaledRoot/AddScaledMesh) with its own "Mesh"
            // child 0.04 thick, top surface at local y=+0.02 — sit the pickup flush on top of it.
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickup.name = "Pickup";
            pickup.transform.SetParent(shelf, false);
            pickup.transform.localPosition = new Vector3(0, 0.06f, 0);
            pickup.transform.localScale = new Vector3(0.15f, 0.08f, 0.12f);
            pickup.GetComponent<Renderer>().sharedMaterial = MatInteractive;

            InventoryPickup ip = pickup.AddComponent<InventoryPickup>();
            ip.item = new InventoryItem(InspectionLampCradle.BaseItemId, "Inspection Lamp Base", new Color(0.95f, 0.55f, 0.2f));
            ip.prompt = "Pick up inspection lamp base";
        }

        // ---------------- Shared: inspection lamp spotlight on the player ----------------

        static InspectionLamp BuildInspectionLamp(GameObject player)
        {
            Transform cam = player.transform.Find("Camera");
            GameObject spotGO = new GameObject("InspectionSpot");
            spotGO.transform.SetParent(cam, false);
            Light spot = spotGO.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.spotAngle = 25f;
            spot.range = 4f;
            spot.intensity = 3f;
            spot.color = new Color(1f, 0.96f, 0.85f);
            spot.shadows = LightShadows.None;

            InspectionLamp lamp = player.AddComponent<InspectionLamp>();
            lamp.spotLight = spot;
            return lamp;
        }

        // ---------------- Mechanic A: cradle assembly ----------------

        static void BuildCradle(Transform content3, InspectionLamp lamp)
        {
            Transform cradleRoot = content3.Find("Gameplay_Cradle");
            if (cradleRoot == null) { Debug.LogWarning("[WhiteBox] Room 3 cradle not found."); return; }

            // Dusty outline: always visible until assembled.
            GameObject outline = GameObject.CreatePrimitive(PrimitiveType.Cube);
            outline.name = "Outline";
            outline.transform.SetParent(cradleRoot, false);
            Object.DestroyImmediate(outline.GetComponent<Collider>());
            outline.transform.localPosition = new Vector3(0, 0, 0.03f);
            outline.transform.localScale = new Vector3(0.9f, 0.9f, 0.1f);
            Material dusty = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.55f, 0.55f, 0.5f) };
            outline.GetComponent<Renderer>().sharedMaterial = dusty;

            GameObject baseOnly = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseOnly.name = "BaseOnly";
            baseOnly.transform.SetParent(cradleRoot, false);
            Object.DestroyImmediate(baseOnly.GetComponent<Collider>());
            baseOnly.transform.localPosition = new Vector3(0, -0.1f, 0.08f);
            baseOnly.transform.localScale = new Vector3(0.6f, 0.4f, 0.15f);
            baseOnly.GetComponent<Renderer>().sharedMaterial = MatStructure;
            baseOnly.SetActive(false);

            GameObject headOnly = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headOnly.name = "HeadOnly";
            headOnly.transform.SetParent(cradleRoot, false);
            Object.DestroyImmediate(headOnly.GetComponent<Collider>());
            headOnly.transform.localPosition = new Vector3(0.15f, 0.15f, 0.1f);
            headOnly.transform.localRotation = Quaternion.Euler(0, 0, 25f); // placed crooked
            headOnly.transform.localScale = new Vector3(0.3f, 0.3f, 0.2f);
            headOnly.GetComponent<Renderer>().sharedMaterial = MatStructure;
            headOnly.SetActive(false);

            GameObject assembled = GameObject.CreatePrimitive(PrimitiveType.Cube);
            assembled.name = "Assembled";
            assembled.transform.SetParent(cradleRoot, false);
            Object.DestroyImmediate(assembled.GetComponent<Collider>());
            assembled.transform.localPosition = new Vector3(0, 0, 0.1f);
            assembled.transform.localScale = new Vector3(0.7f, 0.6f, 0.25f);
            assembled.GetComponent<Renderer>().sharedMaterial = MatInteractive;
            assembled.SetActive(false);

            // Gameplay_Cradle is now an unscaled root (its visual "Mesh" child's collider was
            // deliberately removed — see AddScaledMesh), so the root needs its own collider here
            // for the player's raycast to actually hit something.
            BoxCollider cradleCol = cradleRoot.gameObject.AddComponent<BoxCollider>();
            cradleCol.center = new Vector3(0, 0, 0.08f);
            cradleCol.size = new Vector3(0.9f, 0.9f, 0.35f);

            InspectionLampCradle cradle = cradleRoot.gameObject.AddComponent<InspectionLampCradle>();
            cradle.outlineVisual = outline;
            cradle.baseOnlyVisual = baseOnly;
            cradle.headOnlyVisual = headOnly;
            cradle.assembledVisual = assembled;
            cradle.assembledFlashRenderer = assembled.GetComponent<Renderer>();
        }

        static void BuildLampHeadPickup(Transform content3)
        {
            Transform workbench = content3.Find("Gameplay_Workbench");
            if (workbench == null) { Debug.LogWarning("[WhiteBox] Room 3 workbench not found."); return; }

            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickup.name = "Gameplay_LampHead";
            pickup.transform.SetParent(workbench, false);
            pickup.transform.localPosition = new Vector3(0, 0.12f, 0);
            pickup.transform.localScale = new Vector3(0.14f, 0.1f, 0.14f);
            pickup.GetComponent<Renderer>().sharedMaterial = MatInteractive;

            InventoryPickup ip = pickup.AddComponent<InventoryPickup>();
            ip.item = new InventoryItem(InspectionLampCradle.HeadItemId, "Inspection Lamp Head", new Color(0.95f, 0.55f, 0.2f));
            ip.prompt = "Pick up inspection lamp head";
        }

        // ---------------- Mechanic B: breaker panel + nameplate + exit door ----------------

        static void BuildBreakerPanelAndDoor(Transform walls, Transform content, RoomTimer timer)
        {
            Transform breakerBox = content.Find("Gameplay_BreakerBox");
            if (breakerBox == null) { Debug.LogWarning("[WhiteBox] Room 3 breaker box not found."); return; }

            float[] breakerX = { -0.3f, -0.1f, 0.1f, 0.3f };
            BreakerSwitch[] breakers = new BreakerSwitch[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject lever = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lever.name = $"Breaker_{i}";
                lever.transform.SetParent(breakerBox, false);
                lever.transform.localPosition = new Vector3(breakerX[i], 0, 0.1f);
                lever.transform.localScale = new Vector3(0.1f, 0.2f, 0.2f);
                lever.GetComponent<Renderer>().sharedMaterial = MatInteractive;
                BreakerSwitch sw = lever.AddComponent<BreakerSwitch>();
                sw.leverVisual = lever.transform;
                sw.SetUp(false);
                breakers[i] = sw;
            }

            GameObject mainLever = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainLever.name = "MainBreakerLever";
            mainLever.transform.SetParent(breakerBox, false);
            mainLever.transform.localPosition = new Vector3(0.65f, 0, 0.1f);
            mainLever.transform.localScale = new Vector3(0.15f, 0.35f, 0.2f);
            mainLever.GetComponent<Renderer>().sharedMaterial = MatInteractive;

            GameObject statusLight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            statusLight.name = "StatusLight";
            statusLight.transform.SetParent(breakerBox, false);
            Object.DestroyImmediate(statusLight.GetComponent<Collider>());
            statusLight.transform.localPosition = new Vector3(0, 0.35f, 0.1f);
            statusLight.transform.localScale = new Vector3(0.1f, 0.1f, 0.05f);
            Renderer statusRend = statusLight.GetComponent<Renderer>();
            statusRend.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            // MainBreaker lives on the lever itself (not the box root — the box root has no
            // collider of its own since its "Mesh" child's collider was deliberately removed).
            MainBreaker mb = mainLever.AddComponent<MainBreaker>();
            mb.breakers = breakers;
            mb.statusLightRenderer = statusRend;
            mb.penaltyTimer = timer;

            // Nameplate, directly above the 4 small breakers (not the main one). breakerBox is an
            // unscaled root, so its localPosition is a plain room-local offset.
            GameObject nameplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nameplate.name = "Gameplay_Nameplate";
            nameplate.transform.SetParent(content, false);
            Object.DestroyImmediate(nameplate.GetComponent<Collider>());
            nameplate.transform.localPosition = breakerBox.localPosition + new Vector3(-0.2f, 0.5f, -0.05f);
            nameplate.transform.localScale = new Vector3(0.6f, 0.3f, 0.05f);
            nameplate.GetComponent<Renderer>().sharedMaterial = MatStructure;

            TMP_Text[] arrows = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject labelGO = CreateLabel("?", nameplate.transform, new Vector3(breakerX[i] * 0.85f, 0, -0.03f), content, content.parent.position, doubleSided: false);
                arrows[i] = labelGO.GetComponent<TMP_Text>();
                arrows[i].alpha = 0f;
            }
            NameplateArrows arrowSetter = nameplate.AddComponent<NameplateArrows>();
            arrowSetter.arrows = arrows;
            GrazingLightReveal reveal = nameplate.AddComponent<GrazingLightReveal>();
            reveal.revealedLabels = arrows;

            // Exit door: north wall center, sealed slab (no keypad — MainBreaker drives it).
            Transform doorFrame = walls.Find("Wall_North_Center");
            if (doorFrame == null) { Debug.LogWarning("[WhiteBox] Room 3 north door not found; skipping exit."); return; }

            Vector3 doorPos = doorFrame.position;
            Quaternion doorRot = doorFrame.rotation;
            Vector3 outward = doorRot * Vector3.forward;

            GameObject seal = new GameObject("Gameplay_ExitDoor3");
            seal.transform.SetParent(content, false);
            seal.transform.position = doorPos + outward * 0.02f + Vector3.up * (DoorGapHeight / 2f);
            seal.transform.rotation = doorRot;
            SlideMover doorMover = seal.AddComponent<SlideMover>();

            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "Slab";
            slab.transform.SetParent(seal.transform, false);
            slab.transform.localScale = new Vector3(1f, DoorGapHeight, 0.15f);
            slab.GetComponent<Renderer>().sharedMaterial = MatInteractive;

            mb.door = doorMover;
            mb.doorOpenLocalPosition = seal.transform.localPosition + Vector3.up * 2.2f;

            GameObject exit = new GameObject("Gameplay_RoomExit3", typeof(BoxCollider));
            exit.transform.SetParent(content, false);
            exit.transform.position = doorPos + outward * 1.2f + Vector3.up * (DoorGapHeight / 2f);
            exit.transform.rotation = doorRot;
            BoxCollider col = exit.GetComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(1.2f, DoorGapHeight, 1.6f);
            RoomExitTrigger exitTrigger = exit.AddComponent<RoomExitTrigger>();
            exitTrigger.roomNumber = 3;
            exitTrigger.isFinalExit = false;
        }

        // ---------------- Room entry (west doorway, from F) ----------------

        static void BuildEntry(Transform content, Transform walls, RoomTimer timer)
        {
            Transform westDoor = walls.Find("Wall_West_Center");
            if (westDoor == null) return;

            GameObject entry = new GameObject("Gameplay_Entry3West", typeof(BoxCollider));
            entry.transform.SetParent(content, false);
            entry.transform.position = westDoor.position + westDoor.rotation * Vector3.forward * 0.5f + Vector3.up * 1.2f;
            entry.transform.rotation = westDoor.rotation;
            BoxCollider col = entry.GetComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(1.4f, 2.4f, 1.2f);
            RoomEntryTrigger rt = entry.AddComponent<RoomEntryTrigger>();
            rt.roomNumber = 3;
            rt.timer = timer;
            rt.objectiveText = "Something is missing from the cradle. The blank plate isn't blank.";
        }
    }
}
#endif
