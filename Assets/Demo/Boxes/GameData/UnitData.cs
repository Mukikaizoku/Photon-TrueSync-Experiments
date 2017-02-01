using UnityEngine;
using System.Collections;
using TrueSync;

public class UnitData {

    // Fields
    [AddTracking]
    public int currentHp;
    [AddTracking]
    public int totalHp;
    [AddTracking]
    public int atk;
    [AddTracking]
    public FP range;

    public UnitData(int hp, int atk, FP range)
    {
        this.currentHp = hp;
        this.totalHp = hp;
        this.atk = atk;
        this.range = range;
    }

    // Enable a behaviour to enable tracking on the class' fields
    public void SetStateTracking ()
    {
        StateTracker.AddTracking(this);
    }
}
