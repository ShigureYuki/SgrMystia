using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using BepInEx.Logging;
using UnityEngine;

namespace MetaMystia;

public class MultiplayerManager
{
    private static MultiplayerManager _instance;
    private static readonly object _lock = new object();
    private static ManualLogSource Log => Plugin.Instance.Log;

    private const int TCP_PORT = 40815;

    private string _localId;
    private TcpListener _tcpListener;
    private Thread _tcpListenerThread;
    private bool _isRunning = false;
    private bool _isConnected = false;
    private TcpClient _peerConnection;
    private Thread _peerHandlerThread;
    private string _peerAddress;

    public static MultiplayerManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new MultiplayerManager();
                    }
                }
            }
            return _instance;
        }
    }

    private MultiplayerManager()
    {
        GenerateLocalId();
    }

    private void GenerateLocalId()
    {
        // 基于硬件信息和随机数生成唯一ID
        string hwInfo = SystemInfo.deviceUniqueIdentifier;
        string randomPart = Guid.NewGuid().ToString("N").Substring(0, 8);
        _localId = $"{hwInfo.Substring(0, Math.Min(8, hwInfo.Length))}-{randomPart}";
        Log.LogInfo($"生成本地 ID: {_localId}");
    }

    public string GetLocalId()
    {
        return _localId;
    }

    public bool IsRunning()
    {
        return _isRunning;
    }

    public bool IsConnected()
    {
        return _isConnected;
    }

    public string GetPeerAddress()
    {
        return _peerAddress;
    }

    public void Start()
    {
        if (_isRunning)
        {
            Log.LogWarning("MultiplayerManager 已在运行中");
            return;
        }

        _isRunning = true;
        Log.LogInfo("启动 MultiplayerManager");

        // 启动 TCP 监听器
        StartTcpListener();
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        Log.LogInfo("停止 MultiplayerManager");
        _isRunning = false;

        // 断开对等连接
        DisconnectPeer();

        // 停止 TCP 监听器
        try
        {
            _tcpListener?.Stop();
        }
        catch (Exception e)
        {
            Log.LogError($"停止 TCP 监听器时出错: {e.Message}");
        }

        Log.LogInfo("MultiplayerManager 已停止");
    }

    private void StartTcpListener()
    {
        try
        {
            _tcpListener = new TcpListener(IPAddress.Any, TCP_PORT);
            _tcpListener.Start();
            Log.LogInfo($"TCP 监听器启动在端口 {TCP_PORT}");

            _tcpListenerThread = new Thread(TcpListenerLoop);
            _tcpListenerThread.IsBackground = true;
            _tcpListenerThread.Start();
        }
        catch (Exception e)
        {
            Log.LogError($"启动 TCP 监听器失败: {e.Message}");
        }
    }

    private void TcpListenerLoop()
    {
        while (_isRunning)
        {
            try
            {
                if (_tcpListener.Pending())
                {
                    if (_isConnected)
                    {
                        // 已有连接，拒绝新连接
                        TcpClient rejectedClient = _tcpListener.AcceptTcpClient();
                        Log.LogInfo($"拒绝来自 {rejectedClient.Client.RemoteEndPoint} 的连接（已有活动连接）");
                        rejectedClient.Close();
                    }
                    else
                    {
                        TcpClient client = _tcpListener.AcceptTcpClient();
                        Log.LogInfo($"接受来自 {client.Client.RemoteEndPoint} 的连接");
                        AcceptPeerConnection(client);
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                if (_isRunning)
                {
                    Log.LogError($"TCP 监听循环出错: {e.Message}");
                }
            }
        }
    }

    public bool ConnectToPeer(string peerIp)
    {
        if (_isConnected)
        {
            Log.LogWarning("已有活动连接，请先断开");
            return false;
        }

        try
        {
            Log.LogInfo($"正在连接到 {peerIp}:{TCP_PORT}...");
            TcpClient client = new TcpClient();
            client.Connect(peerIp, TCP_PORT);
            Log.LogInfo($"成功连接到对等端 {peerIp}:{TCP_PORT}");

            _peerAddress = peerIp;
            _isConnected = true;
            _peerConnection = client;

            // 启动对等连接处理线程
            _peerHandlerThread = new Thread(() => HandlePeerConnection(client));
            _peerHandlerThread.IsBackground = true;
            _peerHandlerThread.Start();

            return true;
        }
        catch (Exception e)
        {
            Log.LogError($"连接到对等端失败: {e.Message}");
            return false;
        }
    }

    private void AcceptPeerConnection(TcpClient client)
    {
        if (_isConnected)
        {
            Log.LogWarning("已有活动连接，拒绝新连接");
            client.Close();
            return;
        }

        _isConnected = true;
        _peerConnection = client;
        _peerAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
        
        Log.LogInfo($"接受对等连接来自 {_peerAddress}");

        // 启动对等连接处理线程
        _peerHandlerThread = new Thread(() => HandlePeerConnection(client));
        _peerHandlerThread.IsBackground = true;
        _peerHandlerThread.Start();
    }

    private void HandlePeerConnection(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        StringBuilder messageBuilder = new StringBuilder();

        try
        {
            while (_isRunning && _isConnected && client.Connected)
            {
                if (stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        messageBuilder.Append(data);

                        // 处理完整的行
                        string accumulated = messageBuilder.ToString();
                        int newlineIndex;
                        while ((newlineIndex = accumulated.IndexOf('\n')) >= 0)
                        {
                            string line = accumulated.Substring(0, newlineIndex).Trim('\r', '\n');
                            accumulated = accumulated.Substring(newlineIndex + 1);

                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                ProcessPeerMessage(line, client);
                            }
                        }
                        messageBuilder.Clear();
                        messageBuilder.Append(accumulated);
                    }
                }
                else
                {
                    Thread.Sleep(50);
                }
            }
        }
        catch (Exception e)
        {
            Log.LogError($"处理对等连接时出错: {e.Message}");
        }
        finally
        {
            Log.LogInfo("对等连接已断开");
            DisconnectPeer();
        }
    }

    private void ProcessPeerMessage(string message, TcpClient client)
    {
        Log.LogInfo($"收到对等消息: {message}");

        string[] parts = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return;

        string command = parts[0].ToLower();

        switch (command)
        {
            case "ping":
                SendToPeer("pong\n");
                Log.LogInfo("响应 ping，发送 pong");
                break;

            case "pong":
                Log.LogInfo("收到 pong 响应");
                break;

            case "move":
                // 格式: move <x> <y>
                if (parts.Length >= 3)
                {
                    if (float.TryParse(parts[1], out float x) && float.TryParse(parts[2], out float y))
                    {
                        KyoukoManager.Instance.UpdateInputDirection(new UnityEngine.Vector2(x, y));
                    }
                }
                break;

            default:
                Log.LogWarning($"未知的对等命令: {command}");
                break;
        }
    }

    private void SendToPeer(string message)
    {
        if (!_isConnected || _peerConnection == null)
        {
            Log.LogWarning("没有活动的对等连接");
            return;
        }

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            NetworkStream stream = _peerConnection.GetStream();
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }
        catch (Exception e)
        {
            Log.LogError($"发送到对等端失败: {e.Message}");
            DisconnectPeer();
        }
    }

    public void SendPing()
    {
        if (_isConnected)
        {
            Log.LogInfo("发送 ping 到对等端");
            SendToPeer("ping\n");
        }
        else
        {
            Log.LogWarning("无法发送 ping：未连接");
        }
    }

    public void SendMoveData(UnityEngine.Vector2 inputDirection)
    {
        if (_isConnected)
        {
            // 格式: move <x> <y>
            string message = $"move {inputDirection.x} {inputDirection.y}\n";
            SendToPeer(message);
        }
    }

    public void DisconnectPeer()
    {
        if (_peerConnection != null)
        {
            try
            {
                _peerConnection.Close();
            }
            catch { }
            _peerConnection = null;
        }

        _isConnected = false;
        _peerAddress = null;

        Log.LogInfo("Peer connection disconnected");
    }

    public string GetStatus()
    {
        StringBuilder status = new StringBuilder();
        status.AppendLine($"Local ID: {_localId}");
        status.AppendLine($"Local Port: {TCP_PORT}");
        status.AppendLine($"Running: {(_isRunning ? "Yes" : "No")}");
        status.AppendLine($"Connected: {(_isConnected ? "Yes" : "No")}");
        
        if (_isConnected)
        {
            status.AppendLine($"Peer Address: {_peerAddress ?? "Unknown"}");
        }

        return status.ToString();
    }
}
