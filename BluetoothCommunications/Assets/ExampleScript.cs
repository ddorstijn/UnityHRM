using UnityEngine;

/// <summary>
/// State object for receiving data from remote device.  
/// </summary>
public class ExampleScript : MonoBehaviour {

	// Use this for initialization
	void Start () 
    {
        // Start connection to the program that receives HRM status
        SocketManager.StartClient();
        // Subrscribe to heart rate updates
        SocketManager.OnHeartRateChanged += DoCalculation;
	}

    void OnDestroy() 
    {
        // Unsubsribe
        SocketManager.OnHeartRateChanged -= DoCalculation;
        SocketManager.Shutdown();
    }

    /// <summary>
    /// Use the heart rate in your algorithm. Be aware: storing the heart rate might violate user privacy!
    /// </summary>
    /// <param name="heartRate">The updated heart rate value.</param>
    void DoCalculation(int heartRate) 
    {
        Debug.Log("Heart rate updated! The new heart rate is " + heartRate);
    }
}
