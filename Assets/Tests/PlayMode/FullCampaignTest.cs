using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class FullCampaignTest
{
    static void Use(string name) => GameObject.Find(name).GetComponent<CampaignInteractable>().Interact(FirstPersonController.Instance.gameObject);
    static void Place(Vector3 position)
    { var p=FirstPersonController.Instance;var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=position;cc.enabled=true; }
    [UnityTest]
    public IEnumerator NorthPassageDecalsDoNotBlockAStandingPlayer()
    {
        GameState.BeginNewGame();yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight");yield return null;yield return null;yield return null;
        Assert.IsFalse(Physics.CheckCapsule(new Vector3(16,.4f,18.92f),new Vector3(16,1.4f,18.92f),.36f));
    }
    [UnityTest]
    public IEnumerator MissingPartsThenBothPuzzlesAndNormalEnding()
    {
        GameState.BeginNewGame();yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight");yield return null;yield return null;yield return null;
        var game=FullCampaign.Instance;
        var cradle=GameObject.Find("LampCradle").GetComponent<CampaignInteractable>();
        Assert.IsFalse(cradle.CanInteract);
        Use("LampHead");Assert.IsFalse(cradle.CanInteract,"Missing F base must require a return trip.");
        Use("LampBase");Assert.IsTrue(cradle.CanInteract);Use("LampCradle");Assert.IsTrue(game.LampOwned);
        Place(new Vector3(16,0,24));yield return null;
        Assert.AreEqual(3,GameState.CheckpointRoom);
        var before=Object.FindObjectsByType<RoomTimer>(FindObjectsSortMode.None).First(t=>t.roomNumber==3).RemainingSeconds;
        Use("MainBreaker");Assert.IsFalse(game.RoomThreeSolved);
        Assert.Less(Object.FindObjectsByType<RoomTimer>(FindObjectsSortMode.None).First(t=>t.roomNumber==3).RemainingSeconds,before);
        for(int i=0;i<4;i++)if(GameState.Room3ArrowUp[i])Use("Breaker"+i);
        Use("MainBreaker");Assert.IsTrue(game.RoomThreeSolved);Assert.IsNull(GameObject.Find("R3ExitDoor"));
        Place(new Vector3(24,0,32));yield return null;
        Use("MaintenancePanel");
        var answer=GameState.Room4MirrorOrder.Select(s=>GameState.Room1Code[(int)s]).ToArray();
        // Explicit wrong answer, then recovery.
        int wrong=(answer[0]+1)%10;for(int k=0;k<wrong;k++)Use("Dial0");Use("DialConfirm");Assert.IsFalse(game.RoomFourSolved);
        for(int i=0;i<4;i++)for(int j=0;j<answer[i];j++)Use("Dial"+i);
        Use("DialConfirm");Assert.IsTrue(game.RoomFourSolved);
        Use("LoopDoor");yield return new WaitForSeconds(4.3f);
        Assert.AreEqual("End",SceneManager.GetActiveScene().name);Assert.IsTrue(GameState.Won);Assert.AreEqual("normal",FullCampaign.Ending);
    }
    [UnityTest]
    public IEnumerator RetryRestoresRoomFourAndLampWithoutRegeneratingSecrets()
    {
        GameState.BeginNewGame();GameState.MarkRoomSolved(1);GameState.MarkRoomSolved(2);GameState.MarkRoomSolved(3);
        GameState.SetCheckpoint(4,new System.Collections.Generic.List<string>{"InspectionLamp"});
        var code=(int[])GameState.Room1Code.Clone();GameState.Fail(4);
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight");yield return null;yield return null;yield return null;
        Assert.Less(Vector3.Distance(FirstPersonController.Instance.transform.position,new Vector3(24,0,30)),.2f);
        Assert.IsTrue(FullCampaign.Instance.LampOwned);Assert.AreEqual(4,GameState.CheckpointRoom);
        CollectionAssert.AreEqual(code,GameState.Room1Code);Assert.IsTrue(GameState.IsRoomSolved(3));
        Assert.AreEqual(270,Object.FindObjectsByType<RoomTimer>(FindObjectsSortMode.None).First(t=>t.roomNumber==4).totalDuration);
    }
    [UnityTest]
    public IEnumerator MasterSwitchRequiresTapeAndDarkExitReachesCampus()
    {
        GameState.BeginNewGame();yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight");yield return null;yield return null;yield return null;
        var game=FullCampaign.Instance;Use("LampBase");Use("LampHead");Use("LampCradle");
        Assert.IsFalse(game.MasterOff);Assert.IsFalse(game.TapeFourHeard);
        Place(new Vector3(32,.45f,40));Use("Tape4");Assert.IsTrue(game.TapeFourHeard);StoryExperience.Instance.Close();
        Use("MasterSwitch");Assert.IsTrue(game.MasterOff);
        // Invoke the same transition used when the player toggles their last light off.
        typeof(FullCampaign).GetMethod("ArmDarkEscape",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(game,null);
        Assert.IsTrue(game.DarkEscapeArmed);
        Place(new Vector3(41,0,40));yield return null;yield return new WaitForSeconds(.2f);
        Assert.AreEqual("CampusLab",SceneManager.GetActiveScene().name);
        yield return new WaitForSeconds(5.3f);Assert.AreEqual("End",SceneManager.GetActiveScene().name);Assert.IsTrue(GameState.Won);Assert.AreEqual("true",FullCampaign.Ending);
    }
}
