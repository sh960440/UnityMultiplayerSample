﻿using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using NetworkObjects;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

public class NetworkClient : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public string serverIP;
    public ushort serverPort;

    public GameObject player;
    public Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
    public string assignedPlayerID = null;
    
    

    
    void Start ()
    {
        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverIP,serverPort);
        endpoint.Port = serverPort;
        m_Connection = m_Driver.Connect(endpoint);
    }
    
    void SendToServer(string message){
        var writer = m_Driver.BeginSend(m_Connection);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message),Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    void OnConnect(){
        Debug.Log("We are now connected to the server");
        StartCoroutine(SendRepeatedHandshake());

        //players.Add(assignedPlayerID, Instantiate(player, new Vector3(0, 0, 0), Quaternion.identity));
        // players[assignedPlayerID].name = "Player" + assignedPlayerID.ToString();

        //// Example to send a handshake message:
        // HandshakeMsg m = new HandshakeMsg();
        // m.player.id = m_Connection.InternalId.ToString();
        // SendToServer(JsonUtility.ToJson(m));
    }

    IEnumerator SendRepeatedHandshake()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            
            Debug.Log("Sending handshake");
            PlayerUpdateMsg m = new PlayerUpdateMsg();
            m.player.id = assignedPlayerID;
            m.player.cube = players[assignedPlayerID];
            m.player.cubPos = players[assignedPlayerID].transform.position; 
            SendToServer(JsonUtility.ToJson(m));
        }
    }

    void OnData(DataStreamReader stream){
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length,Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch(header.cmd){
            case Commands.HANDSHAKE:
            HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
            Debug.Log("Handshake message received!");
            assignedPlayerID = hsMsg.player.id;
            players.Add(assignedPlayerID, Instantiate(player)); 
            break;
            case Commands.PLAYER_UPDATE:
            PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
            Debug.Log("Player update message received!");
            break;
            case Commands.SERVER_UPDATE:
            ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
            for (int i = 0; i < suMsg.players.Count; i++) 
            {
                if (!players.ContainsKey(suMsg.players[i].id))
                {
                    players.Add(suMsg.players[i].id, Instantiate(player));
                }
                if (suMsg.players[i].id != assignedPlayerID)
                {
                    players[suMsg.players[i].id].transform.position = suMsg.players[i].cubPos;
                    //players[suMsg.players[i].id].GetComponent<Renderer>().material.color = suMsg.players[i].cubeColor;
                }
            }
            Debug.Log("Server update message received!");
            break;
            default:
            Debug.Log("Unrecognized message received!");
            break;
        }
    }

    void Disconnect(){
        m_Connection.Disconnect(m_Driver);
        m_Connection = default(NetworkConnection);
    }

    void OnDisconnect(){
        Debug.Log("Client got disconnected from server");
        m_Connection = default(NetworkConnection);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }   
    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        if (Input.GetKey("w"))
        {
            players[assignedPlayerID].transform.Translate(new Vector3(0, 1 * Time.deltaTime, 0));
        }
        if (Input.GetKey("s"))
        {
            players[assignedPlayerID].transform.Translate(new Vector3(0, -1 * Time.deltaTime, 0));
        }
        if (Input.GetKey("d"))
        {
            players[assignedPlayerID].transform.Translate(new Vector3(1 * Time.deltaTime, 0, 0));
        }
        if (Input.GetKey("a"))
        {
            players[assignedPlayerID].transform.Translate(new Vector3(-1 * Time.deltaTime, 0, 0));
        }


        if (!m_Connection.IsCreated)
        {
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;
        cmd = m_Connection.PopEvent(m_Driver, out stream);
        while (cmd != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                OnConnect();
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                OnData(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                OnDisconnect();
            }

            cmd = m_Connection.PopEvent(m_Driver, out stream);
        }
    }
}