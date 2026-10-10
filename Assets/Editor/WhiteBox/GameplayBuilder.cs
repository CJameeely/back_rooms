#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using static WhiteBoxKit.WhiteBoxModules;

namespace WhiteBoxKit
{
    /// Step 3: layers a small, playable escape-room on top of the already-generated Room 1
    /// (R1 Displaced Object). Must run after Step 2 has built WhiteBox_LastLight.unity. Re-run
    /// safely any time — it clears its own "Gameplay" groups first, so it never duplicates.
    public static class GameplayBuilder
    {
        const string ScenePath = "Assets/Scenes/WhiteBox_LastLight.unity";
        const float DoorGapHeight = 2.1f;

        [MenuItem("Tools/WhiteBox/Step 3 - Build Room1 Gameplay")]
        public static void Step3_BuildRoom1Gameplay()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Dictionary<string, GameObject> prefabs = LoadPrefabs();
            LoadMaterials();

            GameObject roomsGroup = GameObject.Find("Rooms");
            GameObject playerGroup = GameObject.Find("Player");
            if (roomsGroup == null || playerGroup == null)
            {
                Debug.LogError("[WhiteBox] Rooms/Player groups not found — run Step 2 first.");
                return;
            }

            Transform room1 = FindRoomContaining(roomsGroup.transform, "R1_Displaced");
            Transform room2Marker = FindRoomContaining(roomsGroup.transform, "R2_LockedCounter");
            if (room1 == null)
            {
                Debug.LogError("[WhiteBox] Could not find the R1_Displaced room instance.");
                return;
            }
            Transform content = room1.Find("Content");
            Transform walls = room1.Find("Walls");
            Transform ceiling = room1.Find("Ceiling");
            Vector3 roomCenter = room1.position;

            ClearPreviousGameplay(content, playerGroup.transform, scene);

            BuildPlayer(playerGroup.transform, roomCenter, out GameObject player, out Camera playerCam);
            DisableFlyCamera(playerGroup.transform);

            Canvas canvas = UIBuilder.CreateScreenCanvas("GameplayCanvas");
            BuildCrosshairAndPrompt(canvas.transform, player);

            GameObject managers = new GameObject("GameplayManagers");
            CodeManager codeManager = managers.AddComponent<CodeManager>();
            ClueTracker clueTracker = BuildClueTrackerUI(canvas.transform);

            BuildCabinetClue(content, roomCenter);
            BuildDrawerAndPaperClue(content, prefabs, roomCenter, canvas.transform);
            BuildPillarClue(content, roomCenter);
            LightSwitchClue switchClue = BuildLightSwitchClue(content, prefabs, ceiling, roomCenter);

            BuildDoorKeypadAndWin(walls, content, prefabs, canvas.transform, roomCenter);

            BuildLightsOutTimer(managers.transform, ceiling, switchClue);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[WhiteBox] Step 3 complete: Room 1 is now playable.");
        }

        static Transform FindRoomContaining(Transform roomsGroup, string contains)
        {
            foreach (Transform child in roomsGroup)
                if (child.name.Contains(contains))
                    return child;
            return null;
        }

        static void ClearPreviousGameplay(Transform content, Transform playerGroup, Scene scene)
        {
            foreach (string n in new[] { "GameplayManagers", "GameplayCanvas" })
            {
                GameObject go = GameObject.Find(n);
                if (go != null) Object.DestroyImmediate(go);
            }
            Transform old = playerGroup.Find("PlayerController");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            Transform capsule = playerGroup.Find("ScaleReference_Capsule_1.8m");
            if (capsule != null) Object.DestroyImmediate(capsule.gameObject);

            foreach (string n in new[] { "Gameplay_Cabinet", "Gameplay_Drawer", "Gameplay_Switch",
                "Gameplay_Keypad", "Gameplay_Door", "Gameplay_WinTrigger", "Gameplay_PillarClue",
                "Gameplay_CabinetReveal" })
            {
                Transform t = content.Find(n);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }
        }

