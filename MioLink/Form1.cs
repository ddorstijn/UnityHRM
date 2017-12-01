using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MioLink
{
    public partial class Form1 : Form
    {
        Random random = new Random(1);

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void Form1_Closing(object sender, CancelEventArgs e)
        {
            AsynchronousSocketListener.Shutdown();
        }

        private void btn_connect_Click(object sender, EventArgs e)
        {
            int heartrate = random.Next(50, 200);
            AsynchronousSocketListener.Send(heartrate.ToString());
        }
    }



    // State object for reading client data asynchronously  
    public class StateObject
    {
        // Client  socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 1024;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }

    public class AsynchronousSocketListener
    {
        // The port number for the remote device.  
        private const int port = 6107;

        // The socket the program is connected to.
        private static Socket handler;

        // The response from the remote device.  
        private static String response = String.Empty;

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
                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Shutdown();
            }
        }

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

        public static void Receive()
        {
            try {
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
                    response = state.sb.ToString();
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

        public static void Send(String data)
        {
            if (handler != null && handler.Connected)
            {
                // Convert the string data to byte data using ASCII encoding.  
                byte[] byteData = Encoding.ASCII.GetBytes(data + "<EOF>");

                // Begin sending the data to the remote device.  
                handler.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), handler);
            }
            else
            {
                Console.WriteLine("Not Connected.");
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
