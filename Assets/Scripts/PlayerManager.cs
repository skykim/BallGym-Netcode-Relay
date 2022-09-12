using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System;
using Unity.Services.Relay.Models;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;

public class PlayerManager : MonoBehaviour
{
    [SerializeField]
    private Button startHostButton;

    [SerializeField]
    private Button startClientButton;

    [SerializeField]
    private TMP_InputField joinCodeInput;

    private static string _joinCode = "";
    private int _numOfClients = 0; 
        
    async void Start()
    {
        await Initialize();
        
        startHostButton?.onClick.AddListener(async () =>
        {
            await HostGame(4);
        });
        
        startClientButton?.onClick.AddListener(async () =>
        {
            await JoinGame(joinCodeInput.text);
        });

        NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
        {
            _numOfClients++;
            Debug.Log($"{id} just connected...");
        };

        NetworkManager.Singleton.OnClientDisconnectCallback += (id) =>
        {
            _numOfClients--;
            Debug.Log($"{id} just disconnected...");
        };
    }
    
    void Update()
    {
        if (NetworkManager.Singleton.LocalClient != null)
        {
            // Get `PlayerBall` component from the player's 'PlayerObject'
            if (NetworkManager.Singleton.LocalClient.PlayerObject.TryGetComponent(out PlayerBallControl playerBall))
            {
                float horizontal = Input.GetAxis("Horizontal");
                float vertical = Input.GetAxis("Vertical");

                // Invoke a 'ServerRpc' from client-side to move player on the server-side
                playerBall.ClientInputServerRpc(horizontal, vertical);
            }
        }
    }
    
    async Task Initialize()
    {
        //Initialize the Unity Services engine
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            //If not already logged, log the user in
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
    
    /// <summary>
    /// RelayHostData represents the necessary information
    /// for a Host to host a game on a Relay
    /// </summary>
    public struct RelayHostData
    {
        public string JoinCode;
        public string IPv4Address;
        public ushort Port;
        public Guid AllocationID;
        public byte[] AllocationIDBytes;
        public byte[] ConnectionData;
        public byte[] Key;
    }

    /// <summary>
    /// HostGame allocates a Relay server and returns needed data to host the game
    /// </summary>
    /// <param name="maxConn">The maximum number of peer connections the host will allow</param>
    /// <returns>A Task returning the needed hosting data</returns>
    public static async Task<RelayHostData> HostGame(int maxConn)
    {
        //Initialize the Unity Services engine
        await UnityServices.InitializeAsync();
        //Always autheticate your users beforehand
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            //If not already logged, log the user in
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        //Ask Unity Services to allocate a Relay server
        Allocation allocation = await Unity.Services.Relay.RelayService.Instance.CreateAllocationAsync(maxConn);

        //Populate the hosting data
        RelayHostData data = new RelayHostData
        {
            // WARNING allocation.RelayServer is deprecated
            IPv4Address = allocation.RelayServer.IpV4,
            Port = (ushort) allocation.RelayServer.Port,

            AllocationID = allocation.AllocationId,
            AllocationIDBytes = allocation.AllocationIdBytes,
            ConnectionData = allocation.ConnectionData,
            Key = allocation.Key,
        };

        //Retrieve the Relay join code for our clients to join our party
        data.JoinCode = await Unity.Services.Relay.RelayService.Instance.GetJoinCodeAsync(data.AllocationID);
        Debug.Log(data.JoinCode);

        _joinCode = data.JoinCode;
                
        //Retrieve the Unity transport used by the NetworkManager
        UnityTransport transport = NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>();
        transport.SetRelayServerData(data.IPv4Address, data.Port, data.AllocationIDBytes, data.Key,
            data.ConnectionData);

        NetworkManager.Singleton.StartHost();

        return data;
    }

    private void OnGUI()
    {
        GUIStyle _style= GUIStyle.none;
        _style.fontSize = 20; //change the font size
        
        GUILayout.BeginArea(new Rect(10, 80, 200, 200));
        GUILayout.Label(_joinCode, _style);
        
        if (NetworkManager.Singleton.IsHost)
            GUILayout.Label("Clients:" + _numOfClients.ToString(), _style);
        
        GUILayout.EndArea();
    }

    /// <summary>
    /// RelayHostData represents the necessary information
    /// for a Host to host a game on a Relay
    /// </summary>
    public struct RelayJoinData
    {
        public string IPv4Address;
        public ushort Port;
        public Guid AllocationID;
        public byte[] AllocationIDBytes;
        public byte[] ConnectionData;
        public byte[] HostConnectionData;
        public byte[] Key;
    }

    /// <summary>
    /// Join a Relay server based on the JoinCode received from the Host or Server
    /// </summary>
    /// <param name="joinCode">The join code generated on the host or server</param>
    /// <returns>All the necessary data to connect</returns>
    public static async Task<RelayJoinData> JoinGame(string joinCode)
    {
        //Initialize the Unity Services engine
        await UnityServices.InitializeAsync();
        //Always authenticate your users beforehand
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            //If not already logged, log the user in
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    
        //Ask Unity Services for allocation data based on a join code
        JoinAllocation allocation = await Unity.Services.Relay.RelayService.Instance.JoinAllocationAsync(joinCode);
    
        //Populate the joining data
        RelayJoinData data = new RelayJoinData
        {
            // WARNING allocation.RelayServer is deprecated. It's best to read from ServerEndpoints.
            IPv4Address = allocation.RelayServer.IpV4,
            Port = (ushort) allocation.RelayServer.Port,

            AllocationID = allocation.AllocationId,
            AllocationIDBytes = allocation.AllocationIdBytes,
            ConnectionData = allocation.ConnectionData,
            HostConnectionData = allocation.HostConnectionData,
            Key = allocation.Key,
        };
        
        //Retrieve the Unity transport used by the NetworkManager
        UnityTransport transport = NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>();
        transport.SetRelayServerData(data.IPv4Address, data.Port, data.AllocationIDBytes, data.Key,
            data.ConnectionData, data.HostConnectionData);
        
        NetworkManager.Singleton.StartClient();
        
        return data;
    }
}
