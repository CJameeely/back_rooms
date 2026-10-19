#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace WhiteBoxKit
{
    public static class Room2StylePass
    {
        const string Root = "Assets/Room2Style/";
        static Material Surface(string name, string texture, Color color, Vector2 tiling)
        {
            var path = Root + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetColor("_BaseColor", color);
            mat.SetTexture("_BaseMap", texture == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Textures/tex_" + texture + ".png"));
            mat.SetTextureScale("_BaseMap", tiling);
            mat.SetFloat("_Cull", (float)CullMode.Off); mat.SetFloat("_Smoothness", .12f); mat.SetFloat("_Metallic", 0);
            mat.DisableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", Color.black);
            EditorUtility.SetDirty(mat); return mat;
        }
        static void Assign(Renderer r, Material m) { r.sharedMaterials = Enumerable.Repeat(m, r.sharedMaterials.Length).ToArray(); r.receiveShadows = true; r.shadowCastingMode = ShadowCastingMode.On; }
        static void Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localScale = size;
            Object.DestroyImmediate(go.GetComponent<Collider>()); Assign(go.GetComponent<Renderer>(), mat);
            go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        public static void ApplyToLoadedScene()
        {
            var room = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.name.Contains("R2_LockedCounter") && t.parent != null && t.parent.name == "Rooms");
            ApplyRoom(room);
        }
        public static void ApplyRoom(Transform room)
        {
            if(room.name.Contains("R7_FinalAlignment"))
            {
                var north=room.Find("Walls/Wall_North_Center");
                if(north.Find("Mesh_JambL")==null)
                {
                    var parent=north.parent;var pos=north.position;var rot=north.rotation;
                    Object.DestroyImmediate(north.gameObject);
                    var frame=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WhiteBox/Prefabs/Wall_Door.prefab"),parent);
                    frame.name="Wall_North_Center";frame.transform.SetPositionAndRotation(pos,rot);
                }
                var seal=room.Find("Content/Exit_Seal");
                if(seal!=null) {seal.localPosition=new Vector3(0,1.05f,3.1f);seal.localRotation=Quaternion.identity;}
            }
            var old = room.Find("Room2StyleDetails"); if (old != null) Object.DestroyImmediate(old.gameObject);
            var details = new GameObject("Room2StyleDetails").transform; details.SetParent(room, false);
            var wall = Surface("Plaster", "wall", new Color(.77f,.74f,.65f), new Vector2(1,1.5f));
            var floor = Surface("Vinyl", "floor", new Color(.58f,.57f,.51f), new Vector2(2,2));
            var ceiling = Surface("Acoustic", "ceiling", new Color(.72f,.71f,.64f), Vector2.one);
            var green = Surface("PaintedMetal", "metal", new Color(.64f,.70f,.61f), Vector2.one);
            var trim = Surface("DarkTrim", "metal", new Color(.22f,.25f,.22f), Vector2.one);
            var enamel = Surface("CabinetEnamel", null, new Color(.88f,.86f,.74f), Vector2.one);
            var panel = Surface("LightDiffuser", null, new Color(.75f,.72f,.58f), Vector2.one);
            panel.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive; panel.EnableKeyword("_EMISSION"); panel.SetColor("_EmissionColor", new Color(1,.88f,.64f)*.65f);
            foreach (var r in room.Find("Floor").GetComponentsInChildren<Renderer>()) Assign(r, floor);
            foreach (var r in room.Find("Walls").GetComponentsInChildren<Renderer>())
            {
                Assign(r, wall);
                // Follow each actual wall segment; doorway openings remain clear.
                var b = r.bounds; var p = room.InverseTransformPoint(b.center);
                bool alongX = b.size.x > b.size.z;
                var facing = p;
                if(alongX) facing.z += p.z > 0 ? -b.size.z/2-.025f : b.size.z/2+.025f;
                else facing.x += p.x > 0 ? -b.size.x/2-.025f : b.size.x/2+.025f;
                Box(details,"PlasterFacing",facing,new Vector3(alongX?b.size.x:.035f,b.size.y,alongX?.035f:b.size.z),wall);
                if (b.min.y - room.position.y > 1) continue;
                var size = new Vector3(alongX ? b.size.x : .025f, 1.02f, alongX ? .025f : b.size.z);
                if (alongX) p.z += p.z > 0 ? -b.size.z/2-.055f : b.size.z/2+.055f;
                else p.x += p.x > 0 ? -b.size.x/2-.055f : b.size.x/2+.055f;
                p.y = .53f; Box(details,"LowerWall",p,size,green);
                // Trim must stand proud of the lower panel: coplanar front faces flicker in motion.
                Vector3 inward = alongX ? new Vector3(0,0,-Mathf.Sign(p.z)) : new Vector3(-Mathf.Sign(p.x),0,0);
                p += inward * .025f;
                p.y = 1.06f; size.y = .035f; Box(details,"DadoRail",p,size,trim);
                p.y = .065f; size.y = .13f; Box(details,"Skirting",p,size,trim);
            }
            foreach (var t in room.Find("Ceiling").GetComponentsInChildren<Transform>())
            {
                if (t.name == "Mesh_Slab") Assign(t.GetComponent<Renderer>(),ceiling);
                if (t.name == "Mesh_Panel")
                {
                    Assign(t.GetComponent<Renderer>(),panel);
                    t.localScale = new Vector3(1.3f,.02f,.38f);
                    // Housing attaches to a cosmetic group, keeping timed panel references intact.
                    Box(details,"FluorescentHousing",room.InverseTransformPoint(t.position)+Vector3.up*.035f,new Vector3(1.42f,.045f,.49f),trim);
                }
            }
            foreach (var l in room.Find("Ceiling").GetComponentsInChildren<Light>(true))
            {
                l.type = LightType.Spot; l.transform.localRotation = Quaternion.Euler(90,0,0);
                l.transform.localPosition = new Vector3(0,-.13f,0);
                l.spotAngle = 125; l.innerSpotAngle = 85; l.range = 7;
                l.intensity = 1.8f; l.color = new Color(1,.91f,.74f);
                if (Mathf.Abs(l.transform.parent.localPosition.x) > .1f)
                { l.type = LightType.Point; l.intensity = .7f; l.shadows = LightShadows.None; }
                else l.shadows = LightShadows.Soft; l.shadowBias = .12f; l.shadowNormalBias = .5f; l.shadowResolution = LightShadowResolution.Medium; l.shadowStrength = .8f;
            }
            foreach(var r in room.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.GetComponent<TMPro.TMP_Text>() != null) continue;
                if (r.transform.GetComponentsInParent<Transform>().Any(t=>t.name.StartsWith("__Tripo_")))
                {
                    var source = r.sharedMaterial; if(source == null) continue;
                    string id = r.transform.GetComponentsInParent<Transform>().First(t=>t.name.StartsWith("__Tripo_")).name;
                    string path=Root+id+".mat";
                    var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(mat==null) {mat=new Material(source);AssetDatabase.CreateAsset(mat,path);}
                    else if(source != mat) mat.CopyPropertiesFromMaterial(source);
                    mat.SetFloat("_Cull", (float)CullMode.Off); mat.SetFloat("_Smoothness",.06f);mat.SetFloat("_Metallic",0);mat.SetFloat("_BumpScale",.025f);
                    mat.SetTexture("_MetallicGlossMap",null);mat.DisableKeyword("_METALLICSPECGLOSSMAP");
                    mat.SetTexture("_BumpMap",null); mat.DisableKeyword("_NORMALMAP"); mat.SetFloat("_ReceiveShadows",0); mat.EnableKeyword("_RECEIVE_SHADOWS_OFF"); mat.SetFloat("_SpecularHighlights",0);mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                    Assign(r,mat);EditorUtility.SetDirty(mat);
                }
                else if (r.transform.GetComponentsInParent<Transform>().Any(t=>t.name == "Mesh_Body" || t.name == "Mesh_DrawerFront")) Assign(r,r.name=="Mesh_DrawerFront"?trim:enamel);
                else if (r.transform.GetComponentsInParent<Transform>().Any(t=>t.name.StartsWith("Pillar"))) Assign(r,green);
                else if (r.transform.GetComponentsInParent<Transform>().Any(t=>t.name == "Desk" || t.name == "Chair")) Assign(r,enamel);
                else if (r.sharedMaterial != null && r.sharedMaterial.name == "Mat_Structure") Assign(r,enamel);
                else if (r.sharedMaterial != null && r.sharedMaterial.name == "Mat_Interactive") Assign(r,enamel);
                else if(r.GetComponentsInParent<Transform>().Any(t=>t.name=="Trace_Decal"))
                    Assign(r,Surface("DustTrace","floor",new Color(.36f,.35f,.29f),Vector2.one));
                else if (r.sharedMaterial != null && r.sharedMaterial.name == "Mat_Clue")
                    Assign(r, Surface("CluePaper", "wall", new Color(.83f,.78f,.61f), Vector2.one));
            }

            // The original mirror sat across Room 6's entrance; mount it on the solid south wall.
            if(room.name.Contains("R6_MirrorAndCentre"))
            {
                var mirror=room.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Mirror_Panel");
                if(mirror!=null) {var p=room.InverseTransformPoint(mirror.position);p.x=-1.6f;mirror.position=room.TransformPoint(p);}
            }
            // Hide generator annotations, retaining numerical marks used as environmental clues.
            foreach(var label in room.GetComponentsInChildren<TMPro.TextMeshPro>(true))
                if(label.name.StartsWith("Label_"))
                {
                    if(label.text.Any(char.IsLetter)) label.GetComponent<Renderer>().enabled=false;
                    else
                    {
                        var p=room.InverseTransformPoint(label.transform.position);
                        p.x=Mathf.Clamp(p.x,-2.84f,2.84f);p.z=Mathf.Clamp(p.z,-2.84f,2.84f);
                        label.transform.position=room.TransformPoint(p);label.fontSize=1.2f;
                    }
                }
            foreach(var desk in room.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Desk"))
            {
                var top=desk.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled && !r.GetComponentsInParent<Transform>().Any(t=>t.name.StartsWith("__Tripo_") || t.name=="Paper" || t.name=="Key" || t.name.StartsWith("Lamp_Part"))).ToArray();
                if(top.Length==0) continue;
                float y=top.Max(r=>r.bounds.max.y);
                foreach(Transform prop in desk)
                    if(prop.name=="Paper"||prop.name=="Key"||prop.name.StartsWith("Lamp_Part"))
                    {var p=prop.position;p.y=y+.012f;prop.position=p;}
            }
            foreach (var t in room.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Trace_Decal"))
            {
                var p = room.InverseTransformPoint(t.position);
                if(Mathf.Abs(p.x)>2.9f) p.x=Mathf.Sign(p.x)*2.92f;
                if(Mathf.Abs(p.z)>2.9f) p.z=Mathf.Sign(p.z)*2.92f;
                t.position=room.TransformPoint(p);
            }
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Tools/WhiteBox/Room 2 Style and Preview")]
        public static void ApplyAndCapture()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/WhiteBox_LastLight.unity");
            ApplyToLoadedScene(); EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
            var room=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name.Contains("R2_LockedCounter")&&t.parent!=null&&t.parent.name=="Rooms");
            string dir=Path.GetFullPath("../output/room2_style_20261018"); Directory.CreateDirectory(dir);
            var go=new GameObject("StylePreview");var cam=go.AddComponent<Camera>();cam.nearClipPlane=.03f;cam.farClipPlane=100;cam.fieldOfView=55;
            var rt=new RenderTexture(1600,1000,24);cam.targetTexture=rt;
            var positions=new[]{new Vector3(1.5f,1.65f,1.6f),new Vector3(-2,1.65f,-1.4f)};
            var looks=new[]{new Vector3(-.7f,.8f,-2.2f),new Vector3(-.6f,1.5f,2.8f)};
            for(int i=0;i<2;i++)
            {
                go.transform.position=room.position+positions[i];go.transform.LookAt(room.position+looks[i]);cam.Render(); cam.Render(); cam.Render(); RenderTexture.active=rt;
                var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();
                File.WriteAllBytes(Path.Combine(dir,i==0?"after_south.png":"after_north.png"),tex.EncodeToPNG());Object.DestroyImmediate(tex);
            }
            RenderTexture.active=null;cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(go);
            Debug.Log("[Room2Style] Applied and captured two matching camera views.");
        }
    }
}
#endif
