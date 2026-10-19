using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// On-demand, state-based instructions. H opens a readable help panel and pauses the room timer.
public class CampaignGuidance : MonoBehaviour
{
    public string CurrentHelp { get; private set; }
    public void OpenHelp()
    {
        if (!string.IsNullOrEmpty(CurrentHelp) && FirstPersonController.Instance.InputEnabled)
            StoryExperience.Instance.Read("CURRENT OBJECTIVE",CurrentHelp+"\n\nE interacts with the object under your crosshair. F toggles the lamp. Your timer is paused while this help is open.");
    }
    void Update()
    {
        var g=FullCampaign.Instance;var p=FirstPersonController.Instance;
        if (!g || !p) return;
        bool active=p.transform.position.z>14 && !RoomTimer.ReadingPaused;
        if(!active)return;
        Vector3 target;
        string step;
        if(g.DarkEscapeArmed) {step="ESCAPE\nKeep walking forward into the dark. The walls can no longer stop you.";target=p.transform.position+p.transform.forward*8;}
        else if(g.MasterOff) {step="LAST LIGHT\nPress F to switch off your lamp. Then walk forward into the dark.";target=p.transform.position;}
        else if(g.RoomFourSolved) {step=g.TapeFourHeard?"EXIT CORRIDOR\nPress E on the master switch. Then turn off your own lamp with F.":"EXIT CORRIDOR\nWalk onto the stool and look up. Press E to read tape 04 before choosing the door.";target=g.TapeFourHeard?new Vector3(32.95f,1.3f,42):new Vector3(32,1,40);}
        else if(g.RoomThreeSolved) {
            if(!g.MirrorDiscovered) {step="FIND THE REFLECTION\nF: lamp on. Aim at the steel plate. Press E to rotate it until symbols appear.";target=new Vector3(22,1.25f,30.45f);}
            else {step="OPEN THE MAINTENANCE PANEL\nRecorded order: "+string.Join(" ",GameState.Room4MirrorOrder.Select(CodeManager.SymbolGlyph))+"\nMatching digits: "+string.Join(" ",GameState.Room4MirrorOrder.Select(s=>GameState.Room1Code[(int)s]))+"\nE: turn each dial, then confirm.";target=new Vector3(26.4f,1.1f,34.5f);}
        }
        else if(!g.LampOwned) {
            bool hasBase=Inventory.Instance.HasItem("LampBase"),hasHead=Inventory.Instance.HasItem("LampHead");
            step=!hasBase?"ASSEMBLE THE LAMP\nTake the base from the shelf in the previous corridor. E: pick up.":!hasHead?"ASSEMBLE THE LAMP\nTake the lamp head from the lower workbench shelf. E: pick up.":"ASSEMBLE THE LAMP\nBring both parts to the cradle. E: assemble and take the lamp.";
            target=!hasBase?new Vector3(10.6f,1.1f,17.5f):!hasHead?new Vector3(14.7f,.48f,23.35f):new Vector3(18.4f,1,25.2f);
        }
        else if(!g.ArrowsDiscovered) {step="REVEAL THE ARROWS\n"+(g.LampOn?"Lamp ON. ":"Press F to switch on your lamp. ")+"Aim at the plate above the four switches. Move within 3 metres.";target=new Vector3(17.35f,1.55f,26.61f);}
        else {step="MATCH THE FOUR SWITCHES\nRecorded: "+string.Join("  ",GameState.Room3ArrowUp.Select(v=>v?"UP":"DOWN"))+"\nE: toggle switches left to right.\nThen E on the tall switch to submit.";target=new Vector3(17.35f,1,26.55f);}
        var delta=target-p.transform.position;delta.y=0;
        float angle=Vector3.SignedAngle(p.transform.forward,delta,Vector3.up);
        string direction=Mathf.Abs(angle)>130?"behind you":angle>25?"to your right":angle< -25?"to your left":"ahead";
        CurrentHelp=step+"\n\nTarget: "+delta.magnitude.ToString("0.0")+" m, "+direction+".";
        if(Keyboard.current!=null && Keyboard.current.hKey.wasPressedThisFrame && p.InputEnabled)
            OpenHelp();
    }
}