        // ---------------- Player ----------------

        static void BuildPlayer(Transform playerGroup, Vector3 roomCenter, out GameObject player, out Camera cam)
        {
            player = new GameObject("PlayerController", typeof(CharacterController));
            player.tag = "Player";
            player.transform.SetParent(playerGroup, false);
            // Spawn near Room 1's south entrance (back to Start), facing the room center (+Z).
            player.transform.position = roomCenter + new Vector3(0f, 0f, -2f);
            player.transform.rotation = Quaternion.identity;

            CharacterController cc = player.GetComponent<CharacterController>();
            cc.height = 1.8f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.radius = 0.35f;

            GameObject camGO = new GameObject("Camera", typeof(Camera), typeof(AudioListener));
            camGO.transform.SetParent(player.transform, false);
            camGO.transform.localPosition = new Vector3(0, 1.6f, 0);
            cam = camGO.GetComponent<Camera>();
            cam.nearClipPlane = 0.05f;

            FirstPersonController fpc = player.AddComponent<FirstPersonController>();
            fpc.eyeHeight = 1.6f;

            PlayerInteractor interactor = player.AddComponent<PlayerInteractor>();
            interactor.playerCamera = cam;
        }

        static void DisableFlyCamera(Transform playerGroup)
        {
            Transform fly = playerGroup.Find("FlyCamera");
            if (fly != null) fly.gameObject.SetActive(false);
        }

        // ---------------- HUD: crosshair + prompt + clue tracker ----------------

        static void BuildCrosshairAndPrompt(Transform canvasT, GameObject player)
        {
            UIBuilder.AddPanel(canvasT, "Crosshair", new Color(1, 1, 1, 0.85f));
            RectTransform crosshairRect = canvasT.Find("Crosshair").GetComponent<RectTransform>();
            crosshairRect.anchorMin = crosshairRect.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairRect.sizeDelta = new Vector2(4, 4);
            crosshairRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI prompt = UIBuilder.AddText(canvasT, "InteractionPrompt", "", 26,
                TextAlignmentOptions.Center, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(600, 50));

            player.GetComponent<PlayerInteractor>().promptText = prompt;
        }

