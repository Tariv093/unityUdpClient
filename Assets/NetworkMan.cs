using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Security.Cryptography;

public class NetworkMan : MonoBehaviour
{
    public GameObject prefab;
    public UdpClient udp;
    public Dictionary<string, GameObject> existingPlayers = new Dictionary<string, GameObject>();
    public List<string> playerIDs;
    public List<string> leavingPlayers;
    private string myClientID;
    // Start is called before the first frame update
    [Serializable]
    public class updateMessage
    {
        public string cmd;

        public float X;
        public float Y;
        public float Z;
    }
    void Start()
    {
        udp = new UdpClient();
      //  3.131.90.180
        udp.Connect("18.217.121.107", 12345);
           updateMessage uMsg = new updateMessage();
        uMsg.cmd = "connect";
        JsonUtility.ToJson(uMsg);
        Byte[] sendBytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(uMsg));
        udp.Send(sendBytes, sendBytes.Length); 
        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1);
        InvokeRepeating("UpdatePosition", 1,1/30);
    }

    void OnDestroy(){
        udp.Dispose();
        
    }


    public enum commands{
        NEW_CLIENT,
        UPDATE,
        DISCONNECT,
        CONFIRMID

    };
    
    [Serializable]
    public class Message{
        public commands cmd;
        public string id;
    }
    
    [Serializable]
    public class Player{
        [Serializable]
        public struct receivedColor{
            public float R;
            public float G;
            public float B;
        }
        public string id;
        public receivedColor color;
        public float posX, posY, posZ;
    }

    [Serializable]
    public class NewPlayer{
        
    }

    [Serializable]
    public class GameState{
        public Player[] players;
    }

    public Message latestMessage;
    public GameState lastestGameState;
    void OnReceived(IAsyncResult result){
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;
        
        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);
        
        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        UnityEngine.Debug.Log("Got this: " + returnData);
        
        latestMessage = JsonUtility.FromJson<Message>(returnData);
        try{
            switch(latestMessage.cmd){
                case commands.NEW_CLIENT:

                    playerIDs.Add(latestMessage.id);
                    UnityEngine.Debug.Log(latestMessage.id);
                    break;
                case commands.UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    //.Log(lastestGameState.players[0].color.R);
                    break;
                case commands.DISCONNECT:
                    leavingPlayers.Add(latestMessage.id);
                    break;
                case commands.CONFIRMID:
                    myClientID = latestMessage.id;
                    break;
                default:
                    UnityEngine.Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e){
           UnityEngine.Debug.Log(e.ToString());
        }
        
        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    void SpawnPlayers()
    {
       // Vector3 pos = new Vector3(UnityEngine.Random.Range(0, 10), UnityEngine.Random.Range(0, 10),0);

        foreach(string s in playerIDs)
        {
          
            
            if(s == null)
            {

            }
            else if(!existingPlayers.ContainsKey(s))
            {
                existingPlayers.Add(s, Instantiate(prefab, new Vector3(0,0,0), Quaternion.identity));
                existingPlayers[s].GetComponent<NetworkID>().id = s;
            }
            
        }
        playerIDs.Clear();
   
    }

    void UpdatePlayers() {

     //  existingPlayers
     for(int i = 0; i < lastestGameState.players.Length; i++)
        {
            if (existingPlayers.ContainsKey(lastestGameState.players[i].id))
            {
           
                existingPlayers[lastestGameState.players[i].id].GetComponent<NetworkID>().color = new Color(lastestGameState.players[i].color.R, lastestGameState.players[i].color.G, lastestGameState.players[i].color.B);
                existingPlayers[lastestGameState.players[i].id].GetComponent<NetworkID>().pos = new Vector3(lastestGameState.players[i].posX, lastestGameState.players[i].posY, lastestGameState.players[i].posZ);

            }
            else
            {
                playerIDs.Add(lastestGameState.players[i].id);
            }
        }

    }

    void DestroyPlayers(){
       // OnDestroy();

        foreach(string s in leavingPlayers)
        {
            if(s == null)
            {

            }
            else if (existingPlayers.ContainsKey(s))
            {
                Destroy(existingPlayers[s]);
                existingPlayers.Remove(s);
            }
        }
        leavingPlayers.Clear();
    }
    
    void HeartBeat(){
        updateMessage uMsg = new updateMessage();
        uMsg.cmd = "heartbeat";
        JsonUtility.ToJson(uMsg);
        Byte[] sendBytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(uMsg));
        udp.Send(sendBytes, sendBytes.Length);
    }
    void UpdatePosition()
    {
        if (myClientID == null)
        { return; }
        if (existingPlayers.ContainsKey(myClientID))
        {
            updateMessage uMsg = new updateMessage();
            uMsg.cmd = "updateposition";
            uMsg.Y = existingPlayers[myClientID].transform.position.y;
            uMsg.X = existingPlayers[myClientID].transform.position.x;
            uMsg.Z = existingPlayers[myClientID].transform.position.z;
            JsonUtility.ToJson(uMsg);
            Byte[] sendBytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(uMsg));
            udp.Send(sendBytes, sendBytes.Length);
            existingPlayers[myClientID].GetComponent<NetworkID>().setBool(true);
        }
        else
        {
            existingPlayers[myClientID].GetComponent<NetworkID>().setBool(false);
        }
    }
    void Update(){
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();


      if(Input.GetKey("w"))
        {
            //use jsons to translate your position to serverside, which will then translate the player in a direction
            //translate
            existingPlayers[myClientID].transform.Translate(new Vector3(0, 1*Time.deltaTime, 0));
        }
      if(Input.GetKey("s"))
        {

        }
        //if (Input.GetKey("A"))
        //    {
        //    Debug.Log("moving left");
        //}
    }

     void OnApplicationQuit()
    {
        OnDestroy();
    }
}
