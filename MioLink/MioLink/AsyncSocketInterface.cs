using Encrypt;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketInterface
{
    /// <summary>
    /// State object for reading client data asynchronously.  
    /// </summary>
    public class StateObject
    {
        public Socket workSocket = null;                ///< Client socket.  
        public const int BufferSize = 1024;             ///< Size of receive buffer. 
        public byte[] buffer = new byte[BufferSize];    ///< Receive buffer. 
        public StringBuilder sb = new StringBuilder();  ///< Received data string.  
    }

    /// <summary>
    /// Server communication. Listener and sender of packets.
    /// </summary>
    public class AsynchronousSocketListener
    {
        private const int port = 6107;  ///< Port number for the remote device.  
        private static Socket handler;  ///< Socket the program is connected to.

        /// <summary>
        /// Open port and start listening.
        /// </summary>
        public static void StartListening()
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Loopback, port);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                // Start an asynchronous socket to listen for connections.  
                Console.WriteLine("Waiting for a connection...");
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Shutdown();
            }
        }

        /// <summary>
        /// TCP handsake to confirm packets
        /// </summary>
        /// <param name="ar"></param>
        public static void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                // Get the socket that handles the client request.  
                Socket listener = (Socket)ar.AsyncState;
                handler = listener.EndAccept(ar);

                Receive();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Shutdown();
            }
        }

        /// <summary>
        /// Handle the received packet from the callback and start listening again.
        /// </summary>
        public static void Receive()
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = handler;
                handler.BeginReceive(state.buffer,
                                     0,
                                     StateObject.BufferSize,
                                     SocketFlags.None,
                                     new AsyncCallback(ReadCallback),
                                     state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Shutdown();
            }
        }

        /// <summary>
        /// Read the message data that got send.
        /// </summary>
        /// <param name="ar"></param>
        public static void ReadCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the handler socket  
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                handler = state.workSocket;

                // Read data from the client socket.   
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There  might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(
                        state.buffer, 0, bytesRead));

                    // Check for end-of-file tag. If it is not there, read   
                    // more data.  
                    string response = state.sb.ToString();
                    if (response.IndexOf("<EOF>") > -1)
                    {
                        response = response.Remove(response.IndexOf("<EOF>"), 5);
                        // All the data has been read from the   
                        // client. Display it on the console.  
                        Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                            response.Length, response);
                    }
                    else
                    {
                        // Not all data received. Get more.  
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);
                    }
                }

                Receive();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Shutdown();
            }
        }

        /// <summary>
        /// Encrypt and send the data packet.
        /// </summary>
        /// <param name="data">The data to be send as string.</param>
        public static void Send(string data)
        {
            if (handler != null && handler.Connected)
            {
                byte[] eof = Convert.FromBase64String("EOTF");

                // Hash the algoritm with MD5
                byte[] hashed = StringCipher.Encrypt(data);
                byte[] packet = new byte[hashed.Length + eof.Length];
                Array.Copy(hashed, packet, hashed.Length);
                Array.Copy(eof, 0, packet, hashed.Length, eof.Length);

                // Begin sending the data to the remote device.  
                handler.BeginSend(packet, 0, packet.Length, 0,
                    new AsyncCallback(SendCallback), handler);
            }
            else
            {
                Console.WriteLine("No unity instance is connected.");
            }
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Shutdown();
            }
        }

        /// <summary>
        /// Close the server.
        /// </summary>
        public static void Shutdown()
        {
            if (handler != null && handler.Connected)
            {
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
        }
    }
}
