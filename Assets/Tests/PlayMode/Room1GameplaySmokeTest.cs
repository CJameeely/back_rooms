using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class Room1GameplaySmokeTest
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

    [UnityTest]
    public IEnumerator FullWinPlaythrough_ReachesEndSceneWithWin()
    {
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight", LoadSceneMode.Single);
        yield return null;

        GameObject codeManagerGO = Find("GameplayManagers");
        Assert.IsNotNull(codeManagerGO, "GameplayManagers should exist.");
        CodeManager code = codeManagerGO.GetComponent<CodeManager>();
        Assert.IsNotNull(code);

        int[] digits =
        {
            code.GetDigit(ClueSymbol.Circle),
            code.GetDigit(ClueSymbol.Triangle),
            code.GetDigit(ClueSymbol.Square),
            code.GetDigit(ClueSymbol.Bar),
        };

        // Clue 1: move the cabinet.
        GameObject cabinet = Find("Desk");
        Assert.IsNotNull(cabinet, "Cabinet (Desk) should exist in Room 1.");
        var cabinetClue = cabinet.GetComponent<MovableCabinetClue>();
        cabinetClue.Interact(null);
        yield return new WaitForSeconds(1.2f); // slide duration
        Assert.IsTrue(cabinetClue.revealTarget.activeSelf, "Reveal text should activate after the cabinet slides.");

        // Clue 2: open drawer, then read the paper.
        GameObject drawer = Find("Gameplay_Drawer");
        Assert.IsNotNull(drawer);
        var drawerClue = drawer.transform.Find("Mesh_DrawerFront").GetComponent<DrawerClue>();
        drawerClue.Interact(null);
        yield return new WaitForSeconds(0.8f);
        Assert.IsTrue(drawerClue.paperObject.activeSelf, "Paper should be revealed after the drawer opens.");
        var paperClue = drawerClue.paperObject.GetComponent<PaperCloseupClue>();
        paperClue.Interact(null);
        yield return null;
        Assert.IsTrue(PaperCloseupUI.Instance.IsOpen, "Paper close-up should be open.");

        // Clue 3: pillar text is passive (no E needed) — nothing to trigger, just confirms it exists.
        GameObject pillarClue = Find("Gameplay_PillarClue");
        Assert.IsNotNull(pillarClue, "Pillar clue label should exist.");

        // Clue 4: switch must be OFF to be readable.
        GameObject switchGO = Find("Gameplay_Switch");
        Assert.IsNotNull(switchGO);
        var switchClue = switchGO.GetComponent<LightSwitchClue>();
        Assert.IsTrue(switchClue.CanInteract);
        switchClue.Interact(null); // turn it off
        yield return new WaitForSeconds(0.6f);
        Assert.IsTrue(switchClue.IsReadable, "Clue 4 should be readable once its light is off.");

        // Keypad: open, enter the real code, confirm.
        GameObject keypadGO = Find("Gameplay_Keypad");
        Assert.IsNotNull(keypadGO);
        keypadGO.GetComponent<KeypadController>().Interact(null);
        yield return null;
        Assert.IsTrue(KeypadUI.Instance.IsOpen, "Keypad UI should be open.");
        foreach (int d in digits)
            KeypadUI.Instance.PressDigit(d);
        KeypadUI.Instance.PressConfirm();
        yield return null;
        Assert.IsTrue(KeypadUI.Instance.Solved, "Correct code should solve the keypad.");

        // Door should now be sliding open; wait it out, then walk the player through the win trigger.
        yield return new WaitForSeconds(1.2f);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Assert.IsNotNull(player, "Player should exist and be tagged 'Player'.");
        GameObject winTrigger = Find("Gameplay_WinTrigger");
        Assert.IsNotNull(winTrigger);

        // Place the player just short of the (now-open) doorway, then walk it through with real
        // CharacterController.Move() sweeps, so the trigger fires the same way it would for an
        // actual player rather than relying on a teleport landing inside an already-overlapping volume.
        Vector3 outward = winTrigger.transform.forward;
        CharacterController cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.position = winTrigger.transform.position - outward * 1.0f;
        player.transform.rotation = Quaternion.LookRotation(outward, Vector3.up);
        cc.enabled = true;
        yield return null;

        float elapsed = 0f;
        while (SceneManager.GetActiveScene().name != "End" && elapsed < 3f)
        {
            cc.Move(outward * 3f * Time.deltaTime);
            yield return null;
            elapsed += Time.deltaTime;
        }

        Assert.AreEqual("End", SceneManager.GetActiveScene().name, "Walking through the win trigger should load End.");
        Assert.IsTrue(GameResult.Won, "GameResult.Won should be true after winning.");
    }

    [UnityTest]
    public IEnumerator TimeoutWithoutAnyInput_ReachesEndSceneWithLoss()
    {
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight", LoadSceneMode.Single);
        yield return null;

        GameObject managers = Find("GameplayManagers");
        LightsOutTimer timer = managers.GetComponent<LightsOutTimer>();
        Assert.IsNotNull(timer);
        // Compress the whole countdown so the test doesn't need to wait 180 real seconds.
        timer.totalDuration = 1.0f;
        timer.flickerWindow = 0.6f;
        timer.shutoffWindow = 0.2f;
        timer.blackoutHold = 0.2f;

        float elapsed = 0f;
        while (SceneManager.GetActiveScene().name != "End" && elapsed < 5f)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        Assert.AreEqual("End", SceneManager.GetActiveScene().name, "Timing out should load End.");
        Assert.IsFalse(GameResult.Won, "GameResult.Won should be false after timing out.");
    }
}
