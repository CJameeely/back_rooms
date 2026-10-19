#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using static WhiteBoxKit.WhiteBoxModules;

namespace WhiteBoxKit
{
    /// Step 3b: layers Room 2's (R2_LockedCounter, "sample storage") gameplay on top of its
    /// Populate_LockedCounter set dressing — same division of labor GameplayBuilder uses for Room
    /// 1 (level builder lays down named furniture, this step finds it and wires up the puzzle).
    /// Must run after Step 3 (needs the shared Inventory/ObjectiveToast singletons Room 1's step
    /// builds). Clears its own additions first, so it's safe to re-run on its own.
    public static class Room2GameplayBuilder
    {
        const string ScenePath = "Assets/Scenes/WhiteBox_LastLight.unity";
        const float DoorGapHeight = 2.1f;
        const float CeilingHeight = 3.2f;
        const float RoomTimerDuration = 210f;

        [MenuItem("Tools/WhiteBox/Step 3b - Build Room2 Gameplay")]
        public static void Step3b_BuildRoom2Gameplay()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            LoadMaterials();

            GameObject roomsGroup = GameObject.Find("Rooms");
            if (roomsGroup == null) { Debug.LogError("[WhiteBox] Rooms group not found — run Step 2 first."); return; }
            Transform room2 = FindRoomContaining(roomsGroup.transform, "R2_LockedCounter");
            if (room2 == null) { Debug.LogError("[WhiteBox] Could not find the R2_LockedCounter room instance."); return; }
            if (GameObject.Find("GameplayCanvas") == null)
            {
                Debug.LogError("[WhiteBox] GameplayCanvas not found — run Step 3 (Room 1) first.");
                return;
            }

            Transform content = room2.Find("Content");
            Transform walls = room2.Find("Walls");
            Transform ceiling = room2.Find("Ceiling");
            Vector3 roomCenter = room2.position;

            ClearPrevious(content);
            RaiseRoomCeiling(room2, CeilingHeight);

            Canvas canvas = UIBuilder.CreateScreenCanvas("Room2Canvas");

            GameObject managers = new GameObject("Room2GameplayManagers");
            Room2CodeValidator validator = managers.AddComponent<Room2CodeValidator>();

            RoomTimer timer = managers.AddComponent<RoomTimer>();
            timer.roomNumber = 2;
            timer.totalDuration = RoomTimerDuration;
            var lights = new List<Light>();
            var panels = new List<Renderer>();
            foreach (Transform tile in ceiling)
            {
                Light l = tile.GetComponentInChildren<Light>();
                if (l == null) continue;
                lights.Add(l);
                panels.Add(tile.Find("Mesh_Panel")?.GetComponent<Renderer>());
            }
            timer.roomLights = lights.ToArray();
            timer.roomPanels = panels.ToArray();

            BuildStoolAndCeilingCard(managers, content, ceiling, roomCenter);
            BuildCabinetAndCard(content);
            BuildMatrixScreen(content, canvas.transform);
            BuildExitDoorAndKeypad(walls, content, canvas.transform, validator, timer);
            BuildEntryAndBlockers(content, walls, timer);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[WhiteBox] Step 3b complete: Room 2 is now playable.");
        }

        static Transform FindRoomContaining(Transform roomsGroup, string contains)
        {
            foreach (Transform child in roomsGroup)
                if (child.name.Contains(contains))
                    return child;
            return null;
        }

