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
    public IEnumerator ExitKeypadIsReachableFromInsideRoom()
    {
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight");
        yield return null;
        var keypad = Find("Gameplay_Keypad").transform;
        Vector3 origin = keypad.position + keypad.forward * 1.25f;
        Assert.IsTrue(Physics.Raycast(origin, -keypad.forward, out RaycastHit hit, 2f));
        Assert.IsNotNull(hit.collider.GetComponentInParent<KeypadController>(),
            "Player's interaction ray must hit keypad rather than the wall.");
        Assert.Greater(keypad.position.y, 1f, "Keypad must be above floor level.");
        var slab = Find("Gameplay_Door").transform.Find("Slab").GetComponent<Collider>();
        Assert.That(slab.bounds.min.y, Is.EqualTo(0f).Within(0.05f));
        Assert.Greater(slab.bounds.max.y, 2f, "Closed door must fill the doorway.");
    }

    [UnityTest]
    public IEnumerator IncorrectCodeCanBeClosedAndRetried()
    {
        GameState.BeginNewGame();
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight");
        yield return null;
        var managers = Find("GameplayManagers");
        var timer = managers.GetComponent<RoomTimer>();
        float elapsedBefore = ElapsedViaReflection(timer);

        // Room 2 now has its own KeypadUI too, so KeypadUI.Instance ("whichever was built most
        // recently") is no longer reliably Room 1's — go through Room 1's own KeypadController,
        // which was wired to its specific panel at build time.
        var keypad = Find("Gameplay_Keypad").GetComponent<KeypadController>().ui;
        keypad.Open();
        for (int i = 0; i < 4; i++)
            keypad.PressDigit((CodeManager.Instance.GetDigit((ClueSymbol)i) + 1) % 10);
        keypad.PressConfirm();
        Assert.IsFalse(keypad.Solved);
        // Spec 1.5: a wrong Room 1 guess now costs 5s, which previously did nothing.
        Assert.That(ElapsedViaReflection(timer), Is.GreaterThanOrEqualTo(elapsedBefore + 4.9f),
            "Wrong Room 1 guess should apply a 5s penalty.");

        keypad.Close(); // Closing during rejection must cancel its coroutine and reset the panel.
        keypad.Open();
        for (int i = 0; i < 4; i++) keypad.PressDigit(CodeManager.Instance.GetDigit((ClueSymbol)i));
        keypad.PressConfirm();
        yield return null;
        Assert.IsTrue(keypad.Solved);
        Assert.IsTrue(FirstPersonController.Instance.InputEnabled);
    }

    // RoomTimer keeps its elapsed-time accumulator private (nothing outside it needs a live
    // countdown readout — the whole point is no number is ever shown) so the penalty check above
    // reads it via reflection rather than adding a public field just for this one test.
    static float ElapsedViaReflection(RoomTimer timer)
    {
        var field = typeof(RoomTimer).GetField("_elapsed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (float)field.GetValue(timer);
    }

    [UnityTest]
    public IEnumerator FullRoom1Playthrough_UnlocksDoorAndMarksSolvedWithoutEndingGame()
    {
        // GameState is a static class, so without an explicit fresh start here it would carry
        // over whatever an earlier test in this same run left behind (e.g. Room 1 already marked
        // solved) — exactly what a real player gets from Title's "Start" button, just inlined.
        GameState.BeginNewGame();
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
        // Regression check: the paper used to be a descendant of the drawer front, which has a
        // non-uniform scale (0.4, 0.15, 0.05) — scale compounds down the whole transform chain,
        // so it silently squashed the paper's actual rendered mesh down to a sliver, invisible
        // to a player, even though the paper's own local scale looked fine in isolation. Checking
        // the renderer's real world-space bounds (not just a transform's lossyScale) is what
        // actually catches that, however many levels of parenting are involved.
        Bounds paperBounds = drawerClue.paperObject.GetComponentInChildren<Renderer>().bounds;
        Assert.Greater(paperBounds.size.x, 0.1f, $"Paper's rendered width looks squashed: {paperBounds.size}");
        Assert.Greater(paperBounds.size.z, 0.1f, $"Paper's rendered depth looks squashed: {paperBounds.size}");
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
        KeypadController keypadController = keypadGO.GetComponent<KeypadController>();
        keypadController.Interact(null);
        yield return null;
        KeypadUI keypad2 = keypadController.ui;
        Assert.IsTrue(keypad2.IsOpen, "Keypad UI should be open.");
        foreach (int d in digits)
            keypad2.PressDigit(d);
        keypad2.PressConfirm();
        yield return null;
        Assert.IsTrue(keypad2.Solved, "Correct code should solve the keypad.");

        // Door should now be sliding open; wait it out, then walk the player through the exit trigger.
        yield return new WaitForSeconds(1.2f);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Assert.IsNotNull(player, "Player should exist and be tagged 'Player'.");
        GameObject exitTrigger = Find("Gameplay_RoomExit");
        Assert.IsNotNull(exitTrigger);

        // Place the player just short of the (now-open) doorway, then walk it through with real
        // CharacterController.Move() sweeps, so the trigger fires the same way it would for an
        // actual player rather than relying on a teleport landing inside an already-overlapping volume.
        Vector3 outward = exitTrigger.transform.forward;
        CharacterController cc = player.GetComponent<CharacterController>();
        cc.enabled = false;
        player.transform.position = exitTrigger.transform.position - outward * 1.0f;
        player.transform.rotation = Quaternion.LookRotation(outward, Vector3.up);
        cc.enabled = true;
        yield return null;

        float elapsed = 0f;
        while (!GameState.IsRoomSolved(1) && elapsed < 3f)
        {
            cc.Move(outward * 3f * Time.deltaTime);
            yield return null;
            elapsed += Time.deltaTime;
        }

        // Per spec, Room 1's own exit no longer ends the game — it just marks Room 1 solved and
        // lets the player keep walking toward Room 2, still in the same scene.
        Assert.IsTrue(GameState.IsRoomSolved(1), "Walking through Room 1's exit should mark it solved.");
        Assert.AreEqual("WhiteBox_LastLight", SceneManager.GetActiveScene().name,
            "Room 1's exit must not load End by itself.");
        Assert.IsFalse(GameState.Won, "Nothing should have won the whole game yet.");
    }

    [UnityTest]
    public IEnumerator TimeoutReachesEndThenRetryRoomRestoresSameCode()
    {
        GameState.BeginNewGame(); // see FullRoom1Playthrough... for why this is needed here.
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight", LoadSceneMode.Single);
        yield return null;

        int[] codeBefore = (int[])GameState.Room1Code.Clone();

        GameObject managers = Find("GameplayManagers");
        RoomTimer timer = managers.GetComponent<RoomTimer>();
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
        Assert.IsFalse(GameState.Won, "GameState.Won should be false after timing out.");
        Assert.AreEqual(1, GameState.FailedRoomNumber, "Room 1 is the one that timed out.");
        yield return null;
        Assert.AreEqual(CursorLockMode.None, Cursor.lockState);

        // Retry Room: reload the level, same code, nothing marked solved.
        Object.FindFirstObjectByType<EndMenu>().OnRetryRoomPressed();
        yield return null;
        yield return null;
        Assert.AreEqual("WhiteBox_LastLight", SceneManager.GetActiveScene().name);
        CollectionAssert.AreEqual(codeBefore, GameState.Room1Code, "Retry Room must not regenerate Room 1's code.");
        Assert.IsFalse(Find("Gameplay_Keypad").GetComponent<KeypadController>().ui.Solved,
            "A fresh retry must start with the door locked again.");
        Assert.IsFalse(PaperCloseupUI.Instance.IsOpen);
        Assert.IsFalse(GameState.IsRoomSolved(1), "Room 1 was never actually solved in this run.");
    }

    [UnityTest]
    public IEnumerator TimeoutThenTitleButtonReturnsToTitle()
    {
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight", LoadSceneMode.Single);
        yield return null;

        GameObject managers = Find("GameplayManagers");
        RoomTimer timer = managers.GetComponent<RoomTimer>();
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
        Assert.AreEqual("End", SceneManager.GetActiveScene().name);

        yield return null;
        Object.FindFirstObjectByType<EndMenu>().OnTitlePressed();
        yield return null;
        yield return null;
        Assert.AreEqual("Title", SceneManager.GetActiveScene().name);
        Assert.IsNotNull(GameObject.Find("StartButton"));
    }
}
