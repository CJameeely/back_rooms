#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WhiteBoxKit
{
    public static class LevelStylePass
    {
        public static void ApplyToLoadedScene()
        {
            var rooms=GameObject.Find("Rooms").transform;
            foreach(Transform room in rooms) Room2StylePass.ApplyRoom(room);
            var corridors=GameObject.Find("Corridors");
            foreach(var r in corridors.GetComponentsInChildren<MeshRenderer>(true))
            {
                string id=r.name=="Mesh_Panel"?"LightDiffuser":r.name=="Mesh_Slab"?"Acoustic":
                    r.GetComponentsInParent<Transform>().Any(t=>t.name.StartsWith("Floor_"))?"Vinyl":"Plaster";
                var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Room2Style/"+id+".mat");
                r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();
                if(r.name=="Mesh_Panel") r.transform.localScale=new Vector3(1.3f,.02f,.38f);
            }
            foreach(var l in corridors.GetComponentsInChildren<Light>())
            {l.color=new Color(1,.91f,.74f);l.intensity=.7f;}
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Tools/WhiteBox/Style Entire Level and Preview")]
        public static void ApplyAndCapture()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/WhiteBox_LastLight.unity");
            ApplyToLoadedScene();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
            string dir=Path.GetFullPath("../output/level_style_20261018");Directory.CreateDirectory(dir);
            var go=new GameObject("LevelPreview");var cam=go.AddComponent<Camera>();cam.nearClipPlane=.03f;cam.farClipPlane=80;cam.fieldOfView=65;
            var rt=new RenderTexture(1600,1000,24);cam.targetTexture=rt;
            string html="<!doctype html><html lang='zh-CN'><meta charset='utf-8'><title>全关卡风格预览</title><style>body{background:#191c19;color:#e5e2d5;font:16px system-ui;margin:32px}section{display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-bottom:32px}img{width:100%}h2{font-weight:500}a{color:inherit}</style><h1>全关卡实际渲染</h1><p>9 个空间，统一暖灰墙面、灰绿护墙和旧金属材质。每个房间展示两个方向。</p>";
            foreach(Transform room in GameObject.Find("Rooms").transform)
            {
                html+="<h2>"+room.name+"</h2><section>";
                for(int i=0;i<2;i++)
                {
                    go.transform.position=room.position+(i==0?new Vector3(-2.35f,1.65f,-2.3f):new Vector3(1.2f,1.65f,2.3f));
                    go.transform.LookAt(room.position+(i==0?new Vector3(.5f,1.2f,1.5f):new Vector3(-.5f,1.2f,-1.5f)));
                    cam.Render();cam.Render();cam.Render();RenderTexture.active=rt;
                    var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();
                    string file=room.name+"_"+i+".png";File.WriteAllBytes(Path.Combine(dir,file),tex.EncodeToPNG());Object.DestroyImmediate(tex);
                    html+="<a href='"+file+"'><img src='"+file+"'></a>";
                }
                html+="</section>";
            }
            File.WriteAllText(Path.Combine(dir,"preview.html"),html+"</html>");
            RenderTexture.active=null;cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(go);
            Debug.Log("[LevelStyle] All rooms and corridors styled, 18 previews captured.");
        }
    }
}
#endif