        static void ClearPrevious(Transform content)
        {
            foreach (string n in new[] { "Room2GameplayManagers", "Room2Canvas" })
            {
                GameObject go = GameObject.Find(n);
                if (go != null) Object.DestroyImmediate(go);
            }
            // These are all additions this step itself makes fresh each run (not Populate_
            // LockedCounter's base furniture), so wholesale destroy-and-recreate is safe.
            foreach (string n in new[] { "Gameplay_StoolGhost", "Gameplay_CardReader",
                "Gameplay_CalibrationCard", "Gameplay_ExitDoor", "Gameplay_Keypad2", "Gameplay_RoomExit2",
                "Gameplay_Entry2West", "Gameplay_StoolBlockWest", "Gameplay_StoolBlockNorth" })
            {
                Transform t = content.Find(n);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }
            // The stool/cabinet/matrix placeholders themselves persist across reruns
            // (Populate_LockedCounter only makes them fresh on a full Step 2), but any
            // components this step bolted onto them need to be cleared before re-adding.
            Transform stoolClean = content.Find("Gameplay_Stool");
            if (stoolClean != null)
            {
                var pt = stoolClean.GetComponent<StoolPickupTarget>();
                if (pt != null) Object.DestroyImmediate(pt);
            }
            Transform cabinet = content.Find("Gameplay_SampleCabinet");
            if (cabinet != null)
            {
                var front = cabinet.Find("Mesh_DrawerFront");
                if (front != null)
                {
                    var sm = front.GetComponent<SlideMover>();
                    if (sm != null) Object.DestroyImmediate(sm);
                }
            }
            Transform matrixObj = content.Find("Gameplay_DataMatrix");
            if (matrixObj != null)
            {
                var screen = matrixObj.GetComponent<DataMatrixScreen>();
                if (screen != null) Object.DestroyImmediate(screen);
                var matrixCol = matrixObj.GetComponent<BoxCollider>();
                if (matrixCol != null) Object.DestroyImmediate(matrixCol);
            }
            // The stool's own primitive gets rebuilt wholesale above via the Populate step's name
            // list, but the ceiling's L5 cover/collider additions need explicit cleanup since the
            // ceiling tiles themselves aren't recreated unless Step 2 reruns.
            GameObject l5Cover = GameObject.Find("Cover");
            if (l5Cover != null && l5Cover.transform.parent != null && l5Cover.transform.parent.name.StartsWith("Ceiling_Light_2x2"))
                Object.DestroyImmediate(l5Cover);
        }

        // ---------------- Geometry: raise this room's ceiling ----------------

        static void RaiseRoomCeiling(Transform room, float newHeight)
        {
            Transform ceiling = room.Find("Ceiling");
            foreach (Transform tile in ceiling)
            {
                Vector3 pos = tile.localPosition;
                pos.y = newHeight;
                tile.localPosition = pos;
            }
            Transform walls = room.Find("Walls");
            foreach (Transform wall in walls)
                RaiseWallSegment(wall, newHeight);
        }

        static void RaiseWallSegment(Transform wallInstance, float newHeight)
        {
            foreach (Transform child in wallInstance)
            {
                if (child.name == "Mesh" || child.name == "Mesh_JambL" || child.name == "Mesh_JambR")
                {
                    Vector3 scale = child.localScale; scale.y = newHeight; child.localScale = scale;
                    Vector3 pos = child.localPosition; pos.y = newHeight / 2f; child.localPosition = pos;
                }
                else if (child.name == "Mesh_Header")
                {
                    float headerHeight = newHeight - DoorGapHeight;
                    Vector3 scale = child.localScale; scale.y = headerHeight; child.localScale = scale;
                    Vector3 pos = child.localPosition; pos.y = DoorGapHeight + headerHeight / 2f; child.localPosition = pos;
                }
            }
        }

