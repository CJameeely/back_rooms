using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ImmersiveTitleTest
{
    static void Capture(Camera cam,string name)
    {
        if(System.Environment.GetEnvironmentVariable("LL_TITLE_CAPTURE")!="1")return;
        string dir=Path.GetFullPath("../output/immersive_cover_20261020");Directory.CreateDirectory(dir);
        var rt=new RenderTexture(1600,900,24);cam.targetTexture=rt;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;
        var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();
        Directory.CreateDirectory(Path.GetDirectoryName(dir+"/"+name+".png"));
        File.WriteAllBytes(dir+"/"+name+".png",tex.EncodeToPNG());
        RenderTexture.active=null;cam.targetTexture=null;rt.Release();Object.Destroy(rt);Object.Destroy(tex);
    }
    [UnityTest]
    public IEnumerator CoverMovesGuideIsOptionalAndStartEntersGame()
    {
        yield return SceneManager.LoadSceneAsync("Title");yield return null;yield return null;
        var title=Object.FindFirstObjectByType<ImmersiveTitle>();Assert.IsNotNull(title);Assert.IsFalse(title.GuideOpen);
        Assert.IsNotNull(GameObject.Find("EnterButton"));Assert.IsNotNull(GameObject.Find("GuideButton"));
        var position=title.CoverCamera.transform.position;yield return new WaitForSeconds(.6f);
        Assert.Greater(Vector3.Distance(position,title.CoverCamera.transform.position),.001f);
        Capture(title.CoverCamera,"cover");
        if(System.Environment.GetEnvironmentVariable("LL_TITLE_CAPTURE")=="1")
            for(int i=0;i<120;i++){Capture(title.CoverCamera,"frames/"+i.ToString("D4"));yield return null;}

        title.SetGuide(true);yield return null;Assert.IsTrue(title.GuideOpen);Capture(title.CoverCamera,"guide");
        title.SetGuide(false);Assert.IsFalse(title.GuideOpen);
        title.BeginEnter();Assert.IsTrue(title.Entering);
        yield return new WaitForSeconds(2.4f);
        Assert.AreEqual("WhiteBox_LastLight",SceneManager.GetActiveScene().name);
        Assert.IsNotNull(FirstPersonController.Instance);StoryExperience.Instance.Close();
    }
}
