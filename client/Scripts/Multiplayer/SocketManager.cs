using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json.Linq;

public class SocketManager : MonoBehaviour
{
    WebSocket socket;
    public GameObject player;
    public GameObject playerBody;
    public GameObject playerRotBody;
    public PlayerData playerData;
    public JObject allPlayersJSON;
    public JArray playerIDArray;

    public List<Transform> playerObjects = new List<Transform>();

    public GameObject otherPlayerPrefab;

    public string deleteObject = "";
    public string serverComandString = "";

    public Transform multiplayerSpawnPoint;

    //Package URL for WebSocket Sharp for Unity WebSocket
    string PackageURL = "https://github.com/sta/websocket-sharp";

    // Start is called before the first frame update
    void Start()
    {

        socket = new WebSocket("ws://localhost:8080");
        //socket = new WebSocket("ws://Your-Cloud-Server-URL");
        socket.Connect();

        //WebSocket onMessage function
        socket.OnMessage += (sender, e) =>
        {

            //Notify player/client that another player has left the server
            if( e.Data.Contains("Closed:") ) {

                //Remove tag as to get the id of player who is to be removed
                string strOne = e.Data.Replace("Closed:", "");

                //Set string/flag needed for deleting other player game object from scene
                deleteObject = strOne;
            }

            //If received data is type text...
            if (e.IsText)
            {
                //Debug.Log("IsText");
                JObject jsonObj = JObject.Parse(e.Data);
                
                if (jsonObj["serverCommand"] != null)
                {
                    Debug.Log("Received Server Command");
                    Debug.Log(jsonObj);
                    //Set server command String
                    serverComandString = jsonObj["serverCommand"].ToString();
                    return;
                }

                //Get All players connected to server data
                if (jsonObj["type"] != null)
                {
                    //Set all player data json (from server)
                    allPlayersJSON = jsonObj;
                    return;
                }

                //Get Initial Data server ID data (From intial serverhandshake
                if (jsonObj["id"] != null)
                {
                    Debug.Log("ID RECIEVED!");
                    
                    //Convert Intial player data Json (from server) to Player data object
                    PlayerData tempPlayerData = JsonUtility.FromJson<PlayerData>(e.Data);
                    playerData = tempPlayerData;
                    return;
                }
                
            }

            //If received data is type binary
            if (e.IsBinary)
            {
                Debug.Log(JsonUtility.ToJson(playerData));
                Debug.Log("IsBinary");
                Debug.Log(e.RawData);
            }

        };

        //If server connection closes (not client originated)
        socket.OnClose += (sender, e) =>
        {
            Debug.Log(e.Code);
            Debug.Log(e.Reason);
            Debug.Log("Connection Closed!");
        };
    }

    // Update is called once per frame
    void Update()
    {
        if (socket == null)
        {
            return;
        }

        //Check if allPlayersJSON is valid (meaning it has data)
        if (allPlayersJSON != null)
        {
            //Convert all players player IDs Data to array object (using to easily loop over available players)
            JArray Jarr = JArray.FromObject(allPlayersJSON["playerIDs"]);

            //Iterate over all player server data
            foreach (string playerID in Jarr)
            {

                //If found item does not match current player character (is other player), update other player data
                if (playerData.id != playerID)
                {
                    //Attempt to grab other player representative gameobject in scene
                    GameObject otherplayer = GameObject.Find(playerID);

                    if (allPlayersJSON[playerID]["position"] == null)
                        break;

                    if (allPlayersJSON[playerID]["position"]["xPos"] == null)
                        break;

                    Debug.Log(allPlayersJSON[playerID]["position"]);

                    //Grab data of other player from allPlayersJSON object
                    float xPos = float.Parse(allPlayersJSON[playerID]["position"]["xPos"].ToString());
                    float yPos = float.Parse(allPlayersJSON[playerID]["position"]["yPos"].ToString());
                    float zPos = float.Parse(allPlayersJSON[playerID]["position"]["zPos"].ToString());
                    float xRot = float.Parse(allPlayersJSON[playerID]["position"]["xRot"].ToString());
                    float yRot = float.Parse(allPlayersJSON[playerID]["position"]["yRot"].ToString());
                    float zRot = float.Parse(allPlayersJSON[playerID]["position"]["zRot"].ToString());
                    double timestamp = double.Parse(allPlayersJSON[playerID]["position"]["timestamp"].ToString());

                    if (otherplayer != null)
                    {
                        if (allPlayersJSON[playerID] != null) {
                            
                            //Move Character using Other Player Movment Logic (Smooth - with a minor Stutter)
                            otherplayer.GetComponent<OtherPlayer>().targetPosition = new Vector3(xPos, yPos, zPos);
                            otherplayer.GetComponent<OtherPlayer>().timestamp = timestamp;
                            otherplayer.GetComponent<OtherPlayer>().targetRotation = Quaternion.Euler(xRot, yRot, zRot);

                        } else
                        {
                            //In case other player object exists but no longer exists in allPlayersJSON, remove from wolrd
                            Destroy(otherplayer);
                        }
                    }
                    else
                    {
                        //If other player is found in allPlayersJSON but does not have a game object in world, create it
                        GameObject oPGameObject = Instantiate(otherPlayerPrefab, multiplayerSpawnPoint.position, Quaternion.identity);
                        oPGameObject.name = playerID;
                    }
                }

            }
        }

        //If player is correctly configured, begin sending player data to server
        if (player != null && playerData.id != "")
        {
            //Grab player current position and rotation data
            playerData.xPos = playerBody.transform.position.x;
            playerData.yPos = playerBody.transform.position.y;
            playerData.zPos = playerBody.transform.position.z;
            playerData.xRot = playerRotBody.transform.rotation.eulerAngles.x;
            playerData.yRot = playerRotBody.transform.rotation.eulerAngles.y;
            playerData.zRot = playerRotBody.transform.rotation.eulerAngles.z;

            System.DateTime epochStart =  new System.DateTime(1970, 1, 1, 8, 0, 0, System.DateTimeKind.Utc);
            double timestamp = (System.DateTime.UtcNow - epochStart).TotalSeconds;
            //Debug.Log(timestamp);
            playerData.timestamp = timestamp;

            string playerDataJSON = JsonUtility.ToJson(playerData);
            socket.Send(playerDataJSON);
        }

        //If the delete object string is non-empty, other player game object needs to be removed
        if (deleteObject != "")
        {
            //Co-routine to have slight time delay before delete
            StartCoroutine( DeleteObject(deleteObject));
            //Reset string/flag
            deleteObject = "";
        }

        //If the serverCommand object string is non-empty, respond to server Command...
        if (serverComandString != "")
        {

            if (serverComandString == "Start")
            {
                Debug.Log("Server Did a Start");
            }
            //Reset string/flag
            serverComandString = "";
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            string messageJSON = "{\"message\": \"Some Message From Client\"}";
            socket.Send(messageJSON);
        }
    }

    private void OnDestroy()
    {
        //Close socket when exiting application
        socket.Close();
    }

    IEnumerator DeleteObject(string objectName)
    {
        //Wait a few moments before removing player from game
        yield return new WaitForSeconds(1.0f);
        Destroy(GameObject.Find(objectName));
    }

}
