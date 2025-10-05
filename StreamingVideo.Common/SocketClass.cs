using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Threading;
using static System.Net.WebRequestMethods;

namespace StreamingVideo.Common {

    public enum SocketTypes {
        Client,
        Server
    }

    internal class StateObject {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
    }

    public class AsynchronousSocketListener {

        private static ManualResetEvent allDone = new ManualResetEvent(false);

        public static int Port = 8090;

        public static EventHandler<Command>? CommandRecived;

        private static Socket Socket;

        private static SocketTypes Type;

        private static List<Socket> ActiveSockets = new List<Socket>();

        private static void StartServer(IPEndPoint localEndPoint, StateObject state) {
            // Bind the socket to the local endpoint and listen for incoming connections.
            try {
                CommandRecived += (s, e) => {
                    SendCmd(e.Cmd, e.TimeSkipMillis);
                };

                Socket.Bind(localEndPoint);
                Socket.Listen(100);

                while (true) {
                    // Set the event to nonsignaled state.
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    Console.WriteLine("Waiting for a connection...");
                    Socket.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        state);

                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        private static void StartClient(IPEndPoint localEndPoint, StateObject state) {
            Socket.Connect(localEndPoint);

            Socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        public static void StartSocket(SocketTypes type, IPAddress ip) {
            // Data buffer for incoming data.
            byte[] bytes = new Byte[1024];

            IPEndPoint localEndPoint = new IPEndPoint(ip, Port);
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Type = type;

            StateObject state = new StateObject {
                workSocket = Socket
            };
            if (type == SocketTypes.Server) {
                StartServer(localEndPoint, state);
            }
            else {
                StartClient(localEndPoint, state);
            }
        }

        public static void AcceptCallback(IAsyncResult ar) {
            // Signal the main thread to continue.
            allDone.Set();

            // Get the socket that handles the client request.
            StateObject obj = (StateObject)ar.AsyncState;
            Socket handler = obj.workSocket.EndAccept(ar);

            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = handler;
            ActiveSockets.Add(handler);
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        public static void ReadCallback(IAsyncResult ar) {

            StateObject state = (StateObject)ar.AsyncState;
            if (state == null)
                return;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            try {
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0) {
                    string content = Encoding.ASCII.GetString(
                        state.buffer, 0, bytesRead);

                    HandleMessage(content, state);
                }
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
            }
            catch(SocketException er) {
                if(er.ErrorCode == 10054) {
                    ActiveSockets.Remove(handler);
                }
            }
            catch (Exception ex) {
                Debug.WriteLine("Error reading message: " + ex.Message);
            }
        }

        private static void Send(Socket handler, String data) {
            // Convert the string data to byte data using ASCII encoding.

            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar) {
            try {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        public static void SendCmd(CommandType type, double timeSkipMillis = 0) {

            var msg = JsonSerializer.Serialize(new Command { Cmd = type, TimeSkipMillis = timeSkipMillis });
            var toSend = Encoding.ASCII.GetBytes(msg);

            try {

                if (Type == SocketTypes.Client) {
                    Socket.Send(toSend);
                }
                else {
                    List<Socket> toRem = new List<Socket>();
                    foreach (var sock in ActiveSockets) {
                        try {
                            sock.Send(toSend);
                        }
                        catch (Exception e) {
                            Debug.WriteLine(e.Message);
                            toRem.Add(sock);
                        }
                    }
                    foreach (var sock in toRem) {
                        ActiveSockets.Remove(sock);
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
        private static void HandleMessage(string json, StateObject state) {
            try {
                Debug.WriteLine(json);
                var cmd = JsonSerializer.Deserialize<Command>(json);
                if (cmd == null)
                    return;

                CommandRecived?.Invoke(null, cmd);
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.Message);
            }
        }

    }
}
