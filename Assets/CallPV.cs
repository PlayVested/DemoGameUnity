using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

 [RequireComponent(typeof(Button))]
 public class CallPV : MonoBehaviour {

    public Transform PlayVestedPackage;
    public GameObject summaryObj;
    public Button iapButton;

    private PlayVested script;
    private string playerID = "";

    const string GAME_ID = "5bfe194f4de8110016de4347"; // This is a the unique ID for the game

    // Use this for initialization
    void Start () {
        if (!PlayVestedPackage) {
            Debug.LogError("Error: need to set a reference to the PlayVested prefab first");
            return;
        }

        Transform trans = Instantiate(PlayVestedPackage, new Vector3(0, 0, 0), Quaternion.identity);
        this.script = trans.GetComponentInChildren<PlayVested>();
        if (this.script) {
            this.script.init(GAME_ID);
        } else {
            Debug.LogError("Error finding script object");
        }

        // make sure the summary button is hidden until we have a valid player ID
        if (this.summaryObj) {
            this.summaryObj.SetActive(false);
        } else {
            Debug.LogError("Error: set the summary object reference");
        }
    }

    void OnDestroy() {
        if (this.script) {
            this.script.shutdown();
        }
    }

    private void pauseGame() {
        Time.timeScale = 0;
    }

    private void unpauseGame() {
        Time.timeScale = 1;
    }

    private void createPlayerCleanup() {
        if (this.iapButton) {
            this.iapButton.interactable = true;
        }
        unpauseGame();
    }

    // callback when the player is successfully created
    private void recordPlayerCB(string playerID) {
        Debug.Log("Created player: " + playerID);
        this.playerID = playerID;

        // show the button to view the summary for the game
        this.summaryObj.SetActive(true);

        // call this to finish the cleanup
        this.createPlayerCleanup();
    }

    public void recordEarningCB(float amountRecorded) {
        Debug.Log("Your purchase of " + amountRecorded + " has been added to your PlayVested total for this month!");
    }

    public void handleIAP() {
        if (this.script) {
            this.pauseGame();
            if (this.playerID == "") {
                // disable the IAP button until the callback fires
                if (this.iapButton) {
                    this.iapButton.interactable = false;
                }
                this.script.createPlayer(this.recordPlayerCB, this.createPlayerCleanup);
            } else {
                Debug.Log("Making a donation...");
                float amount = Random.Range(0.99f, 9.99f);
                this.script.reportEarning(amount, this.recordEarningCB, this.unpauseGame);
            }
        }
    }

    public void handleSummary() {
        if (this.script) {
            QueryTotalParams queryParams = new QueryTotalParams(GAME_ID);
            queryParams.previousWeeks = 1;

            this.pauseGame();
            this.script.showSummary(queryParams, null, this.unpauseGame);
        }
    }
}
