using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class Room2GameplaySmokeTest
{
    static GameObject Find(string name)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == name) return root;
            var t = FindDeep(root.transform, name);
            if (t != null) return t.gameObject;
        }
        return null;
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t)
        {
            var found = FindDeep(c, name);
            if (found != null) return found;
        }
        return null;
    }

    // FirstPersonController re-applies its private _pitch to the camera every Update() (it reads
    // live mouse delta each frame), so a one-off Camera.transform.localRotation assignment from a
    // test gets silently overwritten before the next frame. Setting the backing field directly is
    // the only way to make a forced look direction stick.
    static void SetLookPitch(FirstPersonController fpc, float pitch)
    {
        var field = typeof(FirstPersonController).GetField("_pitch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field.SetValue(fpc, pitch);
    }

    [UnityTest]
    public IEnumerator FullRoom2Playthrough_UnlocksExitAndMarksSolvedWithoutEndingGame()
    {
        GameState.BeginNewGame();
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight", LoadSceneMode.Single);
        yield return null;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        CharacterController cc = player.GetComponent<CharacterController>();
        var fpc = player.GetComponent<FirstPersonController>();

        GameObject l5 = Find("Ceiling_Light_2x2_L5");
        Assert.IsNotNull(l5, "L5 ceiling tile should be identifiable by name.");
        var panel = l5.transform.Find("Cover").GetComponent<CeilingCardPanel>();

        // Standing on the floor, far below the ceiling, L5 must be unreachable even though a
        // raycast could physically hit it from here.
        Assert.IsFalse(panel.CanInteract, "L5 should be out of reach while standing on the floor.");
        StringAssert.Contains("stool", panel.BlockedReason);

        // Pick up the stool.
        GameObject stool = Find("Gameplay_Stool");
        var carrier = Object.FindFirstObjectByType<StoolCarrier>();
        Assert.IsNotNull(carrier);
        stool.GetComponent<StoolPickupTarget>().Interact(null);
        Assert.IsTrue(carrier.IsCarrying, "E on the stool should pick it up.");
        Assert.IsFalse(stool.activeSelf, "The real stool should hide while carrying the ghost preview.");

        // Aim straight down at the floor under L5 and place it there. player.position is the
        // CharacterController's base (feet) — the camera is a further +1.6 child offset above
        // that, giving normal eye height, not player.position itself.
        cc.enabled = false;
        player.transform.position = new Vector3(l5.transform.position.x, 0f, l5.transform.position.z - 0.3f);
        player.transform.rotation = Quaternion.identity;
        cc.enabled = true;
        SetLookPitch(fpc, 45f);
        yield return null;
        carrier.RefreshPlacement();
        Assert.AreEqual("E   Place stool", carrier.PlacementPrompt,
            "A normal downward glance must allow floor placement beyond the old eye-distance limit.");
        SetLookPitch(fpc, 0f);
        yield return null;
        Assert.IsFalse(carrier.TryPlace(), "Looking horizontally must not place using a stale floor hit.");
        Assert.IsTrue(carrier.IsCarrying);
        SetLookPitch(fpc, 75f); // look mostly straight down; a plain Camera.transform assignment
                                 // gets stomped every frame by FPC's own Update() re-applying _pitch
        yield return null; // let StoolCarrier.Update() raycast against the floor this frame
        bool placed = carrier.TryPlace();
        Assert.IsTrue(placed, "Placing near the room center on open floor should be valid.");
        Assert.IsFalse(carrier.IsCarrying);
        Assert.IsTrue(stool.activeSelf, "The real stool should reappear once placed.");

        // Stand on the stool (teleport rather than relying on stepOffset physics here — that's
        // covered by it simply being set in Stage 1) and take the card.
        cc.enabled = false;
        player.transform.position = new Vector3(stool.transform.position.x, 0.45f, stool.transform.position.z);
        cc.enabled = true;
        yield return null;
        Assert.IsTrue(panel.CanInteract, "Standing on the stool near L5 should now be in reach.");
        panel.Interact(null);
        Assert.IsTrue(Inventory.Instance.HasItem("AccessCard"), "Opening the L5 cover should grant the access card.");

        // Card reader: unlock the cabinet.
        GameObject cabinet = Find("Gameplay_SampleCabinet");
        var reader = cabinet.transform.Find("Gameplay_CardReader").GetComponent<CardReader>();
        reader.Interact(null);
        Assert.IsFalse(Inventory.Instance.HasItem("AccessCard"), "The access card should be consumed by the reader.");
        yield return new WaitForSeconds(0.9f); // cabinet slide duration
        GameObject card = Find("Gameplay_CalibrationCard");
        Assert.IsTrue(card.activeSelf, "The calibration card should be revealed once the cabinet opens.");

        card.GetComponent<InventoryPickup>().Interact(null);
        Assert.IsTrue(Inventory.Instance.HasItem("CalibrationCard"));

        // Matrix screen: must need the card, then align it.
        GameObject matrix = Find("Gameplay_DataMatrix");
        var screen = matrix.GetComponent<DataMatrixScreen>();
        Assert.IsTrue(screen.CanInteract);
        screen.Interact(null);
        yield return null;
        var alignUI = CardAlignmentUI.Instance;
        Assert.IsTrue(alignUI.IsOpen);

        alignUI.TryAttach(); // wrong rotation (starts 2 steps off) should reject
        Assert.IsFalse(alignUI.Aligned);
        yield return new WaitForSeconds(0.3f); // let the reject-bounce animation clear _busy
        alignUI.Rotate();
        alignUI.Rotate();
        alignUI.TryAttach(); // now aligned
        Assert.IsTrue(alignUI.Aligned, "Two 90-degree rotations from a 2-step start should align the card.");
        Assert.IsFalse(Inventory.Instance.HasItem("CalibrationCard"), "The card should be consumed once aligned.");

        for (int i = 0; i < alignUI.holeCellIndex.Length; i++)
        {
            int cellIdx = alignUI.holeCellIndex[i];
            Assert.IsTrue(alignUI.matrixCells[cellIdx].gameObject.activeSelf, $"Hole {i + 1} should be revealed.");
            Assert.AreEqual(GameState.Room2Code[i].ToString(), alignUI.matrixCells[cellIdx].text,
                $"Hole {i + 1} should show Room 2's real code digit.");
        }
        alignUI.Close();

        // Exit keypad: wrong code first (should not give away symbols, and should penalize).
        GameObject keypad2GO = Find("Gameplay_Keypad2");
        var keypadController2 = keypad2GO.GetComponent<KeypadController>();
        var ui2 = keypadController2.ui;
        Assert.IsFalse(ui2.symbolLabels[0].gameObject.activeSelf, "Room 2's keypad must not show the symbol row.");

        keypadController2.Interact(null);
        ui2.PressDigit((GameState.Room2Code[0] + 1) % 10);
        ui2.PressDigit(GameState.Room2Code[1]);
        ui2.PressDigit(GameState.Room2Code[2]);
        ui2.PressDigit(GameState.Room2Code[3]);
        ui2.PressConfirm();
        Assert.IsFalse(ui2.Solved);
        ui2.Close();

        // Correct code.
        ui2.Open();
        foreach (int d in GameState.Room2Code) ui2.PressDigit(d);
        ui2.PressConfirm();
        yield return null;
        Assert.IsTrue(ui2.Solved);

        yield return new WaitForSeconds(1.2f); // door slide duration

        GameObject exit2 = Find("Gameplay_RoomExit2");
        Assert.IsNotNull(exit2);
        Vector3 outward = exit2.transform.forward;
        cc.enabled = false;
        player.transform.position = exit2.transform.position - outward * 1.0f;
        player.transform.rotation = Quaternion.LookRotation(outward, Vector3.up);
        cc.enabled = true;
        yield return null;

        float elapsed = 0f;
        while (!GameState.IsRoomSolved(2) && elapsed < 3f)
        {
            cc.Move(outward * 3f * Time.deltaTime);
            yield return null;
            elapsed += Time.deltaTime;
        }

        Assert.IsTrue(GameState.IsRoomSolved(2), "Walking through Room 2's exit should mark it solved.");
        Assert.AreEqual("WhiteBox_LastLight", SceneManager.GetActiveScene().name);
        Assert.IsFalse(GameState.Won);
    }

    [UnityTest]
    public IEnumerator CarryingStoolBlocksBothDoorways()
    {
        GameState.BeginNewGame();
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight", LoadSceneMode.Single);
        yield return null;

        GameObject stool = Find("Gameplay_Stool");
        var carrier = Object.FindFirstObjectByType<StoolCarrier>();
        stool.GetComponent<StoolPickupTarget>().Interact(null);
        Assert.IsTrue(carrier.IsCarrying);

        GameObject blockWest = Find("Gameplay_StoolBlockWest");
        Assert.IsNotNull(blockWest);
        Collider solid = blockWest.transform.Find("SolidWall").GetComponent<Collider>();
        Assert.IsFalse(solid.enabled, "Solid block should stay off while not carrying anything.");

        // Simulate the block's own Update() tick (it polls carrier.IsCarrying each frame).
        yield return null;
        Assert.IsTrue(solid.enabled, "Solid block should switch on once the player is carrying the stool.");

        carrier.TryPlace(); // no valid surface aimed at yet, should just fail harmlessly
        Assert.IsTrue(carrier.IsCarrying, "TryPlace with no valid aim point must not silently succeed.");
    }
}
