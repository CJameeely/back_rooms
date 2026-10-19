using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class StoryHandoffTest
{
    [UnityTest]
    public IEnumerator ReadingPausesTimeAndDiscoveryRewardsOnlyOnce()
    {
        GameState.BeginNewGame();
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight");
        yield return null; yield return null;
        var timer = Object.FindObjectsByType<RoomTimer>(FindObjectsSortMode.None).First(t => t.roomNumber == 1);
        var tape = GameObject.Find("Story_Tape1").GetComponent<StoryObject>();
        Assert.AreEqual(StoryExperience.TapeOne, tape.body);
        Assert.IsNotNull(GameObject.Find("Story_AlmondWater"));
        var secret = GameObject.Find("Story_UnderDesk").GetComponent<StoryObject>();
        var before = timer.RemainingSeconds;
        secret.Interact(FirstPersonController.Instance.gameObject);
        yield return new WaitForSeconds(.25f);
        Assert.AreEqual(before + 45, timer.RemainingSeconds, .05f);
        Assert.IsTrue(StoryExperience.IsReading);
        StoryExperience.Instance.Close();
        yield return new WaitForSeconds(.15f);
        Assert.Less(timer.RemainingSeconds, before + 45);
        before = timer.RemainingSeconds;
        secret.Interact(FirstPersonController.Instance.gameObject);
        Assert.AreEqual(before, timer.RemainingSeconds, .05f);
        StoryExperience.Instance.Close();
        var id = GameObject.Find("Story_StudentID_L7").GetComponent<StoryObject>();
        Assert.IsFalse(id.CanInteract, "A player on the ground must not collect the elevated ID.");
        timer.StopCountdown(); before = timer.RemainingSeconds;
        yield return new WaitForSeconds(.2f);
        Assert.AreEqual(before, timer.RemainingSeconds, .05f);
    }
    [UnityTest]
    public IEnumerator OpeningRunsOnceAndPausesTheRoom()
    {
        GameState.BeginNewGame(); StoryExperience.IntroPending = true;
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight");
        yield return null; yield return null;
        Assert.IsTrue(StoryExperience.IsReading);
        var timer = Object.FindObjectsByType<RoomTimer>(FindObjectsSortMode.None).First(t => t.roomNumber == 1);
        var before = timer.RemainingSeconds;
        yield return new WaitForSeconds(.3f);
        Assert.AreEqual(before, timer.RemainingSeconds, .05f);
        yield return new WaitForSeconds(12f);
        Assert.IsFalse(StoryExperience.IsReading);
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight");
        yield return null; yield return null;
        Assert.IsFalse(StoryExperience.IsReading, "Reload / retry must not replay the opening.");
    }
}
