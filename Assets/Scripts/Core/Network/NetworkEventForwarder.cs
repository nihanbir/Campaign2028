using UnityEngine;

/// <summary>
/// Forwards TurnFlowBus events to/from network.
/// This is the ONLY networking code you need.
/// </summary>
public class NetworkEventForwarder : MonoBehaviour
{
    private static NetworkEventForwarder _instance;
    public static NetworkEventForwarder Instance => _instance;
    
    private bool _isOnline = false;
    private bool _isServer = true;
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void StartOnlineMode(bool isServer)
    {
        _isOnline = true;
        _isServer = isServer;
        
        GameManager.SetNetworkMode(isServer);
        
        // Start listening to bus events
        TurnFlowBus.Instance.OnEvent += OnLocalBusEvent;
    }
    
    public void StopOnlineMode()
    {
        _isOnline = false;
        _isServer = true;
        
        GameManager.SetNetworkMode(true);
        
        TurnFlowBus.Instance.OnEvent -= OnLocalBusEvent;
    }
    
    /// <summary>
    /// When server raises a bus event, send it to all clients
    /// </summary>
    private void OnLocalBusEvent(IGameEvent e)
    {
        if (!_isOnline) return;
        if (!_isServer) return; // Only server sends events
        
        // Serialize and send to all clients
        byte[] data = SerializeEvent(e);
        SendToAllClients(data);
    }
    
    /// <summary>
    /// When client receives event data from server, raise it on local bus
    /// </summary>
    public void OnNetworkEventReceived(byte[] data)
    {
        if (!_isOnline) return;
        if (_isServer) return; // Server doesn't receive events
        
        // Deserialize and raise on local bus
        IGameEvent e = DeserializeEvent(data);
        if (e != null)
        {
            TurnFlowBus.Instance.Raise(e);
        }
    }
    
    // TODO: Implement these with your networking solution
    private byte[] SerializeEvent(IGameEvent e)
    {
        // Use JSON, MessagePack, or whatever you want
        string json = JsonUtility.ToJson(e);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }
    
    private IGameEvent DeserializeEvent(byte[] data)
    {
        // Deserialize back to event
        string json = System.Text.Encoding.UTF8.GetString(data);
        // You'll need to know which event type it is
        return null; // TODO: Implement
    }
    
    private void SendToAllClients(byte[] data)
    {
        // TODO: Use your networking solution (Netcode, Mirror, etc.)
        Debug.Log($"[Network] Sending event to clients: {data.Length} bytes");
    }
}
