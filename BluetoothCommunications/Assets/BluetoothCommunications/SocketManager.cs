using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;


// State object for receiving data from remote device.  
public class StateObject
{
    // Client socket.  
    public Socket workSocket = null;
    // Size of receive buffer.  
    public const int BufferSize = 256;
    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];
    // Received data string.  
    public StringBuilder sb = new StringBuilder();
}

public class SocketManager : MonoBehaviour
{
    // The port number for the remote device.  
    private const int port = 6107;

    // The socket the program is connected to.
    private static Socket handler;

    // Heart rate data holder.
    private static int heart_rate;

    // The event other classes can subscribe to.
    public delegate void HeartRateChanged();
    public static event HeartRateChanged OnHeartRateChanged;

    public static void StartClient()
    {
        // Connect to a remote device.  
        try
        {
            IPEndPoint local_ip = new IPEndPoint(IPAddress.Loopback, port);

            // Create a TCP/IP socket.  
            handler = new Socket(local_ip.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Connect to the remote endpoint.  
            handler.BeginConnect(local_ip, 
                                new AsyncCallback(ConnectCallback), 
                                handler);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            Shutdown();
        }
    }

    private static void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            handler = (Socket)ar.AsyncState;

            // Complete the connection.  
            handler.EndConnect(ar);

            Debug.Log("Socket connected to " +
                handler.RemoteEndPoint.ToString());

            Receive();
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            Shutdown();
        }
    }

    private static void Receive()
    {
        try
        {
            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;

            // Begin receiving the data from the remote device.  
            handler.BeginReceive(state.buffer, 
                                0, 
                                StateObject.BufferSize, 
                                SocketFlags.None,
                                new AsyncCallback(ReceiveCallback), 
                                state);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            Shutdown();
        }
    }

    private static void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            String response = String.Empty;

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
                    int.TryParse(response, out heart_rate);

                    if (OnHeartRateChanged != null)
                    {
                        OnHeartRateChanged();
                    }
                }
                else
                {
                    // Not all data received. Get more.  
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
                }
            }

            Receive();
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            Shutdown();
        }
    }

    private static void Send(String data)
    {
        if (handler != null && handler.Connected)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }
        else
        {
            Debug.Log("Not Connected.");
        }
    }

    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket client = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = client.EndSend(ar);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            Shutdown();
        }
    }

    public static int HeartRate
    {
        get
        {
            return heart_rate;
        }
    }

    private void OnApplicationQuit()
    {
        Shutdown();
    }

    public static void Shutdown()
    {
        if (handler != null && handler.Connected)
        {
            // Release the socket.  
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }
    }
}