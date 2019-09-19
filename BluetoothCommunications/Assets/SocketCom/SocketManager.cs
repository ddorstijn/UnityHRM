using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;


/// <summary>
/// State object for receiving data from remote device.  
/// </summary>
public class StateObject
{
    
    public Socket workSocket = null;                ///< Client socket.  
    public const int BufferSize = 256;              ///< Size of receive buffer.  
    public byte[] buffer = new byte[BufferSize];    ///< Receive buffer.  
    public List<byte> response = new List<byte>();  ///< Received data string.  
}

/// <summary>
/// Manages receiving of updates from Miolink.  
/// </summary>
public class SocketManager : MonoBehaviour
{
    
    private const int port = 6107;                              ///< The port number for the remote device.  
    private static Socket handler;                              ///< The socket the program is connected to.  
    public delegate void HeartRateChanged(int heart_rate);      ///< The event callback.
    public static event HeartRateChanged OnHeartRateChanged;    ///< The event other classes can subscribe to.

    /// <summary>
    /// Start listener for updates from MioLink.
    /// </summary>
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
            Debug.LogError(e.ToString());
            Shutdown();
        }
    }

    /// <summary>
    /// Called when a connection is established.
    /// </summary>
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
            Debug.LogError(e.ToString());
            Shutdown();
        }
    }

    /// <summary>
    /// Start receiving after connection.
    /// </summary>
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
            Debug.LogError(e.ToString());
            Shutdown();
        }
    }

    /// <summary>
    /// Callback from when a package is received. Decrypts the packet and notifies classes subscribed to heart rate update event.
    /// </summary>
    private static void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the state object and the handler socket from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            handler = state.workSocket;

            // Read data from the client socket.   
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                for (int i = 0; i < bytesRead; i++)
                {
                    state.response.Add(state.buffer[i]);
                }

                string responseString = Convert.ToBase64String(state.response.ToArray());
                // Check for end-of-file tag. If it is not there, read more data.  
                if (responseString.IndexOf("EOTF") > -1)
                {
                    responseString = responseString.Remove(responseString.IndexOf("EOTF"), 4);
                    string numberString = StringCipher.Decrypt(responseString);
                    
                    int heart_rate;
                    int.TryParse(numberString, out heart_rate);


                    if (OnHeartRateChanged != null)
                    {
                        OnHeartRateChanged(heart_rate);
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
            Debug.LogError(e.ToString());
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
            // Release the socket.  
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }
    }
}