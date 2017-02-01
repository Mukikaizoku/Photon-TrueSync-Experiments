using UnityEngine;
using System.Collections;
using TrueSync;

public class BoxBehaviour : TrueSyncBehaviour {


    // Fields
    [AddTracking]
    private BoxData boxData;

    private GameObject target;
    private TextMesh textMesh;
    private int direction;

    private FP attackDelay = 1f;
    private FP attackDelayCurrent = 1f;

    // Use this for initialization
    public override void OnSyncedStart ()
    {
        // Store a direction so we can make the local player always play on the same side and create the second player on the other side
        if (owner.Id == localOwner.Id)
        {
            direction = 1;
        }
        else
        {
            direction = -1;
        }

        textMesh = GetComponentInChildren<TextMesh>();
        GetComponentInChildren <Transform> ().localPosition = new Vector3(-2.3f, 0f, direction);
        StateTracker.AddTracking(boxData);
    }

    // Update is called once per frame
    public override void OnSyncedUpdate() {

        if (target != null)
        {
            if (attackDelayCurrent <= 0)
            {
                DealDamage(target, boxData.atk);
                attackDelayCurrent = attackDelay;
            }
            else
            {
                attackDelayCurrent -= TrueSyncManager.DeltaTime;
            }
        }

        // Update health and kill object if necessary
        textMesh.text = "HP: " + boxData.currentHp + " / " + boxData.totalHp;

        if (boxData.currentHp <= 0)
        {
            TrueSyncManager.SyncedDestroy(gameObject);
        }
    }

    public void OnSyncedTriggerEnter(TSCollision other)
    {
        Debug.Log("Base: Collision Trigger Entered between " + gameObject.name + ":" + gameObject.GetInstanceID() + " and " + other.collider.name + ":" + other.collider.GetInstanceID());
        GameObject collided = other.collider.gameObject;

        // Check ownership of the collided
        if (collided.tag == "Unit")
        {
            if (collided.GetComponent<UnitBehavior>().owner.Id != owner.Id)
            {
                Debug.Log("Base: New Target Aquired (" + collided.GetComponent<UnitBehavior>().owner.Id + owner.Id + ")");
                target = collided;
            }
        }

    }

    public void SetData(BoxData stats)
    {
        boxData = new BoxData(stats.totalHp, stats.atk, stats.range);
        boxData.SetStateTracking();
    }

    public void DealDamage(GameObject target, int damage)
    {
        if (target.tag == "Unit")
        {
            Debug.Log("Attacking " + target.name + ":" + target.GetInstanceID());
            target.GetComponent<UnitBehavior>().ReceiveDamage(damage);
        }
    }

    public void ReceiveDamage (int damage)
    {
        boxData.currentHp -= damage;
    }
}
