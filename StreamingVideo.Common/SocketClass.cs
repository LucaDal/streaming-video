using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static System.Net.WebRequestMethods;
namespace StreamingVideo.Common {

    public enum SocketTypes {
        Client,
        Server
    }
    public class MyStreamingSocket : IDisposable {

        public static int Port = 8090;

        public EventHandler<Command>? CommandRecived;

        private SocketTypes Type;

        private Dictionary<int, NetMQFrame> ActiveSockets = new Dictionary<int, NetMQFrame>();

        private RouterSocket Server = null;

        private RequestSocket Client = null;

        private NetMQPoller netMQPoller = new NetMQPoller();
        public void StartSocket(SocketTypes type, IPAddress ip) {

            string address = $"tcp://{ip.MapToIPv4()}:{Port}";
            Type = type;

            if (type == SocketTypes.Server) {
                Server = new RouterSocket(address);
                CommandRecived += (s, e) => {
                    SendCommand(e.Cmd, e.TimeSkipMillis);
                };
                Server.ReceiveReady += Server_ReceiveReady;
                netMQPoller.Add(Server);
            }
            else {
                Client = new RequestSocket($">{address}");
                Client.ReceiveReady += Server_ReceiveReady;
                netMQPoller.Add(Client);
            }
            netMQPoller.RunAsync();
        }

        private void Server_ReceiveReady(object? sender, NetMQSocketEventArgs e) {
            try {
                string payload = "";
                if (Type == SocketTypes.Server) {
                    var clientMessage = e.Socket.ReceiveMultipartMessage();
                    var clientAddress = clientMessage[0];
                    var hash = clientAddress.GetHashCode();
                    payload = clientMessage[2].ConvertToString();
                    ActiveSockets[hash] = clientAddress;
                }
                else {
                    payload = e.Socket.ReceiveFrameString();
                }
                Debug.WriteLine(payload);
                HandleMessage(payload);
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.Message);
            }
        }

        public void SendCommand(CommandType type, double timeSkipMillis = 0) {
            var msg = JsonSerializer.Serialize(new Command { Cmd = type, TimeSkipMillis = timeSkipMillis });
            var toSend = Encoding.ASCII.GetBytes(msg);

            try {
                if (Type == SocketTypes.Client) {
                    Client.SendFrame(toSend);
                }
                else {
                    
                    Dictionary<int, NetMQFrame> tempToRem = new Dictionary<int, NetMQFrame>();
                    Parallel.ForEach(ActiveSockets, (i) => {
                        var messageToClient = new NetMQMessage();
                        messageToClient.Append(i.Value);
                        messageToClient.AppendEmptyFrame();
                        messageToClient.Append(toSend);
                        bool esit = Server.TrySendMultipartMessage(messageToClient);
                        if (!esit) {
                            tempToRem.Add(i.Key, i.Value);
                        }
                    });
                    foreach(var toRem in tempToRem) {
                        ActiveSockets.Remove(toRem.Key);
                    }
                }
            }
            catch (Exception ex) {
                Debug.WriteLine("Error while sending message: " + ex.Message);
            }
        }

        /// <summary>
        /// Call with Dispatcher
        /// </summary>
        /// <param name="json"></param>
        private void HandleMessage(string json) {
            try {
                var cmd = JsonSerializer.Deserialize<Command>(json);
                if (cmd == null)
                    return;

                CommandRecived?.Invoke(null, cmd);
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.Message);
            }
        }

        public void Dispose() {
            netMQPoller.Dispose();
            Server?.Dispose();
            Client?.Dispose();
        }
    }
}
