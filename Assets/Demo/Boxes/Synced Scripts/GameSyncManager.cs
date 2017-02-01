using UnityEngine;
using TrueSync;
using System.Collections;
using UnityEngine.UI;

// The GameSyncManager runs the behaviour of a single player
public class GameSyncManager : TrueSyncBehaviour {

    // INPUT KEYS
    enum InputKey : byte
    {
        None,
        LeftMelee = 10,
        LeftRanged,
        RightMelee,
        RightRanged
    }

    // Fields
    [Header("Prefabs")]
    public GameObject basePrefab;
    public GameObject unitPrefab;
    public GameObject unit2Prefab;

    [Header("UI Items")]
    public GameObject buttonPrefab;

    [Header("Game Variables")]
    public float meleeDelay;
    public float rangedDelay;

    // Private Fields
    private TSVector leftSpawn, rightSpawn;
    private bool leftMeleeKey;
    private bool leftRangedKey;
    private bool rightMeleeKey;
    private bool rightRangedKey;
    private Canvas canvas;
    private Button buttonLeftMelee, buttonLeftRanged, buttonRightMelee, buttonRightRanged;
    private int positionFactor;

    
    // Setup the player
    public override void OnSyncedStart() {
        Debug.Log("Player Number " + owner.Id + " initializing. ");


        // Store a positionFactor so we can make the local player always play on the same side and create the second player on the other side
        if (owner.Id != localOwner.Id)
        {
            positionFactor = -1;
        } else
        {
            positionFactor = 1;
        }

        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.Log("Canvas not found.");
        }

        leftSpawn = new TSVector(-13, 2, -10.25f);
        rightSpawn = new TSVector(13, 2, -10.25f);


