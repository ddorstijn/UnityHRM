using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExampleScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
        SocketManager.StartClient();

        SocketManager.OnHeartRateChanged += UpdateGameWorld;
	}

    void OnDestroy()
    {
        SocketManager.OnHeartRateChanged -= UpdateGameWorld;
    }

    void UpdateGameWorld()
    {
        Debug.Log("Heart rate updated! The new heart rate is " + SocketManager.HeartRate);
    }
}
