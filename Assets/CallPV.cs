using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

 [RequireComponent(typeof(Button))]
 public class CallPV : MonoBehaviour {

    public Transform PlayVestedPackage;
    public GameObject storeObj;
    public GameObject summaryObj;
    public Text popupMessage;
    public Button iapButton;
    public GameObject quitObj;

    private PlayVested script;
    private string playerID = "";
    private string devID =  "5bfe194f4de8110016de4343"; // This is the unique ID for the developer
    private string gameID = "5bfe194f4de8110016de4347"; // This is a the unique ID for the game
    const string SAVE_DIR = "./Assets";
    const string SAVE_FILE = SAVE_DIR + "/save.txt";
    const float DONATION_PCT = 0.1f;

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

    // TODO: wire this up to a save game button
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
            this.OnQuit();
            return;
        }

        // make sure the store starts hidden
        if (this.storeObj) {
            this.storeObj.SetActive(false);
        } else {
            Debug.LogError("Error: need to set a reference to the store object first");
            this.OnQuit();
            return;
        }

        // make sure the summary button is hidden until we have a valid player ID
        if (this.summaryObj) {
            this.summaryObj.SetActive(false);
        } else {
            Debug.LogError("Error: set the summary object reference");
            this.OnQuit();
            return;
        }

        // make sure the pop up message is hidden
        if (this.popupMessage) {
            this.popupMessage.transform.parent.gameObject.SetActive(false);
        } else {
            Debug.LogError("Error: set the pop up reference");
            this.OnQuit();
            return;
        }

        // try pulling IDs from a file on disk
        this.LoadSaveData();

        Transform trans = Instantiate(PlayVestedPackage, new Vector3(0, 0, 0), Quaternion.identity);
        this.script = trans.GetComponentInChildren<PlayVested>();
        if (this.script) {
            this.script.init(this.devID, this.gameID, this.playerID);
        } else {
            Debug.LogError("Error finding script object");
            this.OnQuit();
            return;
        }

        if (!Application.isEditor && this.quitObj) {
            this.quitObj.SetActive(true);
        }
    }

    void OnDestroy() {
        if (this.script) {
            this.script.shutdown();
        }
        // this.WriteSaveData();
    }

    public void OnQuit() {
        Application.Quit();
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
    }

    // callback when the player is successfully created
    private void recordPlayerCB(string playerID) {
        Debug.Log("Created player: " + playerID);
        this.playerID = playerID;

        // show the button to view the summary for the game
        this.summaryObj.SetActive(this.script.isValid(this.playerID));
    }

    public void recordEarningCB(double amountRecorded) {
        string msg = "Your purchase was successful";
        int desiredHeight = 60;
        if (this.script.isValid(this.playerID)) {
            float donationAmount = (float)Math.Round((double)amountRecorded, 2);
            msg += "\n\nYou added $" + donationAmount + " to the donation your selected charity will get this month!";
            desiredHeight += 20;
        }

        Debug.Log(msg);
        if (this.popupMessage) {
            this.popupMessage.text = msg;
            this.popupMessage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, desiredHeight);
            this.popupMessage.transform.parent.gameObject.SetActive(true);
        }
    }

    public void closePopupMessage() {
        if (this.popupMessage) {
            this.popupMessage.transform.parent.gameObject.SetActive(false);
        }
    }

    public void handleStoreOpen() {
        this.pauseGame();
        if (this.playerID == "") {
            // disable the IAP button until the callback fires
            if (this.iapButton) {
                this.iapButton.interactable = false;
            }
            this.script.createPlayer(this.recordPlayerCB, this.createPlayerCleanup);
        }

        this.storeObj.SetActive(true);
    }

    public void handleStoreClose() {
        this.storeObj.SetActive(false);
        unpauseGame();
    }

    public void handleIAP(float amount) {
        Debug.Log("Making a purchase of $" + amount);

        if (this.script.isValid(this.playerID)) {
            // if there is a valid PV player, call the web hook
            float donationAmount = amount * DONATION_PCT;
            this.script.reportEarning(donationAmount, this.recordEarningCB);
        } else {
            // otherwise just report that it was a successful purchase
            this.recordEarningCB(0);
        }

        this.handleStoreClose();
    }

    public void handleSummary() {
        QueryTotalParams queryParams = new QueryTotalParams(devID, gameID);
        queryParams.previousWeeks = 1;

        this.pauseGame();
        this.script.showSummary(queryParams, null, this.unpauseGame);
    }
}