        static Transform FindCenterCeilingTile(Transform ceiling)
        {
            Transform best = null; float bestDist = float.MaxValue;
            foreach (Transform tile in ceiling)
            {
                float d = new Vector2(tile.localPosition.x, tile.localPosition.z).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = tile; }
            }
            return best;
        }

        // ---------------- Mechanic A: stool + L5 ceiling card ----------------

        static void BuildStoolAndCeilingCard(GameObject managers, Transform content, Transform ceiling, Vector3 roomCenter)
        {
            Transform stoolT = content.Find("Gameplay_Stool");
            StoolPickupTarget pickup = stoolT.gameObject.AddComponent<StoolPickupTarget>();

            StoolCarrier carrier = managers.AddComponent<StoolCarrier>();
            carrier.stoolVisual = stoolT.gameObject;
            pickup.carrier = carrier;

            GameObject ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghost.name = "Gameplay_StoolGhost";
            Object.DestroyImmediate(ghost.GetComponent<Collider>());
            ghost.transform.SetParent(content, false);
            ghost.transform.localScale = stoolT.localScale;
            Renderer ghostRend = ghost.GetComponent<Renderer>();
            ghostRend.sharedMaterial = new Material(ghostRend.sharedMaterial);
            ghost.SetActive(false);
            carrier.ghostPreview = ghost.transform;
            carrier.ghostRenderer = ghostRend;

            // Room floor footprint in world space — a touch inset from the walls so "inside the
            // room" doesn't allow placing flush against a wall.
            carrier.roomFloorBounds = new Bounds(roomCenter + new Vector3(0, 0.1f, 0),
                new Vector3(RoomHalf * 2f - 0.5f, 0.4f, RoomHalf * 2f - 0.5f));

            Transform l5 = FindCenterCeilingTile(ceiling);
            l5.name = "Ceiling_Light_2x2_L5";

            GameObject cover = new GameObject("Cover");
            cover.transform.SetParent(l5, false);
            cover.transform.localPosition = new Vector3(0, -0.02f, 0);

            Material darkShadow = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.12f, 0.12f, 0.14f) };
            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shadow.name = "Shadow";
            shadow.transform.SetParent(cover.transform, false);
            Object.DestroyImmediate(shadow.GetComponent<Collider>());
            shadow.transform.localScale = new Vector3(1.2f, 0.015f, 0.8f);
            shadow.GetComponent<Renderer>().sharedMaterial = darkShadow;

            GameObject chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chip.name = "Chip";
            chip.transform.SetParent(cover.transform, false);
            Object.DestroyImmediate(chip.GetComponent<Collider>());
            chip.transform.localPosition = new Vector3(0.45f, 0.015f, 0.3f);
            chip.transform.localScale = new Vector3(0.12f, 0.03f, 0.12f);
            chip.GetComponent<Renderer>().sharedMaterial = MatInteractive;

            BoxCollider col = cover.AddComponent<BoxCollider>();
            col.size = new Vector3(1.2f, 0.1f, 0.8f);

            CeilingCardPanel panel = cover.AddComponent<CeilingCardPanel>();
            panel.cover = cover.transform;
            panel.cardItem = new InventoryItem("AccessCard", "Access Card", new Color(0.95f, 0.55f, 0.2f));
        }

        // ---------------- Mechanic B: cabinet lock + calibration card ----------------

        static void BuildCabinetAndCard(Transform content)
        {
            Transform cabinet = content.Find("Gameplay_SampleCabinet");
            Transform front = cabinet.Find("Mesh_DrawerFront");
            SlideMover frontMover = front.gameObject.AddComponent<SlideMover>();
            Vector3 openPos = front.localPosition + new Vector3(0, 0, 0.3f);

            GameObject card = GameObject.CreatePrimitive(PrimitiveType.Cube);
            card.name = "Gameplay_CalibrationCard";
            card.transform.SetParent(cabinet, false);
            Object.DestroyImmediate(card.GetComponent<Collider>());
            card.AddComponent<BoxCollider>(); // fresh trigger-free collider for the raycast to hit
            card.transform.localPosition = front.localPosition + new Vector3(0, 0.15f, 0.3f);
            card.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
            card.transform.localScale = new Vector3(0.25f, 0.02f, 0.35f);
            card.GetComponent<Renderer>().sharedMaterial = MatClue;
            card.SetActive(false);
            InventoryPickup cardPickup = card.AddComponent<InventoryPickup>();
            cardPickup.item = new InventoryItem("CalibrationCard", "Calibration Card", new Color(0.25f, 0.6f, 0.85f));
            cardPickup.prompt = "Pick up calibration card";

            GameObject readerRoot = new GameObject("Gameplay_CardReader");
            readerRoot.transform.SetParent(cabinet, false);
            readerRoot.transform.localPosition = new Vector3(0.65f, 0.5f, 0f);
            readerRoot.transform.localRotation = Quaternion.Euler(0, 90f, 0);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(readerRoot.transform, false);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.localScale = new Vector3(0.12f, 0.18f, 0.04f);
            body.GetComponent<Renderer>().sharedMaterial = MatStructure;

            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            indicator.name = "Indicator";
            indicator.transform.SetParent(readerRoot.transform, false);
            Object.DestroyImmediate(indicator.GetComponent<Collider>());
            indicator.transform.localPosition = new Vector3(0, 0.05f, 0.025f);
            indicator.transform.localScale = new Vector3(0.03f, 0.03f, 0.01f);
            Renderer indicatorRend = indicator.GetComponent<Renderer>();
            indicatorRend.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            BoxCollider readerCol = readerRoot.AddComponent<BoxCollider>();
            readerCol.size = new Vector3(0.15f, 0.2f, 0.06f);

            CardReader reader = readerRoot.AddComponent<CardReader>();
            reader.cabinetDoor = frontMover;
            reader.cabinetOpenLocalPosition = openPos;
            reader.indicatorRenderer = indicatorRend;
            reader.revealOnUnlock = card;
        }

        // ---------------- Mechanic B continued: matrix screen + alignment UI ----------------

        static void BuildMatrixScreen(Transform content, Transform canvasT)
        {
            Transform matrixObj = content.Find("Gameplay_DataMatrix");

            RectTransform panel = UIBuilder.AddRect(canvasT, "CardAlignmentUI");
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(560, 640);
            panel.anchoredPosition = Vector2.zero;
            UIBuilder.AddPanel(panel, "Backdrop", new Color(0.05f, 0.05f, 0.05f, 0.95f));

            CardAlignmentUI ui = panel.gameObject.AddComponent<CardAlignmentUI>();
            ui.panelRoot = panel.gameObject;
            ui.matrixCells = new TMP_Text[16];
            // One hole per row, one per column (a fixed layout — only the digits inside are
            // randomized per spec, not which cells are holes).
            ui.holeCellIndex = new[] { 0 * 4 + 1, 1 * 4 + 3, 2 * 4 + 0, 3 * 4 + 2 };
            ui.startRotationSteps = 2;

            const float cell = 70f, gridSize = cell * 4f;
            float startX = -gridSize / 2f + cell / 2f;
            float startY = gridSize / 2f - cell / 2f + 30f;

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    int idx = row * 4 + col;
                    float x = startX + col * cell;
                    float y = startY - row * cell;
                    ui.matrixCells[idx] = UIBuilder.AddText(panel, $"Cell_{idx}", "0", 30, TextAlignmentOptions.Center,
                        Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(cell, cell));
                }
            }

            string[] pinLabels = { "1", "2", "3", "4" };
            for (int i = 0; i < ui.holeCellIndex.Length; i++)
            {
                int idx = ui.holeCellIndex[i];
                int row = idx / 4, col = idx % 4;
                float x = startX + col * cell + cell / 2f - 10f;
                float y = startY - row * cell + cell / 2f - 6f;
                UIBuilder.AddText(panel, $"PinLabel_{i}", pinLabels[i], 15, TextAlignmentOptions.Center,
                    new Color(0.9f, 0.6f, 0.3f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x, y), new Vector2(22, 18));
            }

            RectTransform card = UIBuilder.AddRect(panel, "Card");
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = new Vector2(0, 30f);
            card.sizeDelta = new Vector2(gridSize + 24, gridSize + 24);
            Image cardImg = card.gameObject.AddComponent<Image>();
            cardImg.color = new Color(0.25f, 0.6f, 0.85f, 0.35f);
            ui.cardRect = card;

            UIBuilder.AddText(card, "Notch", "▲", 32, TextAlignmentOptions.Center,
                new Color(1f, 1f, 1f, 0.95f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18, -18), new Vector2(36, 36));

            UIBuilder.AddText(panel, "FrameNotch", "▲", 28, TextAlignmentOptions.Center,
                new Color(0.55f, 0.55f, 0.55f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-gridSize / 2f - 16, 30f + gridSize / 2f + 16), new Vector2(36, 36));

            ui.statusText = UIBuilder.AddText(panel, "Status", "", 22, TextAlignmentOptions.Center,
                new Color(0.9f, 0.5f, 0.3f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 95), new Vector2(500, 40));

            UIBuilder.AddText(panel, "Hint", "R  Rotate      E  Attach      Esc  Close", 18, TextAlignmentOptions.Center,
                new Color(0.6f, 0.6f, 0.6f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 55), new Vector2(500, 30));

            DataMatrixScreen screen = matrixObj.gameObject.AddComponent<DataMatrixScreen>();
            screen.ui = ui;
            if (matrixObj.GetComponent<Collider>() == null) matrixObj.gameObject.AddComponent<BoxCollider>();
        }

        // ---------------- Exit door + keypad (reuses Room 1's KeypadUI builder) ----------------

        static void BuildExitDoorAndKeypad(Transform walls, Transform content, Transform canvasT, Room2CodeValidator validator, RoomTimer timer)
        {
            Transform doorFrame = walls.Find("Wall_North_Center");
            if (doorFrame == null) { Debug.LogWarning("[WhiteBox] Room 2 north door not found; skipping exit/keypad."); return; }

            Vector3 doorPos = doorFrame.position;
            Quaternion doorRot = doorFrame.rotation;
            Vector3 outward = doorRot * Vector3.forward;

            GameObject seal = new GameObject("Gameplay_ExitDoor");
            seal.transform.SetParent(content, false);
            seal.transform.position = doorPos + outward * 0.02f + Vector3.up * (DoorGapHeight / 2f);
            seal.transform.rotation = doorRot;
            SlideMover doorMover = seal.AddComponent<SlideMover>();

            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "Slab";
            slab.transform.SetParent(seal.transform, false);
            slab.transform.localScale = new Vector3(1f, DoorGapHeight, 0.15f);
            slab.GetComponent<Renderer>().sharedMaterial = MatInteractive;

            Transform keypadWall = walls.Find("Wall_North_Right");
            Vector3 keypadPos = keypadWall != null ? keypadWall.position : doorPos;

            GameObject keypadRoot = new GameObject("Gameplay_Keypad2");
            keypadRoot.transform.SetParent(content, false);
            keypadRoot.transform.position = keypadPos - outward * 0.06f + Vector3.up * 1.25f;
            keypadRoot.transform.rotation = doorRot * Quaternion.Euler(0, 180, 0);
            KeypadController keypadController = keypadRoot.AddComponent<KeypadController>();

            AddModelPart(keypadRoot, "Backing", KenneyModel("console"), new Vector3(0.55f, 0.45f, 0.12f), Vector3.zero, MatStructure);

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

            GameObject exit = new GameObject("Gameplay_RoomExit2", typeof(BoxCollider));
            exit.transform.SetParent(content, false);
            exit.transform.position = doorPos + outward * 1.2f + Vector3.up * (DoorGapHeight / 2f);
            exit.transform.rotation = doorRot;
            BoxCollider col = exit.GetComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(1.2f, DoorGapHeight, 1.6f);
            RoomExitTrigger exitTrigger = exit.AddComponent<RoomExitTrigger>();
            exitTrigger.roomNumber = 2;
            exitTrigger.isFinalExit = false;

            KeypadUI ui = GameplayBuilder.BuildKeypadUI(canvasT, doorMover, seal.transform.localPosition + Vector3.up * 2.2f);
            ui.showSymbols = false; // Room 2's keypad doesn't give the symbol hint away
            ui.roomNumberForFastForward = 2;
            ui.validatorBehaviour = validator;
            ui.penaltyTimer = timer;
            ui.wrongGuessPenalty = 5f;
            keypadController.ui = ui;
        }

        // ---------------- Room entry (west doorway, from Room 1) + stool doorway blocks ----------------

        static void BuildEntryAndBlockers(Transform content, Transform walls, RoomTimer timer)
        {
            Transform westDoor = walls.Find("Wall_West_Center");
            Transform northDoor = walls.Find("Wall_North_Center");

            if (westDoor != null)
            {
                GameObject entry = new GameObject("Gameplay_Entry2West", typeof(BoxCollider));
                entry.transform.SetParent(content, false);
                entry.transform.position = westDoor.position + westDoor.rotation * Vector3.forward * 0.5f + Vector3.up * 1.2f;
                entry.transform.rotation = westDoor.rotation;
                BoxCollider col = entry.GetComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(1.4f, 2.4f, 1.2f);
                RoomEntryTrigger rt = entry.AddComponent<RoomEntryTrigger>();
                rt.roomNumber = 2;
                rt.timer = timer;
                rt.objectiveText = "The card you need is somewhere you can't reach yet.";

                BuildDoorwayStoolBlock(content, "Gameplay_StoolBlockWest", westDoor);
            }

            if (northDoor != null)
                BuildDoorwayStoolBlock(content, "Gameplay_StoolBlockNorth", northDoor);
        }

        static void BuildDoorwayStoolBlock(Transform content, string name, Transform doorFrame)
        {
            GameObject blockGO = new GameObject(name);
            blockGO.transform.SetParent(content, false);
            blockGO.transform.position = doorFrame.position;
            blockGO.transform.rotation = doorFrame.rotation;

            BoxCollider sense = blockGO.AddComponent<BoxCollider>();
            sense.isTrigger = true;
            sense.center = new Vector3(0, DoorGapHeight / 2f, 0.4f);
            sense.size = new Vector3(1.4f, DoorGapHeight, 1.4f);

            GameObject solidGO = new GameObject("SolidWall");
            solidGO.transform.SetParent(blockGO.transform, false);
            BoxCollider solid = solidGO.AddComponent<BoxCollider>();
            solid.center = new Vector3(0, DoorGapHeight / 2f, 0.1f);
            solid.size = new Vector3(1.05f, DoorGapHeight, 0.25f);
            solid.enabled = false;

            DoorwayStoolBlock block = blockGO.AddComponent<DoorwayStoolBlock>();
            block.solidBlock = solid;
        }
    }
}
#endif
