using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ScenePlacementAuditTest
{
    [Serializable] public class Entry { public string path; public Vector3 min,max; public bool collider; }
    [Serializable] public class Report { public List<Entry> objects=new List<Entry>(); }
    static string PathOf(Transform t) => t.parent ? PathOf(t.parent)+"/"+t.name : t.name;
    [UnityTest]
    public IEnumerator InspectAllRoomsAndOnDemandHelp()
    {
        GameState.BeginNewGame();yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight");
        yield return null;yield return null;yield return null;
        Assert.IsNull(GameObject.Find("CampaignGuidanceHUD"),"Help must not create an always-visible panel.");
        foreach(var pair in new[]{new[]{"LampBase","FShelf"},new[]{"LampHead","WorkbenchLowerShelf"}})
        {
            var prop=GameObject.Find(pair[0]);var rs=prop.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();
            var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
            var shelf=GameObject.Find(pair[1]).GetComponent<Renderer>().bounds;
            Assert.That(b.min.y-shelf.max.y,Is.InRange(-.006f,.015f),pair[0]+" should rest on its shelf.");
            Assert.That(b.center.x,Is.InRange(shelf.min.x,shelf.max.x));
            Assert.That(b.center.z,Is.InRange(shelf.min.z,shelf.max.z));
        }
        Assert.Less(GameObject.Find("ReflectionBacking").GetComponent<Renderer>().bounds.max.z,30.4425f,
            "Backing must not cover the north-facing reflection surface.");
        StoryExperience.Instance.Close();
        var controller=FirstPersonController.Instance;var cc=controller.GetComponent<CharacterController>();
        cc.enabled=false;controller.transform.position=new Vector3(16,0,24);cc.enabled=true;
        yield return null;yield return null;
        Assert.IsFalse(StoryExperience.IsReading,"Help should stay closed during play.");
        FullCampaign.Instance.GetComponent<CampaignGuidance>().OpenHelp();
        Assert.IsTrue(StoryExperience.IsReading);Assert.IsTrue(RoomTimer.ReadingPaused);
        StoryExperience.Instance.Close();yield return null;
        Assert.IsFalse(StoryExperience.IsReading);
        var cover=UnityEngine.Object.FindFirstObjectByType<CeilingCardPanel>();
        cover.Interact(null);yield return new WaitForSeconds(.5f);
        foreach(var renderer in cover.GetComponentsInChildren<MeshRenderer>())
            if(renderer.enabled)Assert.Less(renderer.bounds.max.y,3.20f,"Open lid must swing down, not through the ceiling.");
        Assert.IsFalse(cover.transform.Find("__Tripo_industrial_panel").gameObject.activeSelf);
        cover.transform.localRotation=Quaternion.identity;
        if(Environment.GetEnvironmentVariable("LL_LAYOUT_CAPTURE")!="1")yield break;
        StoryExperience.Instance.Close();FirstPersonController.Instance.enabled=false;
        string dir=System.IO.Path.GetFullPath("../output/layout_fix_20261020");Directory.CreateDirectory(dir);
        var report=new Report();
        foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            if(r.enabled && r.GetComponent<TMPro.TMP_Text>()==null)
                report.objects.Add(new Entry{path=PathOf(r.transform),min=r.bounds.min,max=r.bounds.max,collider=r.GetComponent<Collider>()!=null});
        File.WriteAllText(dir+"/bounds.json",JsonUtility.ToJson(report,true));
        var cam=FirstPersonController.Instance.Camera;var rt=new RenderTexture(960,600,24);cam.targetTexture=rt;
        cam.fieldOfView=65;
        var rooms=GameObject.Find("Rooms").transform.Cast<Transform>().ToArray();
        foreach(var room in rooms)
        {
            for(int side=0;side<4;side++)
            {
                Vector3 offset=Quaternion.Euler(0,side*90,0)*new Vector3(0,0,-2.3f);
                cam.transform.position=room.position+offset+Vector3.up*1.7f;
                cam.transform.LookAt(room.position-offset*.6f+Vector3.up*.95f);
                cam.Render();RenderTexture.active=rt;
                var tex=new Texture2D(960,600,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,960,600),0,0);tex.Apply();
                File.WriteAllBytes(dir+"/"+room.name+"_"+side+".png",tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);
            }
        }
        RenderTexture.active=null;cam.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(rt);
    }
}
