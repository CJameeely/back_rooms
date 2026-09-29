using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CameraTourSmokeTest
{
    [UnityTest]
    public IEnumerator TourCameraMovesThroughAllWaypointsWithoutErrors()
    {
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight", LoadSceneMode.Single);
        yield return null;

        // TourCamera starts inactive, and GameObject.Find() skips inactive objects, so walk the
        // root hierarchy directly instead.
        GameObject tourGroup = null;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == "CameraTour") { tourGroup = root; break; }
        Assert.IsNotNull(tourGroup, "CameraTour group should exist in the scene.");
        Transform tourCamT = tourGroup.transform.Find("TourCamera");
        Assert.IsNotNull(tourCamT, "TourCamera should exist under CameraTour.");
        GameObject tourCam = tourCamT.gameObject;
        var driver = tourCam.GetComponent<CameraTourDriver>();
        Assert.IsNotNull(driver, "CameraTourDriver should be attached.");
        Assert.AreEqual(9, driver.waypoints.Length, "Should have one waypoint per room.");

        Vector3 startPos = driver.waypoints[0].position;
        Vector3 endPos = driver.waypoints[driver.waypoints.Length - 1].position;

        GameObject playerGroup = null;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == "Player") { playerGroup = root; break; }
        Transform flyCamT = playerGroup != null ? playerGroup.transform.Find("FlyCamera") : null;

        tourCam.SetActive(true);
        yield return null;

        Assert.IsNotNull(flyCamT, "FlyCamera should exist under Player.");
        Assert.IsFalse(flyCamT.gameObject.activeSelf, "FlyCamera should be auto-disabled by the tour driver.");

        // Speed up the tour drastically so the test doesn't need to wait for the real ~2.5s
        // per-room dwell time across 9 rooms.
        driver.moveSpeed = 200f;
        driver.turnSpeed = 2000f;
        driver.dwellTime = 0.05f;

        float elapsed = 0f;
        while (elapsed < 15f && Vector3.Distance(tourCam.transform.position, endPos) > 0.2f)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        Assert.Less(Vector3.Distance(tourCam.transform.position, endPos), 0.5f,
            $"Tour camera should reach the last waypoint within 15s (elapsed {elapsed:F1}s, pos={tourCam.transform.position}, target={endPos}).");
    }
}
