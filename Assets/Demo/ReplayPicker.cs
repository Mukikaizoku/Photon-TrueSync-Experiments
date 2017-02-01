using UnityEngine;
using TrueSync;

// attached to each room on join list to do a join if the players hit the button
public class ReplayPicker : MonoBehaviour {

	public ReplayRecordInfo replayRecord;	

	public void Pick () {
        ReplayRecord.replayToLoad = replayRecord.Load();
        ReplayRecord.replayMode = ReplayMode.LOAD_REPLAY;

        Menu.instance.ReplayPanel_LoadLevel();
    }   

}