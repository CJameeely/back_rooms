using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public class SoundscapeTest
{
    [UnityTest]
    public IEnumerator SoundscapeLoadsOnceAndDimsWithRoomLights()
    {
        GameState.BeginNewGame();yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight");yield return null;yield return null;
        var sound=Object.FindObjectsByType<GameSoundscape>(FindObjectsSortMode.None);Assert.AreEqual(1,sound.Length);
        var loop=sound[0].GetComponents<AudioSource>().Single(s=>s.loop);
        Assert.AreEqual("RoomTone",loop.clip.name);Assert.IsTrue(loop.isPlaying);
        Assert.IsNotNull(Resources.Load<AudioClip>("Audio/Footstep"));Assert.IsNotNull(Resources.Load<AudioClip>("Audio/Button"));
        var timer=Object.FindObjectsByType<RoomTimer>(FindObjectsSortMode.None).First(t=>t.roomNumber==2);
        var centre=timer.roomLights.Aggregate(Vector3.zero,(p,l)=>p+l.transform.position)/timer.roomLights.Length;
        var player=FirstPersonController.Instance;player.SetInputEnabled(false);var cc=player.GetComponent<CharacterController>();
        cc.enabled=false;player.transform.position=new Vector3(centre.x,0,centre.z);cc.enabled=true;
        foreach(var l in timer.roomLights)l.enabled=false;
        yield return new WaitForSeconds(.7f);Assert.LessOrEqual(loop.volume,.06f,"Room tone should quiet down with the room blackout.");
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight");yield return null;
        Assert.AreEqual(1,Object.FindObjectsByType<GameSoundscape>(FindObjectsSortMode.None).Length);
    }
}
