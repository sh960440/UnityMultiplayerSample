using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

public class NetworkServer : MonoBehaviour
{
    public NetworkDriver m_Driver; // Used to listen to stuff send things
    public ushort serverPort; // Port of the server
    private NativeList<NetworkConnection> m_Connections;

    public Dictionary<string, NetworkObjects.NetworkPlayer> clients = new Dictionary<string, NetworkObjects.NetworkPlayer>();

    void Start ()
    {
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4; // Set an endpoint to AnyIpv4 (Grab the IP of the machine which is running on)
        endpoint.Port = serverPort;
        if (m_Driver.Bind(endpoint) != 0) 
            Debug.Log("Failed to bind to port " + serverPort); // If it doesn't bind, Log it
        else
            m_Driver.Listen();

        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent); // Initialize a connection list, create a list of 16 end systems.

        StartCoroutine(SendHandshakeToAllClient());
    }

    IEnumerator SendHandshakeToAllClient()
    {
        while (true)
        {
            ServerUpdateMsg suMsg = new ServerUpdateMsg();

            foreach (KeyValuePair<string, NetworkObjects.NetworkPlayer> client in clients)
            {
                suMsg.players.Add(client.Value);
            }

            for (int i = 0; i < m_Connections.Length; i++)
            {
                if (!m_Connections[i].IsCreated)
                    continue;

                // Example to send a handshake message:
                //HandshakeMsg m = new HandshakeMsg();
                //m.player.id = m_Connections[i].InternalId.ToString();
                SendToClient(JsonUtility.ToJson(suMsg), m_Connections[i]);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    void SendToClient(string message, NetworkConnection c){
        var writer = m_Driver.BeginSend(NetworkPipeline.Null, c);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message),Allocator.Temp); // Convert the massage into bytes
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }

    void OnConnect(NetworkConnection c){
        m_Connections.Add(c);
        //clients[c.InternalId.ToString()].id = c.InternalId.ToString();
        Debug.Log("Accepted a connection");

        //// Example to send a handshake message:
        HandshakeMsg m = new HandshakeMsg();
        m.player.id = c.InternalId.ToString();
        SendToClient(JsonUtility.ToJson(m),c); 
        clients.Add(c.InternalId.ToString(), new NetworkObjects.NetworkPlayer()); 
        clients[c.InternalId.ToString()].id = c.InternalId.ToString();
    }

    void OnData(DataStreamReader stream, int i){
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length,Allocator.Temp); // Convert the stream into bytes
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray()); // Convert bytes into string
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg); // Convert the string into a json object

        switch(header.cmd){
            case Commands.HANDSHAKE:
            HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
            Debug.Log("Handshake message received!");
            break;
            case Commands.PLAYER_UPDATE:
            PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
            clients[puMsg.player.id].cubPos = puMsg.player.cubPos;
            //clients[puMsg.player.id].cubeColor = puMsg.player.cubeColor;
            Debug.Log("Player update message received!");
            break;
            case Commands.SERVER_UPDATE:
            ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
            Debug.Log("Server update message received!");
            break;
            default:
            Debug.Log("SERVER ERROR: Unrecognized message received!");
            break;
        }
    }

    void OnDisconnect(int i){
        Debug.Log("Client disconnected from server");
        m_Connections[i] = default(NetworkConnection);
    }

    void Update ()
    {
        m_Driver.ScheduleUpdate().Complete(); // Tell the driver we are ready to listen for next event

        // CleanUpConnections
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {

                m_Connections.RemoveAtSwapBack(i);
                --i;
            }
        }

        // AcceptNewConnections // Check if there is any new connection
        NetworkConnection c = m_Driver.Accept();
        while (c  != default(NetworkConnection))
        {            
            OnConnect(c); // Take the connection that just happeened and send to OnConnect

            c = m_Driver.Accept();
        }


        // Read Incoming Messages
        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            Assert.IsTrue(m_Connections[i].IsCreated); // Make sure a connection is valid (Should not be a default connection)
            
            NetworkEvent.Type cmd;
            cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            while (cmd != NetworkEvent.Type.Empty) // As long as the event type is not empty
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    OnData(stream, i);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    OnDisconnect(i);
                }

                cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            }
        }
    }
}