        static ClueTracker BuildClueTrackerUI(Transform canvasT)
        {
            RectTransform row = UIBuilder.AddRect(canvasT, "ClueTracker");
            row.anchorMin = row.anchorMax = new Vector2(0f, 0f);
            row.pivot = Vector2.zero;
            row.anchoredPosition = new Vector2(40, 30);
            row.sizeDelta = new Vector2(400, 60);

            ClueTracker tracker = row.gameObject.AddComponent<ClueTracker>();
            tracker.slots = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                TextMeshProUGUI slot = UIBuilder.AddText(row, $"Slot_{i}", CodeManager.SymbolGlyph((ClueSymbol)i), 30,
                    TextAlignmentOptions.Center, new Color(0f, 0.85f, 0.9f),
                    Vector2.zero, Vector2.zero, new Vector2(35 + i * 80, 25), new Vector2(70, 50));
                tracker.slots[i] = slot;
            }
            return tracker;
        }

        // ---------------- Clue 1: Circle — the moved cabinet ----------------

        static void BuildCabinetClue(Transform content, Vector3 roomCenter)
        {
            Transform cabinet = content.Find("Desk");
            if (cabinet == null) { Debug.LogWarning("[WhiteBox] Cabinet (Desk) not found in Room 1."); return; }

            if (cabinet.GetComponent<SlideMover>() == null) cabinet.gameObject.AddComponent<SlideMover>();
            MovableCabinetClue clue = cabinet.GetComponent<MovableCabinetClue>();
            if (clue == null) clue = cabinet.gameObject.AddComponent<MovableCabinetClue>();
            clue.restingLocalPosition = new Vector3(0f, 0f, -0.8f); // matches the CLEAN PATCH decal

            // Wall text hidden behind the cabinet's current corner spot; only switched on once
            // the cabinet slides away, so it can't be read early. Mounted on the north wall
            // right behind the cabinet's corner position.
            GameObject revealGO = CreateLabel("● 0", content, new Vector3(2f, 1.4f, RoomHalf - 0.001f), content, roomCenter, doubleSided: false);
            revealGO.name = "Gameplay_CabinetReveal";
            revealGO.AddComponent<Billboard>();
            ClueMarker marker = revealGO.AddComponent<ClueMarker>();
            marker.symbol = ClueSymbol.Circle;
            marker.label = revealGO.GetComponent<TMP_Text>();
            revealGO.SetActive(false);

            clue.revealTarget = revealGO;
        }

        // ---------------- Clue 2: Triangle — drawer + paper ----------------

        static void BuildDrawerAndPaperClue(Transform content, Dictionary<string, GameObject> prefabs, Vector3 roomCenter, Transform canvasT)
        {
            GameObject counter = (GameObject)PrefabUtility.InstantiatePrefab(prefabs["Counter_Drawer"], content);
            counter.name = "Gameplay_Drawer";
            counter.transform.localPosition = new Vector3(2f, 0f, -1.8f);
            counter.transform.localRotation = Quaternion.identity;

            Transform front = counter.transform.Find("Mesh_DrawerFront");
            front.gameObject.AddComponent<SlideMover>();
            DrawerClue drawer = front.gameObject.AddComponent<DrawerClue>();
            drawer.openLocalPosition = front.localPosition + new Vector3(0, 0, 0.3f);

            GameObject paper = (GameObject)PrefabUtility.InstantiatePrefab(prefabs["Paper"], front);
            paper.name = "Paper";
            paper.transform.localPosition = new Vector3(0, 0.05f, 0.05f);
            paper.transform.localRotation = Quaternion.Euler(0, 10f, 0);
            paper.SetActive(false);
            PaperCloseupClue paperClue = paper.AddComponent<PaperCloseupClue>();
            paperClue.symbol = ClueSymbol.Triangle;
            drawer.paperObject = paper;

            BuildPaperCloseupUI(canvasT);
        }

        static void BuildPaperCloseupUI(Transform canvasT)
        {
            Image_ClearIfExists(canvasT, "PaperCloseup");
            RectTransform panel = UIBuilder.FullRect(canvasT, "PaperCloseup");
            UIBuilder.AddPanel(panel, "Backdrop", new Color(0, 0, 0, 0.92f));

            TextMeshProUGUI symbolDigit = UIBuilder.AddText(panel, "SymbolDigit", "▲  0", 64,
                TextAlignmentOptions.Center, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(600, 100));

            string[] placeholderLines = { "One symbol. One digit.", "Match the symbols on the exit keypad.", "E / Esc to close" };
            for (int i = 0; i < placeholderLines.Length; i++)
                UIBuilder.AddText(panel, $"Note_{i}", placeholderLines[i], 26, TextAlignmentOptions.Center,
                    new Color(0.6f, 0.6f, 0.6f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0, -20 - i * 40), new Vector2(700, 40));

            PaperCloseupUI ui = panel.gameObject.AddComponent<PaperCloseupUI>();
            ui.panelRoot = panel.gameObject;
            ui.symbolDigitText = symbolDigit;
        }

        static void Image_ClearIfExists(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        // ---------------- Clue 3: Square — behind the pillar ----------------

        static void BuildPillarClue(Transform content, Vector3 roomCenter)
        {
            Transform pillar = content.Find("Pillar");
            if (pillar == null) { Debug.LogWarning("[WhiteBox] Pillar not found in Room 1."); return; }

            // Mounted on the pillar's north face (world +Z), on the far side from both the
            // south entrance and the open room centre. CreateLabel's usual "face away from
            // room centre" heuristic would make this readable from the easy/near side, which is
            // exactly backwards here, so the facing is set explicitly instead: readable only to
            // a viewer standing north of the pillar looking back south at it.
            GameObject label = CreateLabel("■ 0", content, pillar.localPosition + new Vector3(0, 0.4f, 0.26f), content, roomCenter, doubleSided: false);
            label.name = "Gameplay_PillarClue";
            label.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            label.AddComponent<Billboard>();

            ClueMarker marker = label.AddComponent<ClueMarker>();
            marker.symbol = ClueSymbol.Square;
            marker.label = label.GetComponent<TMP_Text>();
        }

        // ---------------- Clue 4: Bar — switch + handprints ----------------

        static LightSwitchClue BuildLightSwitchClue(Transform content, Dictionary<string, GameObject> prefabs, Transform ceiling, Vector3 roomCenter)
        {
            GameObject switchGO = new GameObject("Gameplay_Switch");
            switchGO.transform.SetParent(content, false);
            switchGO.transform.localPosition = new Vector3(-RoomHalf + 0.05f, 1.2f, -1f);
            switchGO.transform.localRotation = Quaternion.Euler(0, 90, 0);

            // Reads as an actual light switch: a gray backing plate with a small orange toggle
            // flicked to one side, rather than a single flat colored slab.
            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "Plate";
            plate.transform.SetParent(switchGO.transform, false);
            plate.transform.localPosition = new Vector3(0, 0, 0.015f);
            plate.transform.localScale = new Vector3(0.18f, 0.28f, 0.03f);
            plate.GetComponent<Renderer>().sharedMaterial = MatStructure;

            GameObject toggle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            toggle.name = "Mesh";
            toggle.transform.SetParent(switchGO.transform, false);
            toggle.transform.localPosition = new Vector3(0, 0.04f, 0.05f);
            toggle.transform.localRotation = Quaternion.Euler(20f, 0, 0);
            toggle.transform.localScale = new Vector3(0.06f, 0.12f, 0.04f);
            toggle.GetComponent<Renderer>().sharedMaterial = MatInteractive;

            for (int i = 0; i < 3; i++)
            {
                GameObject hp = (GameObject)PrefabUtility.InstantiatePrefab(prefabs["Trace_Decal"], content);
                hp.name = "Handprint_" + i;
                hp.transform.localPosition = new Vector3(-RoomHalf + 0.02f, 1f + i * 0.15f, -1f + (i - 1) * 0.3f);
                hp.transform.localRotation = Quaternion.Euler(0, -90, 90);
                hp.transform.localScale = new Vector3(0.18f, 0.18f, 1f);
            }

            LightSwitchClue clue = switchGO.AddComponent<LightSwitchClue>();
            clue.symbol = ClueSymbol.Bar;

            GameObject label = CreateLabel("| 0", switchGO.transform, new Vector3(0.5f, 0, 0), switchGO.transform, roomCenter, doubleSided: false);
            label.AddComponent<Billboard>();
            clue.label = label.GetComponent<TMP_Text>();

            // Nearest ceiling light to the switch.
            Light nearest = null; float best = float.MaxValue;
            Renderer nearestPanel = null;
            foreach (Transform tile in ceiling)
            {
                Light l = tile.GetComponentInChildren<Light>();
                if (l == null) continue;
                float d = Vector3.Distance(tile.position, switchGO.transform.position);
                if (d < best) { best = d; nearest = l; nearestPanel = tile.Find("Mesh_Panel")?.GetComponent<Renderer>(); }
            }
            clue.targetLight = nearest;
            clue.targetPanelRenderer = nearestPanel;

            return clue;
        }

        // ---------------- Door + keypad + win ----------------

        static void BuildDoorKeypadAndWin(Transform walls, Transform content, Dictionary<string, GameObject> prefabs, Transform canvasT, Vector3 roomCenter)
        {
            Transform doorFrame = walls.Find("Wall_East_Center");
            if (doorFrame == null) { Debug.LogWarning("[WhiteBox] East door not found; skipping door/keypad/win setup."); return; }

            Vector3 doorPos = doorFrame.position;
            Quaternion doorRot = doorFrame.rotation;
            Vector3 outward = doorRot * Vector3.forward; // world +X for the east wall

            // Sealed slab filling the doorway; slides straight up to open. A short handle bar
            // makes it read as a door rather than a plain colored block. Slab and handle are
            // siblings under an unscaled root (SlideMover moves the whole root) rather than
            // parent/child, so the slab's own non-uniform scale can't warp the handle.
            GameObject seal = new GameObject("Gameplay_Door");
            seal.transform.SetParent(content, false);
            seal.transform.position = doorPos + outward * 0.02f + Vector3.up * (DoorGapHeight / 2f);
            seal.transform.rotation = doorRot;
            SlideMover doorMover = seal.AddComponent<SlideMover>();

            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "Slab";
            slab.transform.SetParent(seal.transform, false);
            slab.transform.localScale = new Vector3(1f, DoorGapHeight, 0.15f);
            slab.GetComponent<Renderer>().sharedMaterial = MatInteractive;

            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "Handle";
            handle.transform.SetParent(seal.transform, false);
            handle.transform.localPosition = new Vector3(0.3f, 0f, -0.12f);
            handle.transform.localScale = new Vector3(0.08f, 0.5f, 0.06f);
            handle.GetComponent<Renderer>().sharedMaterial = MatStructure;

            // Keypad panel: a backing plate with a 3x3 grid of raised buttons, so it reads as a
            // keypad by silhouette alone rather than needing a label to explain a plain slab.
            // The backing and buttons are siblings under an unscaled root (not parent/child of
            // each other) so the backing's own non-uniform scale can't warp the buttons' layout.
            Transform keypadWall = walls.Find("Wall_East_Right");
            Vector3 keypadPos = keypadWall != null ? keypadWall.position : doorPos;

            GameObject keypadRoot = new GameObject("Gameplay_Keypad");
            keypadRoot.transform.SetParent(content, false);
            keypadRoot.transform.position = keypadPos - outward * 0.06f + Vector3.up * 1.25f;
            keypadRoot.transform.rotation = doorRot * Quaternion.Euler(0, 180, 0);
            keypadRoot.AddComponent<KeypadController>();

            GameObject backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backing.name = "Backing";
            backing.transform.SetParent(keypadRoot.transform, false);
            backing.transform.localScale = new Vector3(0.55f, 0.45f, 0.03f);
            backing.GetComponent<Renderer>().sharedMaterial = MatStructure;

            for (int row = 0; row < 3; row++)
            {
                for (int c = 0; c < 3; c++)
                {
                    GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    btn.name = $"Button_{row}_{c}";
                    btn.transform.SetParent(keypadRoot.transform, false);
                    btn.transform.localPosition = new Vector3((c - 1) * 0.14f, (1 - row) * 0.14f, 0.03f);
                    btn.transform.localScale = new Vector3(0.1f, 0.1f, 0.04f);
                    btn.GetComponent<Renderer>().sharedMaterial = MatInteractive;
                }
            }

            // Win trigger just past the (now open) doorway, into the existing corridor to Room 2.
            GameObject win = new GameObject("Gameplay_WinTrigger", typeof(BoxCollider));
            win.transform.SetParent(content, false);
            win.transform.position = doorPos + outward * 1.2f + Vector3.up * (DoorGapHeight / 2f);
            win.transform.rotation = doorRot;
            BoxCollider col = win.GetComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(1.2f, DoorGapHeight, 1.6f);
            win.AddComponent<WinTrigger>();

            BuildKeypadUI(canvasT, doorMover, seal.transform.localPosition + Vector3.up * 2.2f);
        }

        static void BuildKeypadUI(Transform canvasT, SlideMover door, Vector3 doorOpenLocalPos)
        {
            RectTransform panel = UIBuilder.AddRect(canvasT, "KeypadUI");
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(420, 520);
            panel.anchoredPosition = Vector2.zero;
            UIBuilder.AddPanel(panel, "Backdrop", new Color(0.05f, 0.05f, 0.05f, 0.95f));

            KeypadUI ui = panel.gameObject.AddComponent<KeypadUI>();
            ui.panelRoot = panel.gameObject;
            ui.door = door;
            ui.doorOpenLocalPosition = doorOpenLocalPos;
            ui.digitSlots = new TMP_Text[4];
            ui.symbolLabels = new TMP_Text[4];

            float slotWidth = 80f;
            float startX = -((4 - 1) * slotWidth) / 2f;
            for (int i = 0; i < 4; i++)
            {
                float x = startX + i * slotWidth;
                ui.symbolLabels[i] = UIBuilder.AddText(panel, $"Symbol_{i}", "?", 30, TextAlignmentOptions.Center,
                    new Color(0f, 0.85f, 0.9f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -50), new Vector2(70, 50));
                ui.digitSlots[i] = UIBuilder.AddText(panel, $"Digit_{i}", "_", 34, TextAlignmentOptions.Center,
                    Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -100), new Vector2(70, 50));
            }

            // 0-9 grid (3 columns x 4 rows, last row centered under 0).
            for (int d = 0; d <= 9; d++)
            {
                int row = d == 0 ? 3 : (d - 1) / 3;
                int col = d == 0 ? 1 : (d - 1) % 3;
                float bx = -110 + col * 110;
                float by = -160 - row * 70;
                var (go, button, _) = UIBuilder.AddButton(panel, $"Digit{d}", d.ToString(),
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(bx, by), new Vector2(90, 55),
                    new Color(1f, 0.45f, 0f), Color.white);
                KeypadDigitButton digitButton = go.AddComponent<KeypadDigitButton>();
                digitButton.digit = d;
                UIBuilder.BindClick(button, digitButton, nameof(KeypadDigitButton.Press));
            }

            var (clearGO, clearBtn, _) = UIBuilder.AddButton(panel, "Clear", "Clear",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-110, -450), new Vector2(90, 45),
                new Color(0.3f, 0.3f, 0.3f), Color.white, 20f);
            UIBuilder.BindClick(clearBtn, ui, nameof(KeypadUI.PressClear));

            var (confirmGO, confirmBtn, _) = UIBuilder.AddButton(panel, "Confirm", "Confirm",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -450), new Vector2(90, 45),
                new Color(0.2f, 0.6f, 0.2f), Color.white, 20f);
            UIBuilder.BindClick(confirmBtn, ui, nameof(KeypadUI.PressConfirm));
            var (_, closeBtn, _) = UIBuilder.AddButton(panel, "Close", "Close",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(110, -450), new Vector2(90, 45),
                new Color(0.3f, 0.3f, 0.3f), Color.white, 20f);
            UIBuilder.BindClick(closeBtn, ui, nameof(KeypadUI.Close));
            // Left active here deliberately: KeypadUI.Awake() needs to run at scene load (to set
            // Instance) before it disables its own panel, matching PaperCloseupUI's pattern. If
            // this object were saved inactive, Awake would never fire and Instance would stay null.
        }

        // ---------------- Lights-out fail timer ----------------

        static void BuildLightsOutTimer(Transform managers, Transform ceiling, LightSwitchClue switchClue)
        {
            List<Light> lights = new List<Light>();
            List<Renderer> panels = new List<Renderer>();
            foreach (Transform tile in ceiling)
            {
                Light l = tile.GetComponentInChildren<Light>();
                if (l == null) continue;
                lights.Add(l);
                panels.Add(tile.Find("Mesh_Panel")?.GetComponent<Renderer>());
            }

            LightsOutTimer timer = managers.gameObject.AddComponent<LightsOutTimer>();
            timer.roomLights = lights.ToArray();
            timer.roomPanels = panels.ToArray();
            timer.linkedLight = switchClue != null ? switchClue.targetLight : null;
            timer.linkedSwitchClue = switchClue;
        }
    }
}
#endif