        // Create initial player bases and UI interface
        CreateBases();
        SetupButtons();
    }


    // Create UI buttons for the player
    private void SetupButtons()
    {
        // Only create the interface for the local player
        if (owner.Id != localOwner.Id)
        {
            return;
        }


        GameObject newButton = Instantiate(buttonPrefab, canvas.gameObject.transform) as GameObject;
        if (newButton == null)
        {
            Debug.Log("Could not instantiate new button");
            return;
        }
        newButton.transform.localPosition = new Vector3(-220, -220, 0);
        buttonLeftMelee = newButton.GetComponent<Button>();
        ButtonTimer buttonTimer = newButton.GetComponent<ButtonTimer>();
        buttonLeftMelee.onClick.AddListener(delegate { buttonTimer.DisableButtonAndStartTimer(meleeDelay); });
        buttonLeftMelee.onClick.AddListener(delegate { CreateLeftMeleeUnit(); });


        newButton = Instantiate(buttonPrefab, canvas.gameObject.transform) as GameObject;
        if (newButton == null)
        {
            Debug.Log("Could not instantiate new button");
            return;
        }
        newButton.transform.localPosition = new Vector3(-125, -220, 0);
        buttonLeftRanged = newButton.GetComponent<Button>();
        ButtonTimer buttonTimer2 = newButton.GetComponent<ButtonTimer>();
        buttonLeftRanged.onClick.AddListener(delegate { buttonTimer2.DisableButtonAndStartTimer(rangedDelay); });
        buttonLeftRanged.onClick.AddListener(delegate { CreateLeftRangedUnit(); });
        buttonLeftRanged.GetComponentInChildren<Text>().text = "Left Ranged";


        newButton = Instantiate(buttonPrefab, canvas.gameObject.transform) as GameObject;
        if (newButton == null)
        {
            Debug.Log("Could not instantiate new button");
            return;
        }
        newButton.transform.localPosition = new Vector3(125, -220, 0);
        buttonRightMelee = newButton.GetComponent<Button>();
        ButtonTimer buttonTimer3 = newButton.GetComponent<ButtonTimer>();
        buttonRightMelee.onClick.AddListener(delegate { buttonTimer3.DisableButtonAndStartTimer(meleeDelay); });
        buttonRightMelee.onClick.AddListener(delegate { CreateRightMeleeUnit(); });
        buttonRightMelee.GetComponentInChildren<Text>().text = "Right Melee";


        newButton = Instantiate(buttonPrefab, canvas.gameObject.transform) as GameObject;
        if (newButton == null)
        {
            Debug.Log("Could not instantiate new button");
            return;
        }
        newButton.transform.localPosition = new Vector3(220, -220, 0);
        buttonRightRanged = newButton.GetComponent<Button>();
        ButtonTimer buttonTimer4 = newButton.GetComponent<ButtonTimer>();
        buttonRightRanged.onClick.AddListener(delegate { buttonTimer4.DisableButtonAndStartTimer(rangedDelay); });
        buttonRightRanged.onClick.AddListener(delegate { CreateRightRangedUnit(); });
        buttonRightRanged.GetComponentInChildren<Text>().text = "Right Ranged";
    }


    // Collect any UI input that might of occured
    public override void OnSyncedInput()
    {
        TrueSyncInput.SetInt((byte)InputKey.LeftMelee, (leftMeleeKey) ? 1 : 0);
        leftMeleeKey = false;
        TrueSyncInput.SetInt((byte)InputKey.LeftRanged, (leftRangedKey) ? 1 : 0);
        leftRangedKey = false;
        TrueSyncInput.SetInt((byte)InputKey.RightMelee, (rightMeleeKey) ? 1 : 0);
        rightMeleeKey = false;
        TrueSyncInput.SetInt((byte)InputKey.RightRanged, (rightRangedKey) ? 1 : 0);
        rightRangedKey = false;
    }


    // Perform any inputs that were performed since last update
    public override void OnSyncedUpdate()
    {
        if (TrueSyncInput.GetInt((byte)InputKey.LeftMelee) == 1)
        {
            CreateUnit(1, 1);
        }
        if (TrueSyncInput.GetInt((byte)InputKey.LeftRanged) == 1)
        {
            CreateUnit(2, 1);
        }
        if (TrueSyncInput.GetInt((byte)InputKey.RightMelee) == 1)
        {
            CreateUnit(1, 2);
        }
        if (TrueSyncInput.GetInt((byte)InputKey.RightRanged) == 1)
        {
            CreateUnit(2, 2);
        }
    }
    

    // Create the initial bases for the player
    void CreateBases()
    {
        Debug.Log("Creating bases. . .");
        BoxData baseStats = new BoxData(100, 10, 3f);

        GameObject castle = TrueSyncManager.SyncedInstantiate(this.basePrefab, new TSVector(0, 2, -18.75f*positionFactor), TSQuaternion.identity);
        castle.GetComponent<BoxBehaviour>().SetData(baseStats);
        castle.GetComponent<BoxBehaviour>().owner = owner;

        castle = TrueSyncManager.SyncedInstantiate(this.basePrefab, new TSVector(-13, 2, -12.25f * positionFactor), TSQuaternion.identity);
        castle.GetComponent<BoxBehaviour>().SetData(baseStats);
        castle.GetComponent<BoxBehaviour>().owner = owner;

        castle = TrueSyncManager.SyncedInstantiate(this.basePrefab, new TSVector(13, 2, -12.25f * positionFactor), TSQuaternion.identity);
        castle.GetComponent<BoxBehaviour>().SetData(baseStats);
        castle.GetComponent<BoxBehaviour>().owner = owner;
    }
    

    // Track button presses
    public void CreateLeftMeleeUnit ()
    {
        leftMeleeKey = true;
    }

    public void CreateLeftRangedUnit ()
    {
        leftRangedKey = true;
    }

    public void CreateRightMeleeUnit()
    {
        rightMeleeKey = true;
    }

    public void CreateRightRangedUnit()
    {
        rightRangedKey = true;
    }


    // Create a unit of a type at the left or right flank
    private void CreateUnit (int type, int positionCode)
    {
        TSVector position;
        GameObject prefab;
        int attack = 1, hp = 1;
        FP range = 0f;


        switch (type)
        {
            case 1:     // Melee Unit
                prefab = this.unitPrefab;
                attack = 10;
                hp = 30;
                range = 0f;
                break;
            case 2:     // Ranged Unit
                prefab = this.unit2Prefab;
                attack = 5;
                hp = 20;
                range = 3f;
                break;
            default:
                Debug.Log("Incorrect type specified by CreateUnit");
                return;
        }
        UnitData stats = new UnitData(hp, attack, range);


        switch (positionCode)
        {
            case 1:     // Left flank
                position = leftSpawn;
                break;
            case 2:     // Right flank
                position = rightSpawn;
                break;
            default:
                Debug.Log("Incorrect positionCode specified by CreateUnit");
                return;
        }

        position.x *= positionFactor;
        position.z *= positionFactor;
        UnitBehavior unitBehavior = TrueSyncManager.SyncedInstantiate(prefab, position, TSQuaternion.identity).GetComponent<UnitBehavior>();
        unitBehavior.owner = owner;
        unitBehavior.SetData(stats);
    }


    /**
    * @brief Logs a text when game is paused.
    **/
    public override void OnGamePaused() {
        Debug.Log("Game Paused");
    }

    /**
    * @brief Logs a text when game is unpaused.
    **/
    public override void OnGameUnPaused() {
        Debug.Log("Game UnPaused");
    }

    /**
    * @brief Logs a text when game is ended.
    **/
    public override void OnGameEnded() {
        Debug.Log("Game Ended");
    }

    /**
    * @brief When a player get disconnected all objects belonging to him are destroyed.
    **/
    public override void OnPlayerDisconnection(int playerId) {
        TrueSyncManager.RemovePlayer(playerId);
    }

}