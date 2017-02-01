using System;
using UnityEngine;

namespace TrueSync {

    /**
     *  @brief Represents each player's behaviour simulated on every machine connected to the game.
     */
    public abstract class TrueSyncBehaviour : MonoBehaviour, ITrueSyncBehaviourGamePlay, ITrueSyncBehaviourCallbacks {

        /**
         * @brief Number of players connected to the game.
         **/
        [HideInInspector]
        public int numberOfPlayers;

        /**
         *  @brief Index of the owner at initial players list.
         */
		public int ownerIndex = -1;

        /**
         *  @brief Basic info about the owner of this behaviour.
         */
        [HideInInspector]
        public TSPlayerInfo owner;

        /**
         *  @brief Basic info about the local player.
         */
        [HideInInspector]
        public TSPlayerInfo localOwner;

        private TSTransform _tsTransform;

        /**
         *  @brief Returns the {@link TSTransform} attached.
         */
        public TSTransform tsTransform {
            get {
                if (_tsTransform == null) {
                    _tsTransform = this.GetComponent<TSTransform>();
                }

                return _tsTransform;
            }
        }

        /**
         * @brief It is not called for instances of {@link TrueSyncBehaviour}.
         **/
        public void SetGameInfo(TSPlayerInfo localOwner, int numberOfPlayers) {}

        /**
         * @brief Called once when the object becomes active.
         **/
        public virtual void OnSyncedStart() { }

        /**
         * @brief Called when the game has paused.
         **/
        public virtual void OnGamePaused() { }

        /**
         * @brief Called when the game has unpaused.
         **/
        public virtual void OnGameUnPaused() { }

        /**
         * @brief Called when the game has ended.
         **/
        public virtual void OnGameEnded() { }

        /**
         *  @brief Called before {@link #OnSyncedUpdate}.
         *  
         *  Called once every lockstepped frame.
         */
        public virtual void OnPreSyncedUpdate() { }

        /**
         *  @brief Game updates goes here.
         *  
         *  Called once every lockstepped frame.
         */
        public virtual void OnSyncedUpdate() { }

        /**
         *  @brief Get local player data.
         *  
         *  Called once every lockstepped frame.
         */
        public virtual void OnSyncedInput() { }

        /**
         * @brief Callback called when a player get disconnected.
         **/
        public virtual void OnPlayerDisconnection(int playerId) {}

    }

}