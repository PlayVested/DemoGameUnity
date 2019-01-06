using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

 [RequireComponent(typeof(Button))]
 public class CallPV : MonoBehaviour {

    public Transform PlayVestedPackage;
    public GameObject summaryObj;
    public Text popupMessage;
    public Button iapButton;

    private PlayVested script;
    private string playerID = "";
    private string devID =  "5bfe194f4de8110016de4343"; // This is the unique ID for the developer
    private string gameID = "5bfe194f4de8110016de4347"; // This is a the unique ID for the game
    const string SAVE_DIR = "./Assets";
    const string SAVE_FILE = SAVE_DIR + "/save.txt";

    private void LoadSaveData() {
        if (File.Exists(SAVE_FILE) && new FileInfo(SAVE_FILE).Length != 0) {
            StreamReader reader = new StreamReader(SAVE_FILE);
            string line = null;
            char[] delimiter = {':'};
            while ((line = reader.ReadLine()) != null) {
                string[] split = line.Split(delimiter);
                if (split[0] == "dev") {
                    this.devID = split[1];
                } else if (split[0] == "game") {
                    this.gameID = split[1];
                } else if (split[0] == "player") {
                    this.recordPlayerCB(split[1]);
                }
            }
            reader.Close();
        }
    }

    private void WriteSaveData() {
        // make sure the output dir exists
        Directory.CreateDirectory(SAVE_DIR);

        // write the IDs
        StreamWriter writer = new StreamWriter(SAVE_FILE, false);
        writer.WriteLine("dev:" + this.devID);
        writer.WriteLine("game:" + this.gameID);
        writer.WriteLine("player:" + this.playerID);
        writer.Close();
    }

    // Use this for initialization
    void Start () {
        if (!PlayVestedPackage) {
            Debug.LogError("Error: need to set a reference to the PlayVested prefab first");
            return;
        }

        // make sure the summary button is hidden until we have a valid player ID
        if (this.summaryObj) {
            this.summaryObj.SetActive(false);
        } else {
            Debug.LogError("Error: set the summary object reference");
        }

        // try pulling IDs from a file on disk
        this.LoadSaveData();

        Transform trans = Instantiate(PlayVestedPackage, new Vector3(0, 0, 0), Quaternion.identity);
        this.script = trans.GetComponentInChildren<PlayVested>();
        if (this.script) {
            this.script.init(this.devID, this.gameID, this.playerID);
        } else {
            Debug.LogError("Error finding script object");
        }
    }

    void OnDestroy() {
        if (this.script) {
            this.script.shutdown();
        }
        this.WriteSaveData();
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
        this.summaryObj.SetActive(this.playerID != "");
    }

    public void recordEarningCB(double amountRecorded) {
        string msg = "Your purchase of $" + amountRecorded + " has been added to your PlayVested total for this month!";
        Debug.Log(msg);
        if (this.popupMessage) {
            this.popupMessage.text = msg;
            this.popupMessage.transform.parent.gameObject.SetActive(true);
        }
    }

    public void closePopupMessage() {
        if (this.popupMessage) {
            this.popupMessage.transform.parent.gameObject.SetActive(false);
        }
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
                float randVal = UnityEngine.Random.Range(0.99f, 9.99f);
                float amount = (float)Math.Round((double)randVal, 2);
                Debug.Log("Making a donation of $" + amount);
                this.script.reportEarning(amount, this.recordEarningCB, this.unpauseGame);
            }
        }
    }

    public void handleSummary() {
        if (this.script) {
            QueryTotalParams queryParams = new QueryTotalParams(devID, gameID);
            queryParams.previousWeeks = 1;

            this.pauseGame();
            this.script.showSummary(queryParams, null, this.unpauseGame);
        }
    }
}
