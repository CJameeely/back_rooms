using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class Room3GameplaySmokeTest
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
        foreach (Transform c in t) { var f = FindDeep(c, name); if (f != null) return f; }
        return null;
    }
    static void SetLook(FirstPersonController fpc, float yaw, float pitch)
    {
        var yawField = typeof(FirstPersonController).GetField("_yaw", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var pitchField = typeof(FirstPersonController).GetField("_pitch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        yawField.SetValue(fpc, yaw);
        pitchField.SetValue(fpc, pitch);
    }

    [UnityTest]
    public IEnumerator EnteringWithoutBaseThenBacktrackingToF_AssemblesLamp()
    {
        GameState.BeginNewGame();
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight", LoadSceneMode.Single);
        yield return null;

        var cradle = Find("Gameplay_Cradle").GetComponent<InspectionLampCradle>();
        Assert.IsFalse(cradle.CanInteract, "Empty cradle should not be interactable yet.");
        Assert.AreEqual("Empty cradle", cradle.BlockedReason);
        cradle.Interact(null); // per spec: no effect
        Assert.IsFalse(cradle.CanInteract);

        // Grab the head from the workbench (present in Room 3 itself) without ever having
        // visited F for the base.
        GameObject head = Find("Gameplay_LampHead");
        head.GetComponent<InventoryPickup>().Interact(null);
        Assert.IsTrue(Inventory.Instance.HasItem(InspectionLampCradle.HeadItemId));

        cradle.Interact(null); // places the head only — crooked, unlit, not assembled
        Assert.IsFalse(Inventory.Instance.HasItem(InspectionLampCradle.HeadItemId), "Head should be consumed into the cradle.");
        Assert.IsFalse(cradle.CanInteract, "Nothing left to place or take yet.");

        // Now backtrack to F for the base. F has no lock/timer, so this must just work.
        GameObject baseShelfPickup = Find("Gameplay_LampBaseShelf").transform.Find("Pickup").gameObject;
        Assert.IsTrue(baseShelfPickup.activeSelf, "Base should still be waiting on F's shelf.");
        baseShelfPickup.GetComponent<InventoryPickup>().Interact(null);
        Assert.IsTrue(Inventory.Instance.HasItem(InspectionLampCradle.BaseItemId));

        // Back in Room 3: placing the base completes the assembly.
        cradle.Interact(null);
        Assert.IsFalse(Inventory.Instance.HasItem(InspectionLampCradle.BaseItemId));
        Assert.IsTrue(cradle.CanInteract, "Assembled lamp should now be takeable.");
        Assert.AreEqual("Take inspection lamp", cradle.InteractionPrompt);

        cradle.Interact(null); // take it
        Assert.IsTrue(InspectionLamp.Instance.Acquired);
        Assert.IsFalse(cradle.CanInteract, "Cradle should be done once the lamp is taken.");
    }

    [UnityTest]
    public IEnumerator FullRoom3Playthrough_RevealsNameplateAndUnlocksExit()
    {
        GameState.BeginNewGame();
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight", LoadSceneMode.Single);
        yield return null;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        var fpc = player.GetComponent<FirstPersonController>();
        CharacterController cc = player.GetComponent<CharacterController>();

        // Assemble and take the lamp (base from F, head from the workbench).
        Find("Gameplay_LampBaseShelf").transform.Find("Pickup").GetComponent<InventoryPickup>().Interact(null);
        Find("Gameplay_LampHead").GetComponent<InventoryPickup>().Interact(null);
        var cradle = Find("Gameplay_Cradle").GetComponent<InspectionLampCradle>();
        cradle.Interact(null); // base
        cradle.Interact(null); // head -> auto-assembles
        cradle.Interact(null); // take
        Assert.IsTrue(InspectionLamp.Instance.Acquired);

        InspectionLamp.Instance.Toggle();
        Assert.IsTrue(InspectionLamp.Instance.IsOn);

        // Nameplate: should read blank with the lamp off or aimed straight at it, and reveal once
        // grazed from the side.
        GameObject nameplateGO = Find("Gameplay_Nameplate");
        var arrows = nameplateGO.GetComponent<NameplateArrows>().arrows;
        Vector3 nameplatePos = nameplateGO.transform.position;

        // Aim straight at the plate from in front of it (NOT grazing) first.
        cc.enabled = false;
        player.transform.position = new Vector3(nameplatePos.x, 0f, nameplatePos.z - 1f);
        cc.enabled = true;
        SetLook(fpc, 0f, 0f); // facing +Z, straight at the north-wall plate
        yield return null;
        yield return null;
        Assert.That(arrows[0].alpha, Is.LessThan(0.05f), "Aiming straight at the plate should not reveal it.");

        // Now graze along the wall from the side, close enough.
        cc.enabled = false;
        player.transform.position = new Vector3(nameplatePos.x - 1f, 0f, nameplatePos.z - 0.3f);
        cc.enabled = true;
        SetLook(fpc, 90f, 0f); // facing world +X, parallel to the plate's north-wall face
        yield return null;
        yield return null;
        bool anyRevealed = false;
        foreach (var a in arrows) if (a.alpha > 0.3f) anyRevealed = true;
        Assert.IsTrue(anyRevealed, "Grazing light along the plate should reveal its arrows.");

        // Read the (now revealed) arrows and set the breakers to match.
        GameObject breakerBox = Find("Gameplay_BreakerBox");
        var breakers = new BreakerSwitch[4];
        for (int i = 0; i < 4; i++)
            breakers[i] = breakerBox.transform.Find($"Breaker_{i}").GetComponent<BreakerSwitch>();
        var mainBreaker = breakerBox.transform.Find("MainBreakerLever").GetComponent<MainBreaker>();

        // Wrong attempt first: leave everything down (unless that's already correct, flip one).
        bool allDown = true;
        foreach (var up in GameState.Room3ArrowUp) if (up) allDown = false;
        if (allDown) Assert.Fail("GameState should never generate an all-down Room 3 pattern.");
        mainBreaker.Interact(null); // breakers are all down initially - wrong unless pattern happened to be all-down (excluded above)
        Assert.IsFalse((bool)typeof(MainBreaker).GetField("_solved", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(mainBreaker));
        foreach (var b in breakers) Assert.IsFalse(b.IsUp, "A wrong submit should reset everything to down.");

        // Now set them correctly.
        for (int i = 0; i < 4; i++)
            if (GameState.Room3ArrowUp[i]) breakers[i].Interact(null);
        mainBreaker.Interact(null);
        yield return new WaitForSeconds(1.2f); // door slide duration

        GameObject exit3 = Find("Gameplay_RoomExit3");
        Vector3 outward = exit3.transform.forward;
        cc.enabled = false;
        player.transform.position = exit3.transform.position - outward * 1.0f;
        player.transform.rotation = Quaternion.LookRotation(outward, Vector3.up);
        cc.enabled = true;
        yield return null;

        float elapsed = 0f;
        while (!GameState.IsRoomSolved(3) && elapsed < 3f)
        {
            cc.Move(outward * 3f * Time.deltaTime);
            yield return null;
            elapsed += Time.deltaTime;
        }

        Assert.IsTrue(GameState.IsRoomSolved(3), "Walking through Room 3's exit should mark it solved.");
        Assert.AreEqual("WhiteBox_LastLight", SceneManager.GetActiveScene().name);
        Assert.IsFalse(GameState.Won);
    }
}
