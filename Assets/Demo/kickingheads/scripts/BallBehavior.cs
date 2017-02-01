using UnityEngine;
using TrueSync;

/**
* @brief Manages ball's behavior.
**/
public class BallBehavior : TrueSyncBehaviour {

    /**
    * @brief Controlled {@link TSRigidBody} of the ball.
    **/
    TSRigidBody2D tsRigidBody;

    /**
    * @brief AudioSource for sound effects.
    **/
    AudioSource audioSource;

    /**
    * @brief Initial setup.
    **/
    void Start() {
        audioSource = GetComponent<AudioSource>();
        tsRigidBody = GetComponent<TSRigidBody2D>();
    }

    /**
    * @brief Calls {@link #ResetProperties} when game is started.
    **/
    public override void OnSyncedStart() {
        ResetProperties();
    }

    public override void OnSyncedUpdate() {
        tsRigidBody.GetComponent<TSTransform2D>().rotation -= 5;
    }

    /**
    * @brief When the ball hits a player a force in Y axis is applied and also plays a sound when the collision is not with a goal.
    **/
    public void OnSyncedCollisionEnter(TSCollision2D other) {
        if (other.gameObject.tag == "player") {
            tsRigidBody.AddForce(new TSVector2(0, -500));
        }
        if (other.gameObject.tag != "goal")
        {
            audioSource.Play();
        }
    }

    /**
    * @brief It called when a goal is scored and executes the method {@link #ResetProperties}. 
    **/
    void GoalScored() {
        ResetProperties();
    }

    /**
    * @brief Places the ball in its initial position and sets to zero its linear velocity.
    **/
    public void ResetProperties() {
        tsRigidBody.position = new TSVector2(0, 5);
        tsRigidBody.velocity = TSVector2.zero;
    }

}