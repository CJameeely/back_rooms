using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// Quiet original room tone and physical feedback; bootstrap also works after scene rebuilding.
public class GameSoundscape : MonoBehaviour
{
    AudioSource ambience, feedback;
    AudioClip step, click;
    FirstPersonController player;
    CharacterController controller;
    Vector3 previous;
    float distance;
    RoomTimer[] timers;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnScene;
        SceneManager.sceneLoaded += OnScene;
    }
    static void OnScene(Scene scene, LoadSceneMode mode)
    {
        if(Object.FindFirstObjectByType<GameSoundscape>() != null) return;
        new GameObject("Game Soundscape").AddComponent<GameSoundscape>();
    }
    void Start()
    {
        feedback=gameObject.AddComponent<AudioSource>();feedback.playOnAwake=false;feedback.spatialBlend=0;feedback.volume=.32f;
        step=Resources.Load<AudioClip>("Audio/Footstep");click=Resources.Load<AudioClip>("Audio/Button");
        foreach(var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            b.onClick.AddListener(Click);
        if(SceneManager.GetActiveScene().name!="WhiteBox_LastLight") return;
        ambience=gameObject.AddComponent<AudioSource>();ambience.clip=Resources.Load<AudioClip>("Audio/RoomTone");
        ambience.loop=true;ambience.spatialBlend=0;ambience.volume=.24f;ambience.Play();
        player=FirstPersonController.Instance;
        if(player!=null){controller=player.GetComponent<CharacterController>();previous=player.transform.position;}
        timers=Object.FindObjectsByType<RoomTimer>(FindObjectsSortMode.None);
    }
    void Click(){if(feedback!=null&&click!=null)feedback.PlayOneShot(click);}
    void Update()
    {
        if(player==null) return;
        Vector3 p=player.transform.position;Vector3 delta=p-previous;delta.y=0;previous=p;
        if(player.InputEnabled && controller!=null && controller.isGrounded && delta.magnitude<.5f)
        {
            distance+=delta.magnitude;
            if(distance>.95f){distance=0;feedback.pitch=Random.Range(.92f,1.06f);feedback.PlayOneShot(step,.8f);}
        }
        else distance=0;
        if(ambience!=null)
        {
            float target=.24f;
            if(timers!=null) foreach(var timer in timers)
            {
                if(timer==null || timer.roomLights.Length==0)continue;
                Vector3 centre=Vector3.zero;int count=0,on=0;
                foreach(var l in timer.roomLights) if(l!=null){centre+=l.transform.position;count++;if(l.enabled)on++;}
                if(count==0)continue;centre/=count;
                if(Mathf.Abs(p.x-centre.x)>3 || Mathf.Abs(p.z-centre.z)>3)continue;
                target=.05f+.19f*on/timer.roomLights.Length;
            }
            ambience.volume=Mathf.MoveTowards(ambience.volume,target,Time.deltaTime*.4f);
        }
    }
}
