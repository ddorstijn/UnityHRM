# UnityHRM
Heart Rate Monitor communication in unity.

# Important note
For this to work, a ANT+ USB is required. ANT+ is a protocol very similar to bluetooth, but uses a proprietary driver. The USB is to be found here: https://buy.garmin.com/en-US/US/p/10997.

# How this works
There are two applications. The MioLink application is a C# project that handles the connection between the computer and the HRM devce. It sets up a local socket to talk to the Unity application and send the updated heart rates. These heart rates are encrypted with a basic encryption for privacy of the user of the application.

The Unity application simply connects to the MioLink server and waits for updates to be sent. It is important to first start the MioLink application, so Unity can connect to it. Once a packet is received it gets decrypted and a event is sent out to all classes subscribed to the heart rate changed event. Heart rate is also not stored because of privacy.

# Documentation
Doxygen was used to create documentation. The prebuilt HTML and PDF documents can be found in docs/MioLink and docs/Unity for the two projects. 
