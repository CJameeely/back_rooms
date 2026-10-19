using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class LevelTraversalTest
{
    [UnityTest]
    public IEnumerator AllNineRoomsHaveWalkableConnectionsAfterPuzzleDoorsOpen()
    {
        GameState.BeginNewGame();
        yield return SceneManager.LoadSceneAsync("WhiteBox_LastLight");yield return null;yield return null;yield return null;
        foreach(var f in Object.FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None)) f.enabled=false;
        foreach(var c in Object.FindObjectsByType<CharacterController>(FindObjectsSortMode.None)) c.enabled=false;
        // Door unlocking is covered by Room1/Room2 playthrough tests; here audit physical clearance.
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if(t.name=="Gameplay_Door"||t.name=="Gameplay_ExitDoor"||t.name=="R3ExitDoor"||t.name=="R4ExitDoor")
                foreach(var c in t.GetComponentsInChildren<Collider>()) c.enabled=false;
        Physics.SyncTransforms();
        var rooms=GameObject.Find("Rooms").transform.Cast<Transform>().OrderBy(t=>t.name).ToArray();
        Assert.AreEqual(9,rooms.Length);
        foreach(var room in rooms)
        {
            Assert.IsNotNull(room.Find("Room2StyleDetails"));
            Assert.IsEmpty(room.Find("Room2StyleDetails").GetComponentsInChildren<Collider>());
        }
        for(int i=1;i<rooms.Length;i++)
        {
            Vector3 a=rooms[i-1].position,b=rooms[i].position;
            // Search a quarter-metre ground grid around both rooms, allowing detours around furniture.
            int minX=Mathf.RoundToInt((Mathf.Min(a.x,b.x)-2.5f)*4),maxX=Mathf.RoundToInt((Mathf.Max(a.x,b.x)+2.5f)*4);
            int minZ=Mathf.RoundToInt((Mathf.Min(a.z,b.z)-2.5f)*4),maxZ=Mathf.RoundToInt((Mathf.Max(a.z,b.z)+2.5f)*4);
            var q=new Queue<Vector2Int>();var seen=new HashSet<Vector2Int>();
            var seed=new Vector2Int(Mathf.RoundToInt(a.x*4),Mathf.RoundToInt(a.z*4));
            // Some room centres intentionally hold furniture: choose a nearby standing position.
            bool seeded=false;
            for(int radius=0;radius<=6&&!seeded;radius++)
                for(int dx=-radius;dx<=radius&&!seeded;dx++) for(int dz=-radius;dz<=radius&&!seeded;dz++)
                {var n=seed+new Vector2Int(dx,dz);if(Clear(n)){q.Enqueue(n);seen.Add(n);seeded=true;}}
            Assert.IsTrue(seeded,rooms[i-1].name+" has no standing position");bool reached=false;
            while(q.Count>0)
            {
                var n=q.Dequeue();var p=new Vector3(n.x*.25f,0,n.y*.25f);
                if(Vector3.Distance(p,b)<1.5f){reached=true;break;}
                foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right})
                {var next=n+d;if(next.x<minX||next.x>maxX||next.y<minZ||next.y>maxZ||!seen.Add(next)||!Clear(next))continue;q.Enqueue(next);}
            }
            Assert.IsTrue(reached,rooms[i-1].name+" -> "+rooms[i].name+" blocked for a standing player");
        }
    }
    static bool Clear(Vector2Int n)
    {
        var p=new Vector3(n.x*.25f,0,n.y*.25f);
        return Physics.Raycast(p+Vector3.up*.15f,Vector3.down,.3f,~0,QueryTriggerInteraction.Ignore)
            && !Physics.CheckCapsule(p+Vector3.up*.32f,p+Vector3.up*1.5f,.25f,~0,QueryTriggerInteraction.Ignore);
    }
}
