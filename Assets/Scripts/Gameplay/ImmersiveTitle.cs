using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ImmersiveTitle : MonoBehaviour
{
    public bool GuideOpen => guide && guide.activeSelf;
    public bool Entering { get; private set; }
    public Camera CoverCamera { get; private set; }
    Canvas canvas;
    CanvasGroup menu;
    GameObject guide;
    Image fade;
    Light[] tubes;
    AudioSource hum;
    TMP_Text audioLabel,motionLabel;
    bool motion=true;
    float elapsed;
    Action enter;
    readonly Color ink=new Color(.88f,.91f,.83f);
    Material wall,green,metal,floor,glow;

    public void Initialize(Action onEnter)
    {
        enter=onEnter;
        var old=GameObject.Find("TitleCanvas");if(old)old.SetActive(false);
        foreach(var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))camera.gameObject.SetActive(false);
        BuildSpace();BuildMenu();
        hum=gameObject.AddComponent<AudioSource>();hum.clip=Resources.Load<AudioClip>("Audio/RoomTone");hum.loop=true;hum.volume=.13f;hum.Play();
        AudioListener.volume=PlayerPrefs.GetInt("LastLight.Muted",0)==1?0:1;audioLabel.text=AudioListener.volume==0?"SOUND  OFF":"SOUND  ON";
    }
    Material Mat(Color color,bool emissive=false)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=color;m.SetFloat("_Smoothness",.12f);
        if(emissive){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*2);}
        return m;
    }
    void Box(string name,Vector3 position,Vector3 size,Material material)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(transform);
        o.transform.position=position;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=material;Destroy(o.GetComponent<Collider>());
    }
    void BuildSpace()
    {
        wall=Mat(new Color(.56f,.59f,.51f));green=Mat(new Color(.20f,.27f,.24f));metal=Mat(new Color(.08f,.12f,.11f));floor=Mat(new Color(.25f,.27f,.23f));glow=Mat(new Color(.76f,.86f,.74f),true);
        RenderSettings.ambientLight=new Color(.20f,.24f,.20f);RenderSettings.ambientIntensity=.5f;
        var cameraObject=new GameObject("CoverCamera",typeof(Camera),typeof(AudioListener));cameraObject.transform.SetParent(transform);
        CoverCamera=cameraObject.GetComponent<Camera>();CoverCamera.nearClipPlane=.03f;CoverCamera.farClipPlane=55;CoverCamera.fieldOfView=62;
        CoverCamera.clearFlags=CameraClearFlags.SolidColor;CoverCamera.backgroundColor=new Color(.015f,.025f,.022f);
        Box("Floor",new Vector3(0,-.08f,11),new Vector3(6,.16f,30),floor);
        Box("Ceiling",new Vector3(0,3.25f,11),new Vector3(6,.12f,30),wall);
        for(int side=-1;side<=1;side+=2)
        {
            Box("Wall",new Vector3(side*3,1.6f,11),new Vector3(.16f,3.2f,30),wall);
            Box("LowerWall",new Vector3(side*2.90f,.55f,11),new Vector3(.05f,1.1f,30),green);
            Box("WallRail",new Vector3(side*2.86f,1.12f,11),new Vector3(.06f,.045f,30),metal);
            Box("Skirting",new Vector3(side*2.85f,.07f,11),new Vector3(.08f,.14f,30),metal);
            for(int i=0;i<4;i++)
            {
                float z=3+i*5;
                Box("DoorFrame",new Vector3(side*2.82f,1.2f,z),new Vector3(.15f,2.4f,1.3f),metal);
                Box("ClosedDoor",new Vector3(side*2.72f,1.15f,z),new Vector3(.08f,2.25f,1.1f),green);
                Box("DoorHandle",new Vector3(side*2.65f,1.05f,z+.35f),new Vector3(.08f,.07f,.22f),wall);
            }
        }
        Box("EndWall",new Vector3(0,1.6f,24),new Vector3(6,3.2f,.2f),wall);
        Box("EndDoorFrame",new Vector3(0,1.25f,23.85f),new Vector3(1.55f,2.5f,.12f),metal);
        Box("EndDoor",new Vector3(0,1.2f,23.75f),new Vector3(1.35f,2.4f,.12f),green);
        Box("LightUnderDoor",new Vector3(0,.025f,23.66f),new Vector3(1.3f,.035f,.025f),glow);
        tubes=new Light[6];
        for(int i=0;i<tubes.Length;i++)
        {
            float z=.5f+i*4;
            Box("Fixture",new Vector3(0,3.14f,z),new Vector3(1.35f,.12f,.45f),metal);
            Box("Diffuser",new Vector3(0,3.07f,z),new Vector3(1.16f,.035f,.32f),glow);
            var l=new GameObject("FluorescentLight").AddComponent<Light>();l.transform.SetParent(transform);l.transform.position=new Vector3(0,2.95f,z);
            l.type=LightType.Point;l.range=6;l.intensity=1.6f;l.color=new Color(.79f,.9f,.76f);tubes[i]=l;
        }
    }
    RectTransform Rect(GameObject o,Transform parent,Vector2 pos,Vector2 size)
    {
        o.transform.SetParent(parent,false);var r=o.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(pos.x,-pos.y);r.sizeDelta=size;return r;
    }
    Image Panel(string name,Transform parent,Vector2 pos,Vector2 size,Color color)
    {var o=new GameObject(name,typeof(RectTransform),typeof(Image));Rect(o,parent,pos,size);var i=o.GetComponent<Image>();i.color=color;return i;}
    TMP_Text Text(string name,Transform parent,string value,Vector2 pos,Vector2 size,float font,Color color)
    {
        var o=new GameObject(name,typeof(RectTransform));Rect(o,parent,pos,size);var t=o.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.fontSize=font;t.text=value;t.color=color;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;
    }
    TMP_Text Button(string name,Transform parent,string caption,Vector2 pos,Vector2 size,Action click,bool primary=false)
    {
        var image=Panel(name,parent,pos,size,primary?new Color(.71f,.77f,.63f):new Color(.10f,.16f,.14f,.95f));
        var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
        var c=button.colors;c.highlightedColor=new Color(.8f,.94f,.82f);c.pressedColor=new Color(.55f,.72f,.6f);button.colors=c;button.onClick.AddListener(()=>click());
        var t=Text(name+"Text",image.transform,caption,new Vector2(18,10),size-new Vector2(36,16),21,primary?new Color(.035f,.08f,.06f):ink);t.alignment=TextAlignmentOptions.MidlineLeft;return t;
    }
    void BuildMenu()
    {
        var o=new GameObject("ImmersiveTitleCanvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));o.transform.SetParent(transform);
        canvas=o.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=CoverCamera;canvas.planeDistance=.2f;
        var s=o.GetComponent<CanvasScaler>();s.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;s.referenceResolution=new Vector2(1600,900);s.matchWidthOrHeight=.5f;
        var main=new GameObject("Menu",typeof(RectTransform),typeof(CanvasGroup));Rect(main,canvas.transform,Vector2.zero,new Vector2(1600,900));menu=main.GetComponent<CanvasGroup>();
        Panel("LeftShade",main.transform,Vector2.zero,new Vector2(660,900),new Color(.025f,.045f,.039f,.94f)).raycastTarget=false;
        Text("Chapter",main.transform,"LEVEL 4.7  /  THE CLEAN ROOMS",new Vector2(76,110),new Vector2(530,35),19,new Color(.58f,.68f,.57f));
        var title=Text("Title",main.transform,"THE LAST\nLIGHT",new Vector2(70,174),new Vector2(570,220),84,ink);title.characterSpacing=7;title.lineSpacing=-8;
        Panel("Rule",main.transform,new Vector2(76,417),new Vector2(64,2),new Color(.63f,.74f,.6f));
        Text("Premise",main.transform,"3:07 AM. You leaned against a wall.\nThe room on the other side has no windows.",new Vector2(76,446),new Vector2(510,85),23,new Color(.65f,.72f,.65f));
        Button("EnterButton",main.transform,"ENTER THE CLEAN ROOMS  >",new Vector2(76,573),new Vector2(490,66),BeginEnter,true);
        Button("GuideButton",main.transform,"USER GUIDE",new Vector2(76,655),new Vector2(490,58),()=>SetGuide(true));
        audioLabel=Button("SoundButton",main.transform,"SOUND  ON",new Vector2(76,749),new Vector2(235,45),ToggleSound);
        motionLabel=Button("MotionButton",main.transform,"MOTION  ON",new Vector2(326,749),new Vector2(240,45),()=>{motion=!motion;motionLabel.text=motion?"MOTION  ON":"MOTION  OFF";});
        Text("Credit",main.transform,"CJ & EVAN",new Vector2(76,840),new Vector2(300,30),17,new Color(.5f,.59f,.52f));
        Button("ResetRecords",main.transform,"RESET RECORDS",new Vector2(1330,832),new Vector2(225,42),()=>{PlayerPrefs.DeleteKey("LastLight.Failures");PlayerPrefs.DeleteKey("LastLight.Loops");PlayerPrefs.Save();});
        Text("Time",main.transform,"03:07\nSIGNAL LOST",new Vector2(1340,65),new Vector2(220,80),23,ink).alignment=TextAlignmentOptions.TopRight;
        var g=Panel("UserGuide",canvas.transform,Vector2.zero,new Vector2(1600,900),new Color(.025f,.045f,.039f,1f));guide=g.gameObject;
        Text("GuideHeading",g.transform,"BEFORE YOU ENTER",new Vector2(160,110),new Vector2(1250,80),45,ink);
        Text("Controls",g.transform,"CONTROLS\n\nWASD     Move\nMouse    Look around\nE             Interact / pick up / place\nF             Toggle assembled lamp\nH             Open help in later rooms\nTab         Recall current objective\nEsc         Close a panel\n0-9         Keypad digits, Enter confirms",new Vector2(160,230),new Vector2(600,440),25,ink);
        Text("Rules",g.transform,"HOW TO PLAY\n\nExplore four rooms. Read the clues, use objects, and unlock each exit.\n\nThe lights warn you as time runs out. Reading notes and help pauses the room timer.\n\nWalk onto stools without jumping. Aiming at an object shows its interaction prompt.\n\nHelp stays hidden until you ask for it. The opening can be skipped with Space.",new Vector2(830,230),new Vector2(600,480),25,new Color(.72f,.79f,.7f));
        Button("CloseGuide",g.transform,"BACK TO COVER  /  ESC",new Vector2(160,748),new Vector2(420,62),()=>SetGuide(false),true);guide.SetActive(false);
        fade=Panel("EnterFade",canvas.transform,Vector2.zero,new Vector2(1600,900),Color.clear);fade.raycastTarget=false;
    }
    public void SetGuide(bool visible){if(!Entering){guide.SetActive(visible);menu.alpha=visible?0:1;menu.interactable=!visible;menu.blocksRaycasts=!visible;}}
    void ToggleSound(){AudioListener.volume=AudioListener.volume>0?0:1;audioLabel.text=AudioListener.volume==0?"SOUND  OFF":"SOUND  ON";PlayerPrefs.SetInt("LastLight.Muted",AudioListener.volume==0?1:0);PlayerPrefs.Save();}
    void Update()
    {
        if(!CoverCamera)return;
        elapsed+=Time.unscaledDeltaTime;
        if(!Entering)
        {
            CoverCamera.transform.position=new Vector3(-.6f+(motion?Mathf.Sin(elapsed*.16f)*.07f:0),1.62f+(motion?Mathf.Sin(elapsed*.23f)*.025f:0),-1.5f+(motion?Mathf.Sin(elapsed*.1f)*.15f:0));
            CoverCamera.transform.LookAt(new Vector3(-2,1.45f,19));
        }
        for(int i=0;i<tubes.Length;i++)tubes[i].intensity=motion?1.55f+.10f*Mathf.Sin(elapsed*.55f+i):1.55f;
        if(Keyboard.current==null||Entering)return;
        if(Keyboard.current.escapeKey.wasPressedThisFrame)SetGuide(false);
        if(Keyboard.current.hKey.wasPressedThisFrame)SetGuide(!GuideOpen);
        if(Keyboard.current.enterKey.wasPressedThisFrame&&!GuideOpen)BeginEnter();
    }
    public void BeginEnter(){if(!Entering)StartCoroutine(Enter());}
    IEnumerator Enter()
    {
        Entering=true;guide.SetActive(false);menu.interactable=false;menu.blocksRaycasts=false;fade.raycastTarget=true;
        Vector3 start=CoverCamera.transform.position;float duration=motion?1.6f:.35f,t=0;
        while(t<duration)
        {
            t+=Time.unscaledDeltaTime;float k=Mathf.Clamp01(t/duration);
            menu.alpha=1-k;hum.volume=.13f*(1-k);
            if(motion)CoverCamera.transform.position=start+Vector3.forward*(k*k*3f);
            fade.color=new Color(0,0,0,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,1,k)));
            yield return null;
        }
        enter?.Invoke();
    }
    void OnDestroy()
    {
        foreach(var m in new[]{wall,green,metal,floor,glow})if(m)Destroy(m);
    }
}
