using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 using UnityEngine.UI;

 [RequireComponent(typeof(Button))]
 public class CallPV : MonoBehaviour {

    public Transform PlayVestedPackage;
    public GameObject summaryObj;
    private PlayVested script;
    private string userID = "";

    // Use this for initialization
    void Start () {
        if (!PlayVestedPackage) {
            Debug.LogError("Error: need to set a reference to the PlayVested prefab first");
            return;
        }

        Transform trans = Instantiate(PlayVestedPackage, new Vector3(0, 0, 0), Quaternion.identity);
        this.script = trans.GetComponentInChildren<PlayVested>();
        if (this.script) {
            this.script.init("5c00a8b7f9bf974de030b42a"); // This is a the unique ID for the game
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

    // callback when the user is successfully created
    private void recordUserCB(string userID) {
        Debug.Log("Created user: " + userID);
        this.userID = userID;
        // GetComponent<Button>().interactable = true;

        // show the button to view the summary for the game
        this.summaryObj.SetActive(true);
    }

    public void recordEarningCB(bool success) {
        Debug.Log("Your purchase has been added to your PlayVested total for this month!");
    }

    public void handleIAP() {
        if (this.script) {
            if (this.userID == "") {
                // disable the IAP button until the callback fires
                // TODO: this isn't disabling the button properly
                // GetComponent<Button>().interactable = false;
                this.script.createUser(recordUserCB);
            } else {
                Debug.Log("Making a donation...");
                float amount = Random.Range(0.99f, 9.99f);
                this.script.reportEarning(amount, recordEarningCB);
            }
        }
    }

    public void handleSummary() {
        if (this.script) {
            this.script.showSummary();
        }
    }
}
