using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BepInEx.Logging;

namespace MetaMystia;

public class MpManager
{
    private static MpManager _instance;
    private static readonly object _lock = new();
    private static ManualLogSource Log => Plugin.Instance.Log;
    private const int TCP_PORT = 40815;
    private string _playerId;
    public string PlayerId {get; set { _playerId = value; Log.LogInfo($"Player ID set to: {value}");}} = System.Environment.MachineName;
    private TcpServer server = new(TCP_PORT);
    private TcpClientWrapper client = null;
    public bool IsHost {get; private set; } = false;
    public bool IsRunning {get; private set;} = false;
    public bool IsConnected {get; private set;} = false;
    public string PeerAddress {get; set;}
    public string PeerId {get; set;} = "<Unknown>";
    public long Latency {get; private set;} = 0;
    private System.Collections.Concurrent.ConcurrentDictionary<int, long> pingSendTimes = new();
    public static long GetTimestampNow => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public static MpManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new MpManager();
                }
            }
            return _instance;
        }
    }

    private MpManager()
    {
    }

    public void Start()
    {
        if (IsRunning)
        {
            Log.LogWarning("MpManager is already running");
            return;
        }

        IsRunning = true;
        Log.LogInfo("Starting MpManager");
        PeerId = "<Unknown>";

        server.Start();
    }

    public void Stop()
    {
        if (!IsRunning)
            return;

        Log.LogInfo("Stopping MpManager");
        IsRunning = false;

        try
        {
            server.Stop();
            client?.Close();
            OnDisconnected();
        }
        catch (Exception e)
        {
            Log.LogError($"Error stopping: {e.Message}");
        }

        PeerId = "<Unknown>";
        Log.LogInfo("MpManager has stopped");
    }

    public void Restart()
    {
        Stop();
        Start();
    }

    public void ToggleRunning()
    {
        if (IsRunning)
        {
            Log.LogInfo("Stopping MpManager...");
            Stop();
        }
        else
        {
            Log.LogInfo("Attempting to start MpManager...");
            Start();
        }
    }


    public bool ConnectToPeer(string peerIp, int port = TCP_PORT)
    {
        if (IsConnected)
        {
            Log.LogWarning("[C] Already connected to a peer. Please disconnect first.");
            return false;
        }

        if (!IsRunning)
        {
            Start();
        }
        try
        {
            Log.LogInfo($"[C] Connecting to {peerIp}:{port}...");
            client = new(peerIp, port);
            OnConnected(client.client);
            Log.LogMessage($"[C] Successfully connected to peer {peerIp}:{port}");

            return true;
        }
        catch (Exception e)
        {
            Log.LogError($"[C] Error connecting to peer: {e.Message}");
            return false;
        }
    }


    public void OnConnected(TcpClient client, bool IsHost = false)
    {
        PeerAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
        Instance.IsConnected = true;
        Instance.IsHost = IsHost;
        SendHello();
        if (!IsHost)
        {
            ClientSendPing();
        }
        SendSync();
    }

    public void OnDisconnected()
    {
        PeerAddress = "<Unknown>";
        Instance.IsHost = false;
        Instance.IsConnected = false;
    }

    public static void OnAction(NetAction action)
    {
        action.OnReceived();
    }

    private void SendToPeer(NetPacket packet)
    {
        SendToPeer(packet, IsHost);
    }

    private void SendToPeer(NetPacket packet, bool asHost)
    {
        var actionType = packet.GetFirstAction().Type;
        if (!IsConnected)
        {
            Log.LogWarning($"Cannot send {actionType}: not connected to a peer");
            return;
        }

        if (asHost)
        {
            Log.LogInfo($"[S] Sending {actionType}");
            TcpClientWrapper.Send(server.currentClient, packet);
        } 
        else
        {
            Log.LogInfo($"[C] Sending {actionType}");
            client.Send(packet);
        }
    }
    
    public void DisconnectPeer()
    {
        if (IsConnected)
        {
            if (IsHost)
            {
                server.DisconnectClient();
            } 
            else
            {
                client.Close();
            }
            Log.LogMessage("Peer connection disconnected");
        }
    }

    /// <summary>
    /// C -> Ping -> S
    /// C <- Pong <- S   Client latency is calculated
    /// C -> Pang -> S   Server latency is calculated
    /// </summary>
    public void ClientSendPing()
    {
        if (client == null)
        {
            Log.LogInfo("client is null, will not send ping");
            return;
        }
        var t = GetTimestampNow;
        pingSendTimes[client.PingPacketId] = t;
        SendToPeer(PingAction.CreatePingPacket(t, client.PingPacketId++), false);
    }

    public void ServerResponsePong(int id)
    {
        var t = GetTimestampNow;    
        pingSendTimes[id] = t;
        SendToPeer(PongAction.CreatePongPacket(t, id), true);
    }

    public void ClientResponsePang(int id)
    {
        var t = GetTimestampNow;
        SendToPeer(PangAction.CreatePangPacket(t, id), false);
    }

    public void UpdateLatency(int id)
    {
        if (!pingSendTimes.TryRemove(id, out long t)) return;
        Latency = (GetTimestampNow - t) / 2;
    }

    public void SendHello()
    {
        NetPacket packet = new() {  };
        packet.Actions.Add(new HelloAction { PeerId = PlayerId });
        SendToPeer(packet);
    }

    public void SendSync()
    {
        // Maybe called before connected (because of patch), just ignore
        if (!IsConnected)
        {
            return;
        }
        var mapLabel = MystiaManager.MapLabel;
        var position = MystiaManager.Instance.GetPosition();
        var isSprinting = MystiaManager.IsSprinting;
        var inputDirection = MystiaManager.InputDirection;

        NetPacket packet = new() { };
        packet.Actions.Add(new SyncAction
        {
            IsSprinting = isSprinting,
            Vx = inputDirection.x,
            Vy = inputDirection.y,
            MapLabel = mapLabel,
            Px = position.x,
            Py = position.y
        });
        SendToPeer(packet);
    }

    public void SendReady()
    {
        NetPacket packet = new() { };
        packet.Actions.Add(new ReadyAction
        {
            IsReady = true
        });
        SendToPeer(packet);
    }

    public void SendMessage(string message)
    {
        SendToPeer(MessageAction.CreateMsgPacket(message));
    }

    public string GetStatus()
    {
        StringBuilder status = new StringBuilder();
        status.AppendLine($"Mystia ID: {PlayerId}");
        status.AppendLine($"Local Port: {TCP_PORT}");
        status.AppendLine($"Running: {(IsRunning ? "Yes" : "No")}");
        status.AppendLine($"Connected: {(IsConnected ? "Yes" : "No")}");
        if (IsConnected)
        {
            status.AppendLine($"Kyouko ID: {PeerId}");
            status.AppendLine($"Kyouko Address: {PeerAddress ?? "<Unknown>"}");
            status.AppendLine($"Latency: {Latency} ms");
        }

        return status.ToString();
    }

    public string GetBriefStatus()
    {
        if (!IsRunning)
        {
            return "Multiplayer: Off";
        }
        if (IsConnected)
        {
            return $"Multiplayer: [{(IsHost ? "S" : "C")}] Connected to {PeerId} ({PeerAddress}), ping: {Latency} ms";
        }
        else
        {
            return "Multiplayer: On (Not connected)";
        }
    }
}
