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
    public GameObject popupObj;
    public GameObject donationBannerObj;
    public Button iapButton;
    public GameObject quitObj;

    private PlayVested script;
    private string playerID = "";
    private string charityName = "";
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
                    this.recordPlayerCB(split[1], "");
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
        if (this.popupObj) {
            this.popupObj.SetActive(false);
        } else {
            Debug.LogError("Error: set the pop up reference");
            this.OnQuit();
            return;
        }

        // make sure the banner is hidden
        if (this.donationBannerObj) {
            this.donationBannerObj.SetActive(false);
        } else {
            Debug.LogError("Error: set the banner reference");
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

        this.unpauseGame();
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

    private IEnumerator showBanner(string msgText) {
        if (!this.donationBannerObj) {
            yield break;
        }

        // update the text on the banner
        Text msgComponent = this.donationBannerObj.GetComponentInChildren<Text>();
        if (msgComponent) {
            msgComponent.text = msgText;
        }

        // show the banner
        this.donationBannerObj.SetActive(true);

        // leave the banner up to for a short time
        float secToWait = 3.0f;
        yield return new WaitForSeconds(secToWait);

        // then close it
        this.donationBannerObj.SetActive(false);
    }

    // callback when the player is successfully created
    private void recordPlayerCB(string playerID, string charityName) {
        Debug.Log("Created player: " + playerID);
        this.playerID = playerID;

        // show the button to view the summary for the game if the player is valid
        this.summaryObj.SetActive(this.script.isValid(this.playerID));

        if (charityName != "") {
            // show a banner that their selection was made
            string msgText = "Thank you for supporting " + charityName;
            this.charityName = charityName;
            StartCoroutine(this.showBanner(msgText));
        }
    }

    public void recordEarningCB(double amountRecorded) {
        // show a banner message to remind them they are supporting a charity
        float donationAmount = (float)Math.Round((double)amountRecorded, 2);
        string charityName = (this.charityName != "" ? this.charityName : "your selected charity");
        string msgText = "You added $" + donationAmount + " to the donation " + charityName + " will get this month!";
        StartCoroutine(this.showBanner(msgText));
    }

    public void closePopupMessage() {
        if (this.popupObj) {
            this.popupObj.SetActive(false);
        }

        if (this.playerID == "") {
            this.script.createPlayer(this.recordPlayerCB, this.unpauseGame);
        } else {
            this.unpauseGame();
        }
    }

    public void handleStoreOpen() {
        this.pauseGame();

        // disable the IAP button until the store closes
        if (this.iapButton) {
            this.iapButton.interactable = false;
        }

        this.storeObj.SetActive(true);
    }

    public void handleStoreClose() {
        if (this.iapButton) {
            this.iapButton.interactable = true;
        }

        this.storeObj.SetActive(false);

        this.unpauseGame();
    }

    public void handleIAP(float amount) {
        Debug.Log("Making a purchase of $" + amount);

        if (this.script.isValid(this.playerID)) {
            // if there is a valid PV player, call the web hook
            float donationAmount = amount * DONATION_PCT;
            this.script.reportEarning(donationAmount, this.recordEarningCB);
        }

        // open the pop-up confirmation
        this.popupObj.SetActive(true);

        // close the store screen
        this.handleStoreClose();

        // keep the game paused while the pop-up is active
        this.pauseGame();
    }

    public void handleSummary() {
        QueryTotalParams queryParams = new QueryTotalParams(devID, gameID);
        queryParams.previousWeeks = 1;

        this.pauseGame();
        this.script.showSummary(queryParams);
    }
}
