#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace WhiteBoxKit
{
    public static class DemoRecorder
    {
        public static void Capture() => Capture(false);
        public static void CaptureWallFix()
        {
            LevelStylePass.ApplyAndCapture();
            Capture(true);
        }
        static void Capture(bool wallOnly)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/WhiteBox_LastLight.unity");
            var dir=Path.GetFullPath(wallOnly?"../output/wall_fix_20261018":"../output/demo_20261018");Directory.CreateDirectory(dir);
            var proc=new Process{StartInfo=new ProcessStartInfo("/opt/homebrew/bin/ffmpeg",
                "-y -loglevel error -f rawvideo -pixel_format rgb24 -video_size 1280x720 -framerate 24 -i pipe:0 -an -vf vflip -c:v libx264 -threads 4 -preset fast -crf 19 -pix_fmt yuv420p \""+Path.Combine(dir,"engine_capture.mp4")+"\"")
                {UseShellExecute=false,RedirectStandardInput=true}};
            proc.Start();var stream=proc.StandardInput.BaseStream;
            var go=new GameObject("DemoCaptureCamera");var cam=go.AddComponent<Camera>();cam.fieldOfView=65;cam.nearClipPlane=.04f;cam.farClipPlane=80;
            var rt=new RenderTexture(1280,720,24);var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);cam.targetTexture=rt;
            Action<Vector3,Vector3,Vector3,Vector3,int> shot=(a,b,la,lb,seconds)=>
            {
                for(int frame=0;frame<seconds*24;frame++)
                {
                    float t=frame/(seconds*24f-1);float ease=t*t*(3-2*t);
                    go.transform.position=Vector3.Lerp(a,b,ease);go.transform.LookAt(Vector3.Lerp(la,lb,ease));
                    cam.Render();if(frame==0){cam.Render();cam.Render();}
                    RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();
                    var bytes=tex.GetRawTextureData<byte>().ToArray();stream.Write(bytes,0,bytes.Length);
                }
            };
            var rooms=GameObject.Find("Rooms").transform.Cast<Transform>().OrderBy(t=>t.name).ToArray();
            if(wallOnly)
            {
                var p=rooms[2].position;
                shot(p+new Vector3(1.4f,1.2f,.5f),p+new Vector3(-1.3f,1.2f,.5f),p+new Vector3(-.7f,.35f,-2.9f),p+new Vector3(.7f,.35f,-2.9f),8);
            }
            else
            {
            for(int i=0;i<rooms.Length;i++)
            {
                var p=rooms[i].position;
                bool reverse=i==5||i==7||i==8;
                Vector3 a=reverse?new Vector3(1.1f,1.65f,2.25f):new Vector3(-2.25f,1.65f,-1.9f);
                Vector3 b=a+new Vector3(.25f,0,-.12f);
                Vector3 la=reverse?new Vector3(-.9f,1.1f,-2.1f):new Vector3(-.1f,1.15f,2.2f);
                Vector3 lb=la+new Vector3(.85f,0,0);
                shot(p+a,p+b,p+la,p+lb,6);
                UnityEngine.Debug.Log("[Demo] Captured "+rooms[i].name);
            }
            var r2=rooms[2].position;
            shot(r2+new Vector3(.9f,1.35f,-.9f),r2+new Vector3(.65f,1.35f,-1.1f),r2+new Vector3(0,.95f,-2.5f),r2+new Vector3(0,.95f,-2.5f),4);
            shot(r2+new Vector3(-1.6f,1.5f,1.6f),r2+new Vector3(-1.4f,1.5f,1.7f),r2+new Vector3(-1.2f,1.5f,3),r2+new Vector3(-1.2f,1.5f,3),4);
            var r7=rooms[8].position;
            shot(r7+new Vector3(.7f,1.5f,-1.4f),r7+new Vector3(.9f,1.5f,-1.5f),r7+new Vector3(1.5f,1.4f,-3),r7+new Vector3(1.5f,1.4f,-3),4);
            }
            stream.Close();proc.WaitForExit();if(proc.ExitCode!=0)throw new Exception("ffmpeg capture failed");
            RenderTexture.active=null;cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(go);
            UnityEngine.Debug.Log(wallOnly?"[Demo] 8-second wall-fix motion check complete.":"[Demo] 66-second engine capture complete.");
        }
    }
}
#endif
