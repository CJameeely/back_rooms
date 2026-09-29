using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class WhiteBoxSmokeTest
{
    [UnityTest]
    public IEnumerator LevelSceneRunsForAFewSecondsWithoutErrors()
    {
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight", LoadSceneMode.Single);
        yield return null;

        for (int i = 0; i < 120; i++)
            yield return null;

        Assert.IsTrue(GameObject.Find("Rooms") != null, "Rooms group should exist in the loaded scene.");
        Assert.IsTrue(GameObject.Find("Player") != null, "Player group should exist in the loaded scene.");
    }
}
