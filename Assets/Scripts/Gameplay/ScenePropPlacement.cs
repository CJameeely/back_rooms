using System.Linq;
using TMPro;
using UnityEngine;

// Authored fixtures support the puzzle props; visual repairs do not add invisible interaction walls.
public static class ScenePropPlacement
{
    static Transform root;
    static Material material;
    static GameObject Find(string name) => GameObject.Find(name);
    static GameObject Solid(string name,Vector3 position,Vector3 size,bool collidable=true)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(root);
        o.transform.position=position;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=material;
        if(!collidable)Object.Destroy(o.GetComponent<Collider>());
        return o;
    }
    static void Legs(string name,Vector3 center,Vector3 size,float bottom)
    {
        float height=bottom;
        foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
            Solid(name+"Leg",new Vector3(center.x+x*(size.x/2-.05f),height/2,center.z+z*(size.z/2-.05f)),new Vector3(.055f,height,.055f));
    }
    static void WallMount(string name,float wallZ)
    {
        var o=Find(name);if(!o)return;
        var b=o.GetComponent<Renderer>().bounds;
        float depth=wallZ-b.max.z;
        if(depth>0)Solid(name+"Mount",new Vector3(b.center.x,b.center.y,b.max.z+depth/2),new Vector3(b.size.x+.06f,b.size.y+.06f,depth),false);
    }
    static void Move(string name,Vector3 delta) {var o=Find(name);if(o)o.transform.position+=delta;}
    public static void Apply(FullCampaign campaign)
    {
        root=new GameObject("PropSupports").transform;root.SetParent(campaign.transform);
        material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=new Color(.26f,.29f,.27f);
        material.SetFloat("_Smoothness",.15f);
        // Remove old vertical placeholder trace cards. They were untextured planes intersecting walls,
        // not any of the playable symbols, notes, or clean-floor clue.
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if(t.name=="Trace_Decal")
            {
                var r=t.GetComponentInChildren<MeshRenderer>();
                if(r && r.bounds.size.y>.05f)t.gameObject.SetActive(false);
            }
            if(t.name.StartsWith("Handprint_"))
            {
                t.rotation=Quaternion.Euler(0,-90,0);
                var p=t.position;p.x=-2.82f;t.position=p;t.localScale*=.25f;
                foreach(var c in t.GetComponentsInChildren<Collider>())c.enabled=false;
            }
        }
        // Wall-mounted screens and controls must sit in front of the added plaster facing.
        Move("Gameplay_DataMatrix",new Vector3(0,0,-.16f));
        Move("Gameplay_Keypad",new Vector3(-.12f,0,0));
        Move("Gameplay_Keypad2",new Vector3(0,0,-.12f));
        Move("Gameplay_Switch",new Vector3(.15f,0,0));
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if(t.name=="Dial_Panel")t.position+=new Vector3(0,0,.14f);
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if(t.name=="Counter_Drawer" || t.name=="Gameplay_Drawer" || t.name=="Gameplay_SampleCabinet")
            {
                var body=t.Find("Mesh_Body");var renderer=body?body.GetComponent<Renderer>():null;
                if(renderer && renderer.bounds.min.y<0)t.position+=Vector3.up*(-renderer.bounds.min.y);
            }
        var repeat=GameObject.Find("Room_04_E1_Repeat");
        if(repeat){var chair=repeat.transform.Find("Content/Chair");if(chair)chair.position+=Vector3.right*.25f;}
        var final=GameObject.Find("Room_09_R7_FinalAlignment");
        if(final){var chair=final.transform.Find("Content/Chair");if(chair)chair.position+=Vector3.forward*.25f;}
        // Countertop clutter uses the actual visible counter surface, not the old primitive height.
        var cabinet=Find("Gameplay_SampleCabinet");
        if(cabinet)
        {
            var body=cabinet.transform.Find("Mesh_Body").GetComponent<Renderer>();
            float top=body.bounds.max.y;
            var dressing=cabinet.transform.parent.Find("ArtDressing");
            if(dressing)foreach(Transform t in dressing)
            {
                var rs=t.GetComponentsInChildren<Renderer>();if(rs.Length==0)continue;
                var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                t.position+=Vector3.right*(t.name.Contains("toolbox")?-.05f:.05f);
                float y=t.name.Contains("folder")?top+.05f:top;
                t.position+=Vector3.up*(y+.003f-b.min.y);
            }
        }
        var tape=Find("Story_Tape1");
        if(tape && cabinet){var top=cabinet.transform.Find("Mesh_Body").GetComponent<Renderer>().bounds.max.y;tape.transform.position=new Vector3(8.02f,top+.07f,5.53f);}
        Move("FShelf",new Vector3(-.13f,0,0));
        Legs("FShelf",new Vector3(10.52f,1,17.5f),new Vector3(.55f,.08f,.6f),.96f);
        Legs("WorkbenchShelf",new Vector3(14.7f,.34f,23.35f),new Vector3(.65f,.08f,.5f),.30f);
        // Lamp cradle becomes a real bench, with four legs and a visible top.
        Legs("Cradle",new Vector3(18.4f,1,25.2f),new Vector3(.75f,.16f,.4f),.92f);
        Solid("BreakerBackboard",new Vector3(17.65f,1.25f,26.7675f),new Vector3(2.15f,1.2f,.185f),false);
        // The mounting plate begins behind the clue plate; short stems connect the controls.
        // Do not bring the whole board forward: that hides the printed arrows at z=26.61.
        for(int i=0;i<4;i++)WallMount("Breaker"+i,26.675f);
        WallMount("MainBreaker",26.675f);
        var cradleLabel=Find("CradleLabel");if(cradleLabel)
        {
            cradleLabel.transform.position=new Vector3(18.76f,1.55f,25.2f);cradleLabel.transform.rotation=Quaternion.Euler(0,90,0);
            var text=cradleLabel.GetComponent<TMP_Text>();text.rectTransform.sizeDelta=new Vector2(1.1f,.55f);text.fontSize=.8f;
            Solid("CradleInstructionsBacking",new Vector3(18.80f,1.55f,25.2f),new Vector3(.06f,.58f,1.15f),false);
        }
        WallMount("Tape2",26.86f);WallMount("Tape3",34.86f);WallMount("AftermathNote",26.86f);
        Solid("MaintenanceBackboard",new Vector3(26,1.08f,34.70f),new Vector3(1.9f,.95f,.30f),false);
        // A pedestal and fork support the rotating plate instead of leaving it suspended.
        Solid("MirrorFoot",new Vector3(22,.06f,30),new Vector3(1.1f,.12f,.95f));
        Solid("MirrorPost",new Vector3(22,.38f,30),new Vector3(.14f,.64f,.14f));
        Solid("MirrorLeftUpright",new Vector3(21.48f,1.1f,30),new Vector3(.09f,1.1f,.14f));
        Solid("MirrorRightUpright",new Vector3(22.52f,1.1f,30),new Vector3(.09f,1.1f,.14f));
        Solid("ReflectionBacking",new Vector3(22,1.25f,30.42f),new Vector3(.86f,.96f,.04f),false);
        Solid("ReflectionPost",new Vector3(22,.42f,30.42f),new Vector3(.1f,.84f,.08f));
        // Attach the pod sign to its front face, instead of floating over Shen.
        var sign=Find("PodLabel");if(sign){sign.transform.position=new Vector3(24,1.02f,31.43f);sign.GetComponent<TMP_Text>().fontSize=.8f;}
        // Shen rests on a mattress, clear of the former opaque 'window' slab.
        Move("Shen",Vector3.up*.04f);
        var window=Find("PodWindow");if(window)window.name="PodMattress";
        Solid("Tape4LightCover",new Vector3(32,2.59f,40),new Vector3(.55f,.04f,.45f),false);
        Solid("Tape4CoverMount",new Vector3(32,2.66f,40.22f),new Vector3(.55f,.12f,.04f),false);
        Solid("MirrorInstructionPlate",new Vector3(22,.48f,30.45f),new Vector3(.85f,.18f,.04f),false);
        // The light cover opens around its rear hinge, not through the ceiling around its centre.
        var cover=Object.FindFirstObjectByType<CeilingCardPanel>();
        if(cover)
        {
            var t=cover.cover;
            t.position+=new Vector3(0,-.10f,.4f);
            foreach(Transform child in t)child.localPosition-=Vector3.forward*.4f;
            var box=t.GetComponent<BoxCollider>();box.center-=Vector3.forward*.4f;
            var housing=t.parent.Find("__Tripo_industrial_panel_1");
            if(housing){housing.localScale=new Vector3(housing.localScale.x,housing.localScale.y,housing.localScale.z*.12f);housing.position+=new Vector3(0,-.10f,.56f);}
            var card=t.Find("__Tripo_industrial_panel");if(card)card.localPosition-=Vector3.up*.035f;
        }
        Solid("StudentIDTray",new Vector3(6.15f,3.085f,6.15f),new Vector3(.32f,.025f,.22f),false);
        Solid("StudentIDTrayBracket",new Vector3(6.15f,3.14f,6.27f),new Vector3(.32f,.12f,.025f),false);
        Move("Story_RepeatScratches",new Vector3(.07f,0,0));
        var underDesk=Find("Story_UnderDesk");
        if(underDesk)
        {
            // Attach the note to the desk's front underside so it follows the movable desk.
            underDesk.transform.position+=new Vector3(0,0,.04f);
        }
        Physics.SyncTransforms();
    }
}
