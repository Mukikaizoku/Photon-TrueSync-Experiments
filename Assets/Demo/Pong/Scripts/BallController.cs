using TrueSync;
using UnityEngine;

/**
* @brief Manages ball's behavior.
**/
public class BallController : TrueSyncBehaviour {

    /**
    * @brief Max allowed movement in X axis.
    **/
    public int maxX;
    /**
    * @brief Max allowed movement in Y axis.
    **/
    public int maxY;

    /**
    * @brief Movement speed in X axis.
    **/
    [AddTracking]
    public float speedX;
    /**
    * @brief Movement speed in Y axis.
    **/
    [AddTracking]
    public float speedY;

    /**
    * @brief Controlled {@link TSRigidBody} of the ball.
    **/
    private TSRigidBody2D tsRigidBody;

    /**
    * @brief Initial setup when game is started.
    **/
    public override void OnSyncedStart() {
        StateTracker.AddTracking(this);

        tsRigidBody = GetComponent<TSRigidBody2D>();
    }

    /**
    * @brief Updates ball's position.
    **/
    public override void OnSyncedUpdate() {
        TSVector2 currentPosition = tsRigidBody.position;

        currentPosition.x += speedX;
        currentPosition.y += speedY;

        tsRigidBody.position = currentPosition;
    }

    /**
    * @brief Check which gameobject the ball has collided and changes the movement accordingly.
    **/
    public void OnSyncedTriggerEnter(TSCollision2D other) {
        // if hits horizontal colliders then inverse X axis movement
        if (other.gameObject.tag == "GroundHor") {
            speedX *= -1;
        // if hits vertical colliders then inverse Y axis movement
        } else if (other.gameObject.tag == "GroundVer") {
            // As the ball is moving down and hits the collider then the player on top side should score
            if (speedY < 0) {
                CallPaddleScore(true);
            // As the ball is moving up and hits the collider then the player on bottom side should score
            } else {
                CallPaddleScore(false);
            }

            speedY *= -1;
        // if hits a paddle then should also inverse Y axis movement
        } else {
            // Check to avoid movement change when the ball hits the paddle by its back
            if (speedY * tsRigidBody.position.y > 0) {
                speedY *= -1;
            }
        }
    }

    /**
    * @brief Call the {@link PaddleController#Score} function on a PaddleController.
    * 
    * @param isTopSide Indicates which paddle should be used
    **/
    private void CallPaddleScore(bool isTopSide) {
        if (PaddleController.paddlesBySide.ContainsKey(isTopSide)) {
            PaddleController.paddlesBySide[isTopSide].Score();
        }
    }

}