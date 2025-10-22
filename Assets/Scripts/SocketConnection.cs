using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Threading; 
using WebSocketSharp;
using TMPro; 

using Yarn.Unity;

namespace OpenAI
{
    public class SocketConnection : MonoBehaviour
    {
        private WebSocket websocket;
        private bool connected = false;
        // private bool remoteServer = false; 
        private bool remoteServer = true; 

        public delegate void ConnectionStatusChanged(bool isConnected);
        public event ConnectionStatusChanged OnConnectionStatusChanged;

        public delegate void MessageReceived(string message);
        public event MessageReceived OnMessageReceived;

        private Queue<string> yarnQueue; // for timing, wait until queue has been cleared before attempting reconnects

        public string pid = ""; // participant id
        public string condition = ""; // condition
        public string grade = "";
        public string peer_tutor = ""; 
        public WebSocketManager websocketManager; 
        private bool reconnectSuccess = false; 

        // Displaying error messages in game
        // public TextMeshProUGUI alertText; 

        void Start()
        {
            yarnQueue = GetComponent<MessageQueueCommands>().messagesQueue;
            websocketManager = GetComponent<WebSocketManager>(); 

            // ConnectToServer();
        }

        [YarnCommand("connect_to_server")]
        public void ConnectToServer(bool remote = false)
        {
            websocket = remote
                // ? new WebSocket("wss://smart-primer-2024-prod.fly.dev/ws")
                ? new WebSocket("wss://smart-primer-2024-7ba11c574f49.herokuapp.com/ws")
                : new WebSocket("ws://127.0.0.1:8000/ws");
            // Required to connect to hosted server
            websocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;

            if (remote) {
                remoteServer = true; 
            }

            Debug.Log("Trying to connect to server...");

            websocket.OnOpen += (sender, e) =>
            {
                Debug.Log("Connected to server");
                connected = true;
                OnConnectionStatusChanged?.Invoke(true);
            };

            websocket.OnMessage += (sender, e) =>
            {
                Debug.Log("Received message: " + e.Data);
                if (e.IsText)
                {
                    Debug.Log("Message is text: " + e.Data);
                    if (OnMessageReceived != null)
                    {
                        Debug.Log("Invoking OnMessageReceived event");
                        OnMessageReceived.Invoke(e.Data);
                        Debug.Log("OnMessageReceived event invoked");
                    }
                    else
                    {
                        Debug.LogWarning("OnMessageReceived event has no subscribers");
                    }
                }
                else
                {
                    Debug.LogWarning("Received non-text message, ignoring");
                }
            };

            websocket.OnClose += (sender, e) =>
            {
                Debug.Log("Disconnected from server: (" + e.Code + ") " + e.Reason);
                connected = false;
                OnConnectionStatusChanged?.Invoke(false);
                
                // If we disconnected in middle of running dialogue, ignore incoming duplicate message
                if (yarnQueue.Count > 0) {
                    websocketManager.acceptResponse = false; 
                } else {
                    websocketManager.acceptResponse = true; 
                }
                MainThreadDispatcher.Enqueue(() => StartCoroutine(Reconnect())); 
            };

            websocket.OnError += (sender, e) =>
            {
                Debug.LogError("WebSocket Error: " + e.Message + e.Exception.ToString());
                string errorMessage = e.Message.ToString(); 
                if (errorMessage == "The current state of the connection is not Open.") {
                    Debug.Log("Failed to connect to server"); 
                }
                if (errorMessage == "A series of reconnecting has failed.") {
                    websocket.Close(); 
                    ConnectToServer(remoteServer); 
                } 
            };

            websocket.Connect();
        }

        public void SendMessageToServer(string message)
        {
            if (connected)
            {
                var formattedMessage = $"{{\"message\":\"{message.Replace("\"", "\\\"")}\"}}";
                Debug.Log("Sending message: " + formattedMessage);
                websocket.Send(formattedMessage);
            }
            else
            {
                Debug.Log("Not connected to server");
            }
        }

        public void SendBinaryToServer(byte[] data)
        {
            if (connected)
            {
                websocket.Send(data);
            }
            else
            {
                Debug.Log("Not connected to server");
            }
        }

        public void SendFirstMessageToServer(string username, string initial_state, string participant_id, string cond, string grade, string peer_tutor)
        {
            if (connected)
            {
                condition = cond; 
                var formattedMessage = $"{{\"username\":\"{username.Replace("\"", "\\\"")}\",\"initial_state\":\"{initial_state.Replace("\"", "\\\"")}\",\"participant_id\":\"{participant_id.Replace("\"", "\\\"")}\",\"type\":\"{cond.Replace("\"", "\\\"")}\",\"grade\":\"{grade.Replace("\"", "\\\"")}\", \"peer_tutor\":\"{peer_tutor.Replace("\"", "\\\"")}\"}}"; 
                Debug.Log("Sending message: " + formattedMessage); 
                websocket.Send(formattedMessage);  
            }
            else
            {
                Debug.Log("Not connected to server"); 
            }
        }

        private void OnDestroy()
        {
            if (websocket != null)
            {
                websocket.Close();
            }
        }

        [YarnCommand("close_websocket")]
        public void CloseWebSocket() 
        {
            if (websocket != null)
                {
                    websocket.Close();
                }
        }

        private IEnumerator Reconnect() {
            reconnectSuccess = false; 
            Debug.Log("Waiting for the queue to clear before attempting reconnection, currently size of queue is" + yarnQueue.Count.ToString()); 
            while (yarnQueue.Count > 0) {
                yield return null;
            }
            Debug.Log("Queue is clear, attempting reconnect"); 
            if (connected == false && !websocket.IsAlive) {
                // Attempting every 3 seconds
                yield return new WaitForSeconds(3); 
                websocket = null; 
                Debug.Log("Attempting a reconnect now"); 
                ConnectToServer(remoteServer);
            }

            if (!connected) {
                Debug.Log("Failed attempt to reconnect"); 
                yield break; 
            }

            if (connected && reconnectSuccess == false) {
                // getting pid and condition again from yarn, hard-coded for now
                GlobalInMemoryVariableStorage.Instance.TryGetValue("$participant_id", out pid);
                if (pid == null) {
                    pid = "";
                }
                GlobalInMemoryVariableStorage.Instance.TryGetValue("$condition", out condition);
                GlobalInMemoryVariableStorage.Instance.TryGetValue("$grade", out grade);
                GlobalInMemoryVariableStorage.Instance.TryGetValue("$peer_tutor", out peer_tutor);
                SendFirstMessageToServer("", "", pid, condition, grade, peer_tutor);
                Debug.Log("Reconnected after disconnected and sent first message for participant" + pid + "with condition" + condition);
                reconnectSuccess = true; 
            }
        }
    }
}
