using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// NetworkMessages and NetworkObjects: The structure of the classes that gonna hold data that clients send to the server and the server send to clients

namespace NetworkMessages
{
    // Can add customized commands if needed
    public enum Commands{
        PLAYER_UPDATE,
        SERVER_UPDATE,
        HANDSHAKE,
        PLAYER_INPUT
    }

    [System.Serializable]
    public class NetworkHeader{
        public Commands cmd;
    }

    [System.Serializable]
    public class HandshakeMsg:NetworkHeader{
        public NetworkObjects.NetworkPlayer player;
        public HandshakeMsg(){      // Constructor. Set the Command to HANDSHAKE and create a new NetworkPlayer instance
            cmd = Commands.HANDSHAKE;
            player = new NetworkObjects.NetworkPlayer();
        }
    }
    
    [System.Serializable]
    public class PlayerUpdateMsg:NetworkHeader{
        public NetworkObjects.NetworkPlayer player;
        public PlayerUpdateMsg(){      // Constructor.
            cmd = Commands.PLAYER_UPDATE;
            player = new NetworkObjects.NetworkPlayer();
        }
    };

    // PlayerInputMsg is used to sned Input to the Server
    public class PlayerInputMsg:NetworkHeader{
        public Input myInput;
        public PlayerInputMsg(){
            cmd = Commands.PLAYER_INPUT;
            myInput = new Input();
        }
        /*
        public KeyCode keyCode;
        public PlayerInputMsg(KeyCode _inputKeycode){
            cmd = Commands.PLAYER_INPUT;
            keyCode = _inputKeycode;
        }
        */
    }
    
    [System.Serializable]
    public class  ServerUpdateMsg:NetworkHeader{
        public List<NetworkObjects.NetworkPlayer> players;
        public ServerUpdateMsg(){      // Constructor
            cmd = Commands.SERVER_UPDATE;
            players = new List<NetworkObjects.NetworkPlayer>();
        }
    }
} 

namespace NetworkObjects
{
    [System.Serializable]
    public class NetworkObject{
        public string id;
    }
    [System.Serializable]
    public class NetworkPlayer : NetworkObject{
        public GameObject cube;
        public Color cubeColor;
        public Vector3 cubPos;
        // public Quaternion cubeRot; // Cube rotation

        public NetworkPlayer(){
            cubeColor = new Color();
        }
    }
}
