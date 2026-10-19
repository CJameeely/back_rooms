#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace WhiteBoxKit
{
    // Visual skins only: original interaction transforms/colliders retain their identity.
    public static class TripoArtPass
    {
        const string Root = "Assets/TripoProps/";
        const string ScenePath = "Assets/Scenes/WhiteBox_LastLight.unity";

        static Material MaterialFor(string id)
        {
            string folder = Root + id + "/";
            string path = folder + "Game.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            var color = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "BaseColor.jpg") ?? AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "BaseColor.png");
            mat.SetTexture("_BaseMap", color); mat.SetColor("_BaseColor", Color.white);
            string normalPath = folder + "Normal.png";
            var importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            { importer.textureType = TextureImporterType.NormalMap; importer.maxTextureSize = 1024; importer.SaveAndReimport(); }
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath)); mat.SetFloat("_BumpScale", .15f); mat.EnableKeyword("_NORMALMAP");
            var metal = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "MetalSmooth.png");
            if (metal != null) { mat.SetTexture("_MetallicGlossMap", metal); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); mat.SetFloat("_Smoothness", .2f); }
            else { mat.SetFloat("_Metallic", .15f); mat.SetFloat("_Smoothness", .25f); }
            EditorUtility.SetDirty(mat); return mat;
        }

        public static GameObject Model(Transform parent, string id, Vector3 size, Vector3 center, Vector3 euler = default)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + id + "/" + id + ".fbx");
            if (prefab == null) throw new InvalidOperationException("Missing prepared Tripo model: " + id);
            var holder = new GameObject("__Tripo_" + id);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            model.transform.SetParent(holder.transform, false);
            model.transform.localRotation = Quaternion.Euler(euler) * model.transform.localRotation;
            var renderers = model.GetComponentsInChildren<Renderer>();
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            model.transform.position -= b.center;
            holder.transform.localScale = new Vector3(size.x / Mathf.Max(b.size.x,.0001f), size.y / Mathf.Max(b.size.y,.0001f), size.z / Mathf.Max(b.size.z,.0001f));
            var mat = MaterialFor(id);
            foreach (var r in renderers) r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
            holder.transform.SetParent(parent,false); holder.transform.localPosition = center;
            return holder;
        }

        static void Skin(Transform target, string id, Vector3 size, Vector3 center, Vector3 euler = default)
        {
            if (target == null) return;
            foreach (Transform child in target.Cast<Transform>().Where(t=>t.name.StartsWith("__Tripo_")).ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            foreach (var r in target.GetComponentsInChildren<MeshRenderer>(true)) r.enabled = r.GetComponent<TMPro.TMP_Text>() != null;
            Model(target,id,size,center,euler);
        }
        static Transform Find(string name) => UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name == name);
        static Transform Room(string name) => Find("Rooms").Cast<Transform>().First(t=>t.name.Contains(name));

        [MenuItem("Tools/WhiteBox/Step 5 - Apply Tripo Art")]
        public static void Apply()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
            // Keep collider dimensions and parent scripts intact, even on scaled primitive roots.
            Skin(Find("Gameplay_Stool"),"metal_stool",Vector3.one,Vector3.zero);
            // Pivot the stool at its feet: placement code places this root at the floor hit.
            var stool=Find("Gameplay_Stool");
            if(stool != null && stool.GetComponent<BoxCollider>() is BoxCollider sc && sc.center.y == 0)
            { stool.position -= Vector3.up * .225f; sc.center=Vector3.up*.5f; stool.Find("__Tripo_metal_stool").localPosition=Vector3.up*.5f; }
            else if(stool!=null) stool.Find("__Tripo_metal_stool").localPosition=Vector3.up*.5f;
            Skin(Find("Gameplay_DataMatrix"),"industrial_monitor",new Vector3(.68f,1.36f,2f),Vector3.zero,new Vector3(0,180,0));
            var reader=Find("Gameplay_CardReader");
            if(reader!=null) { Skin(reader.Find("Body"),"metal_doorbell_button",Vector3.one,Vector3.zero); }
            Skin(Find("Gameplay_CalibrationCard"),"space_station_tile",Vector3.one,Vector3.zero,new Vector3(90,0,0));
            var cover=UnityEngine.Object.FindObjectsByType<CeilingCardPanel>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault();
            if(cover!=null)
            {
                Skin(cover.cover,"metal_storage_box",new Vector3(1.2f,.045f,.8f),Vector3.zero,new Vector3(90,0,0));
                var card=Model(cover.cover,"industrial_panel",new Vector3(.18f,.01f,.11f),new Vector3(.35f,.06f,.2f),new Vector3(90,0,0));
            }
            foreach(var keypad in UnityEngine.Object.FindObjectsByType<KeypadController>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                Skin(keypad.transform,"vintage_calculator",new Vector3(.32f,.48f,.09f),new Vector3(0,0,.035f));
            var sw=Find("Gameplay_Switch");
            Skin(sw,"old_light_switch",new Vector3(.14f,.22f,.06f),Vector3.zero);
            foreach(string name in new[]{"Gameplay_Door","Gameplay_ExitDoor"})
                Skin(Find(name),"metal_cabinet",new Vector3(1f,2.1f,.15f),Vector3.zero);
            // Future room props remain set dressing until those puzzles are implemented.
            foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).ToArray())
            {
                if(t == null) continue;
                if(t.name=="Mirror_Panel") Skin(t,"rusty_framed_mirror",new Vector3(1,1.6f,.07f),new Vector3(0,0,.04f));
                if(t.name=="Dial_Panel") Skin(t,"vintage_dial_timer",new Vector3(.5f,.5f,.1f),new Vector3(0,0,.05f));
                if(t.name=="Key") Skin(t,"golden_key",new Vector3(.08f,.02f,.19f),Vector3.up*.01f,new Vector3(90,0,0));
                if(t.name=="Lamp_PartA") Skin(t,"industrial_connector_base",new Vector3(.32f,.24f,.26f),Vector3.up*.12f);
                if(t.name=="Lamp_PartB") Skin(t,"vintage_submarine_lantern",new Vector3(.3f,.32f,.10f),Vector3.up*.16f);
            }
            var r2=Room("R2_LockedCounter");
            // Fixed housing surrounds the existing timed light; do not replace its renderer reference.
            var tile=r2.GetComponentsInChildren<Transform>().First(t=>t.name=="Ceiling_Light_2x2_L5");
            foreach(Transform c in tile.Cast<Transform>().Where(t=>t.name.StartsWith("__Tripo_")).ToArray()) UnityEngine.Object.DestroyImmediate(c.gameObject);
            Model(tile,"industrial_panel_1",new Vector3(1.65f,.10f,1.65f),new Vector3(0,-.03f,0),new Vector3(90,0,0));
            var content=r2.Find("Content");
            var old=content.Find("ArtDressing");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var dressing=new GameObject("ArtDressing").transform;dressing.SetParent(content,false);
            Model(dressing,"vintage_metal_toolbox",new Vector3(.42f,.25f,.28f),new Vector3(-.3f,1.025f,-2.5f));
            Model(dressing,"metal_tray",new Vector3(.4f,.045f,.26f),new Vector3(.35f,.925f,-2.5f));
            Model(dressing,"old_parchment_folder",new Vector3(.30f,.015f,.22f),new Vector3(.35f,.96f,-2.5f),new Vector3(90,0,0));
            LevelStylePass.ApplyToLoadedScene();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Tripo] All 18 model types applied; gameplay colliders retained.");
        }

        public static void CaptureDetails()
        {
            string dir=Path.GetFullPath("../output/model_integration_20261018");Directory.CreateDirectory(dir);
            var go=new GameObject("ArtPreviewCamera");var cam=go.AddComponent<Camera>();cam.nearClipPlane=.03f;cam.farClipPlane=100;cam.fieldOfView=55;
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.18f,.18f,.18f);
            var rt=new RenderTexture(1600,1000,24);cam.targetTexture=rt;
            Action<string,Vector3,Vector3> shot=(name,pos,look)=>{
                go.transform.position=pos;go.transform.LookAt(look);cam.Render();RenderTexture.active=rt;
                var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();
                File.WriteAllBytes(Path.Combine(dir,name+".png"),tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);RenderTexture.active=null;
            };
            var r=Room("R2_LockedCounter");
            shot("unity_room2_south",r.position+new Vector3(1.5f,1.65f,1.6f),r.position+new Vector3(-.7f,.8f,-2.2f));
            shot("unity_room2_north",r.position+new Vector3(-2f,1.65f,-1.4f),r.position+new Vector3(-.6f,1.5f,2.8f));
            var stool=Find("Gameplay_Stool");shot("unity_stool",stool.position+new Vector3(.7f,.85f,1),stool.position+Vector3.up*.2f);
            var card=Find("Gameplay_CalibrationCard");bool active=card.gameObject.activeSelf;card.gameObject.SetActive(true);
            shot("unity_cards",card.position+new Vector3(.2f,.5f,.65f),card.position);card.gameObject.SetActive(active);
            var holder=new GameObject("PreviewModel");holder.transform.position=new Vector3(0,-100,0);
            Model(holder.transform,"industrial_monitor",new Vector3(1,1,.15f),Vector3.zero);
            shot("unity_monitor_plus_z",holder.transform.position+new Vector3(0,0,1.8f),holder.transform.position);
            shot("unity_monitor_minus_z",holder.transform.position+new Vector3(0,0,-1.8f),holder.transform.position);
            UnityEngine.Object.DestroyImmediate(holder);cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
        }

        [MenuItem("Tools/WhiteBox/Rebuild Level With Tripo Art")]
        public static void RebuildAll()
        {
            WhiteBoxModules.Step1_ImportAndGeneratePrefabs();
            WhiteBoxLevelBuilder.Step2_BuildLevel();
            GameplayBuilder.Step3_BuildRoom1Gameplay();
            Room2GameplayBuilder.Step3b_BuildRoom2Gameplay();
            SceneFlowBuilder.Step4_BuildTitleAndEndScenes();
            EditorSceneManager.OpenScene(ScenePath);BuildAndCapture();
        }

        public static void BuildAndCapture()
        {
            Apply(); WhiteBoxScreenshotTool.CaptureScreenshots(); CaptureDetails();
        }
    }
}
#endif
