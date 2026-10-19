using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class TripoArtSmokeTest
{
    [UnityTest]
    public IEnumerator ImportedArtRetainsVisibleCluesAndFloorAlignedStool()
    {
        GameState.BeginNewGame();
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight");
        yield return null;
        var stool=GameObject.Find("Gameplay_Stool");
        var collider=stool.GetComponent<BoxCollider>();
        Assert.That(collider.bounds.min.y,Is.EqualTo(stool.transform.position.y).Within(.01f),"Placed stool must rest on its feet, not sink halfway into floor.");
        var renders=stool.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();
        Assert.IsNotEmpty(renders);
        var bounds=renders[0].bounds;
        foreach(var r in renders) bounds.Encapsulate(r.bounds);
        Assert.That(bounds.min.y,Is.EqualTo(collider.bounds.min.y).Within(.01f));
        Assert.That(bounds.max.y,Is.EqualTo(collider.bounds.max.y).Within(.01f));
        Assert.IsNotNull(stool.GetComponent<StoolPickupTarget>());
        var lightClue=Object.FindFirstObjectByType<LightSwitchClue>();
        Assert.IsTrue(lightClue.label.GetComponent<Renderer>().enabled,"Art replacement must not hide world-space clue text.");
        var art=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name.StartsWith("__Tripo_")).ToArray();
        Assert.GreaterOrEqual(art.Select(t=>t.name).Distinct().Count(),18);
        foreach(var t in art)
        {
            Assert.IsEmpty(t.GetComponentsInChildren<Collider>(true),"Visual skins must not introduce blocking colliders.");
            foreach(var r in t.GetComponentsInChildren<Renderer>(true))
                foreach(var m in r.sharedMaterials)
                { Assert.AreEqual("Universal Render Pipeline/Lit",m.shader.name);Assert.IsNotNull(m.GetTexture("_BaseMap")); }
        }
    }
}
