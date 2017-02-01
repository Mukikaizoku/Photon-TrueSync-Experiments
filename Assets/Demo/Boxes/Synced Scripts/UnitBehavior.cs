using UnityEngine;
using System.Collections;
using TrueSync;

// Controls the behaviour of a single unit
public class UnitBehavior : TrueSyncBehaviour {

    enum UnitState
    {
        None = 0,
        Walking,
        Attacking,
        Summoning
    }

    //private TSTransform tsTransform;      // NOTE: Already in TrueSyncBehavior
    private TSVector translation;

    [AddTracking]       // May be unnesscary?
    private UnitData unitData;


    private UnitState unitState = UnitState.None;
    private GameObject target;
    private int direction;
    private TextMesh textMesh;
    private FP summonTime = 0.5f;
    private FP attackDelay = 1f;
    private FP attackDelayCurrent = 1f;


    public override void OnSyncedStart() {
        Debug.Log("Started UnitBehavior Script for a unit (ID = " + gameObject.GetInstanceID() + ") owned by Player " + owner.Id);
        // Store a direction so we can make the local player always play on the same side and create the second player on the other side
        if (owner.Id != localOwner.Id)
        {
            direction = -1;
        } else
        {
            direction = 1;
        }
        
        // Hardcode some speed factor because this is just an experiement
        translation = (new TSVector(0, 0, 0.05f * direction));

        textMesh = GetComponentInChildren<TextMesh>();
        StateTracker.AddTracking(unitData);
        unitState = UnitState.Summoning;    // Initialize in a summoning state (to avoid a problem, please talk to MIK to discuss)
    }

    
    public override void OnSyncedUpdate () {

        // Check state
        switch (unitState)
        {
            case UnitState.Walking:
                tsTransform.Translate(translation);
                break;


            case UnitState.Attacking:
                if (attackDelayCurrent <= 0)
                {
                    if (target == null)
                    {
                        if (!FindNewTarget ())
                        {
                            unitState = UnitState.Walking;
                            break;
                        }
                    }
                    DealDamage(target, unitData.atk);
                    attackDelayCurrent = attackDelay;
                } else
                {
                    attackDelayCurrent -= TrueSyncManager.DeltaTime;
                }
                break;


            case UnitState.Summoning:
                if (summonTime > 0)
                {
                    summonTime -= TrueSyncManager.DeltaTime;
                } else
                {
                    unitState = UnitState.Walking;
                }
                break;

        }

        // Update health and kill object if necessary
        textMesh.text = "HP: " + unitData.currentHp + " / " + unitData.totalHp;

        if (unitData.currentHp <= 0)
        {
            TrueSyncManager.SyncedDestroy(gameObject);
        }

    }


    // Detect an enemy
    public void OnSyncedTriggerEnter (TSCollision other)
    {
        if (unitState != UnitState.Walking)
        {
            return;
        }


        Debug.Log("Unit: Collision Trigger Entered between " + gameObject.name + ":" + gameObject.GetInstanceID() + " and " + other.collider.name + ":" + other.collider.GetInstanceID());
        GameObject collided = other.collider.gameObject;


        // Check ownership of the collided
        if (collided.tag == "Unit")
        {
            if (collided.GetComponent<UnitBehavior>().owner.Id != owner.Id)
            {
                Debug.Log("Unit: New Target Aquired");
                target = collided;
                unitState = UnitState.Attacking;
            }
        } else if (collided.tag == "Base")
        {
            if (collided.GetComponent<BoxBehaviour>().owner.Id != owner.Id)
            {
                Debug.Log("Unit: New Base Target Aquired");
                target = collided;
                unitState = UnitState.Attacking;
            }
        }
        

    }


    public void SetData (UnitData stats)
    {
        unitData = new UnitData(stats.totalHp, stats.atk, stats.range);
        unitData.SetStateTracking();
    }


    public void DealDamage (GameObject target, int damage)
    {
        if (target.tag == "Unit")
        {
            Debug.Log("Attacking " + target.name + ":" + target.GetInstanceID());
            target.GetComponent<UnitBehavior>().unitData.currentHp -= damage;
        } else if (target.tag == "Base")
        {
            Debug.Log("Attacking " + target.name + ":" + target.GetInstanceID());
            target.GetComponent<BoxBehaviour>().ReceiveDamage(damage);
        }
    }


    public void ReceiveDamage (int damage)
    {
        unitData.currentHp -= damage;
    }

    private bool FindNewTarget ()
    {
        //TSCollider[] localTargets = Physics.OverlapSphere(gameObject.GetComponent<TSTransform>().position, unitData.range);

        // NOTE: no "TSPhysics" helper class, so not "TSPhysics.OverlapSphere
        return false;
    }

}
