using ANT_Managed_Library;
using SocketInterface;
using System;
using System.Text;

namespace ANT
{
    /// <summary>
    /// Interface for communication with the HRM.
    /// </summary>
    class ANTInterface
    {
        static readonly byte USER_ANT_CHANNEL = 0;          ///< ANT Channel to use
        static readonly ushort USER_DEVICENUM = 0;          ///< Device number 
        static readonly byte USER_DEVICETYPE = 0x78;        ///< Device type
        static readonly byte USER_TRANSTYPE = 0;            ///< Transmission type
        static readonly byte USER_RADIOFREQ = 57;           ///< RF Frequency + 2400 MHz                              
        static readonly ushort USER_CHANNELPERIOD = 8070;   ///< Channel Period (8192/32768)s period = 4Hz

        static readonly byte[] USER_NETWORK_KEY = { 0xB9, 0xA5, 0x21, 0xFB, 0xBD, 0x72, 0xC3, 0x45 };   ///< ANT+ network key that specifies connection type
        static readonly byte USER_NETWORK_NUM = 1;                                                      ///< The network key is assigned to this network number

        static ANT_Device device0;                              ///< Handle to connected HRM
        static ANT_Channel channel0;                            ///< Channel to receive from
        static ANT_ReferenceLibrary.ChannelType channelType;    ///< 0 = Master, 1 = Slave

        /// <summary>
        /// Initialize parameters for a HRM device connection.
        /// </summary>
        public static void Init()
        {
            try
            {
                Console.WriteLine("Attempting to connect to an ANT USB device...");
                // Create a device instance using the automatic constructor (automatic detection of USB device number and baud rate)
                device0 = new ANT_Device();
                // Add device response function to receive protocol event messages
                device0.deviceResponse += new ANT_Device.dDeviceResponseHandler(DeviceResponse);

                // Get channel from ANT device
                channel0 = device0.getChannel(USER_ANT_CHANNEL);
                // Add channel response function to receive channel event messages
                channel0.channelResponse += new dChannelResponseHandler(ChannelResponse);  

                Console.WriteLine("Initialization was successful!");
            }
            catch (Exception ex)
            {
                // Unable to connect to ANT
                // Please make sure you have an usb that is able to connect to garmin devices.
                // It is required for this project, see: https://buy.garmin.com/en-US/US/p/10997.
                if (device0 == null)
                {
                    throw new Exception("Could not connect to any device.\n" +
                    "Details: \n   " + ex.Message);
                }
                else
                {
                    throw new Exception("Error connecting to ANT: " + ex.Message);
                }
            }

            Start();
        }


        /// <summary>
        /// Start ANT+ driver and open ports for connection.
        /// </summary>
        public static void Start()
        {
            // Set to receive mode
            channelType = ANT_ReferenceLibrary.ChannelType.BASE_Slave_Receive_0x00;

            try
            {
                ConfigureANT();
            }
            catch (Exception ex)
            {
                throw new Exception("Couldn't configure ANT+: " + ex.Message + Environment.NewLine);
            }
        }

        /// <summary>
        /// Configure the ANT device.
        /// </summary>
        private static void ConfigureANT()
        {
            Console.WriteLine("Resetting module...");
            device0.ResetSystem();                 // Soft reset
            System.Threading.Thread.Sleep(500);    // Delay 500ms after a reset

            // If you call the setup functions specifying a wait time, you can check the return value for success or failure of the command
            // This function is blocking - the thread will be blocked while waiting for a response.
            // 500ms is usually a safe value to ensure you wait long enough for any response
            // If you do not specify a wait time, the command is simply sent, and you have to monitor the protocol events for the response,
            Console.WriteLine("Setting network key...");
            if (device0.setNetworkKey(USER_NETWORK_NUM, USER_NETWORK_KEY, 500))
                Console.WriteLine("Network key set");
            else
                throw new Exception("Error configuring network key");

            Console.WriteLine("Assigning channel...");
            if (channel0.assignChannel(channelType, USER_NETWORK_NUM, 500))
                Console.WriteLine("Channel assigned");
            else
                throw new Exception("Error assigning channel");

            Console.WriteLine("Setting Channel ID...");
            if (channel0.setChannelID(USER_DEVICENUM, false, USER_DEVICETYPE, USER_TRANSTYPE, 500))  // Not using pairing bit
                Console.WriteLine("Channel ID set");
            else
                throw new Exception("Error configuring Channel ID");

            Console.WriteLine("Setting Radio Frequency...");
            if (channel0.setChannelFreq(USER_RADIOFREQ, 500))
                Console.WriteLine("Radio Frequency set");
            else
                throw new Exception("Error configuring Radio Frequency");

            Console.WriteLine("Setting Channel Period...");
            if (channel0.setChannelPeriod(USER_CHANNELPERIOD, 500))
                Console.WriteLine("Channel Period set");
            else
                throw new Exception("Error configuring Channel Period");

            Console.WriteLine("Opening channel...");
            if (channel0.openChannel(500))
            {
                Console.WriteLine("Channel opened");
            }
            else
            {
                throw new Exception("Error opening channel");
            }
        }

        /// <summary>
        /// A new measurment has been recieved from the HRM.
        /// </summary>
        /// <param name="response">The response object recieved by the ANT+ driver. This contains the status of the HRM device and the value measured by the sensor.</param>
        static void ChannelResponse(ANT_Response response)
        {
            try
            {
                switch ((ANT_ReferenceLibrary.ANTMessageID)response.responseID)
                {
                    case ANT_ReferenceLibrary.ANTMessageID.RESPONSE_EVENT_0x40:
                        {
                            switch (response.getChannelEventCode())
                            {
                                case ANT_ReferenceLibrary.ANTEventID.EVENT_RX_SEARCH_TIMEOUT_0x01:
                                    {
                                        Console.WriteLine("Search Timeout");
                                        break;
                                    }
                                case ANT_ReferenceLibrary.ANTEventID.EVENT_RX_FAIL_0x02:
                                    {
                                        Console.WriteLine("Rx Fail");
                                        break;
                                    }
                                case ANT_ReferenceLibrary.ANTEventID.EVENT_TRANSFER_RX_FAILED_0x04:
                                    {
                                        Console.WriteLine("Burst receive has failed");
                                        break;
                                    }
                                case ANT_ReferenceLibrary.ANTEventID.EVENT_CHANNEL_CLOSED_0x07:
                                    {
                                        // This event should be used to determine that the channel is closed.
                                        Console.WriteLine("Channel Closed");
                                        Console.WriteLine("Unassigning Channel...");
                                        if (channel0.unassignChannel(500))
                                        {
                                            Console.WriteLine("Unassigned Channel");
                                            Console.WriteLine("Press enter to exit");
                                        }
                                        break;
                                    }
                                case ANT_ReferenceLibrary.ANTEventID.EVENT_RX_FAIL_GO_TO_SEARCH_0x08:
                                    {
                                        Console.WriteLine("Go to Search");
                                        break;
                                    }
                                case ANT_ReferenceLibrary.ANTEventID.EVENT_CHANNEL_COLLISION_0x09:
                                    {
                                        Console.WriteLine("Channel Collision");
                                        break;
                                    }
                                default:
                                    {
                                        Console.WriteLine("Unhandled Channel Event " + response.getChannelEventCode());
                                        break;
                                    }
                            }
                            break;
                        }
                    case ANT_ReferenceLibrary.ANTMessageID.BROADCAST_DATA_0x4E:
                    case ANT_ReferenceLibrary.ANTMessageID.ACKNOWLEDGED_DATA_0x4F:
                    case ANT_ReferenceLibrary.ANTMessageID.BURST_DATA_0x50:
                    case ANT_ReferenceLibrary.ANTMessageID.EXT_BROADCAST_DATA_0x5D:
                    case ANT_ReferenceLibrary.ANTMessageID.EXT_ACKNOWLEDGED_DATA_0x5E:
                    case ANT_ReferenceLibrary.ANTMessageID.EXT_BURST_DATA_0x5F:
                        {
                            // Send the recieved packet to Unity
                            AsynchronousSocketListener.Send(response.messageContents[8].ToString());

                            Console.WriteLine(response.messageContents[8]);
                            Console.Write(BitConverter.ToString(response.getDataPayload()) + Environment.NewLine);  // Display data payload

                            break;
                        }
                    default:
                        {
                            Console.WriteLine("Unknown Message " + response.responseID);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Channel response processing failed with exception: " + ex.Message);
            }
        }

        /// <summary>
        /// Called whenever a message is received from ANT unless that message is a channel event message. 
        /// </summary>
        /// <param name="response">ANT message</param>
        static void DeviceResponse(ANT_Response response)
        {
            switch ((ANT_ReferenceLibrary.ANTMessageID)response.responseID)
            {
                case ANT_ReferenceLibrary.ANTMessageID.STARTUP_MESG_0x6F:
                    {
                        Console.Write("RESET Complete, reason: ");

                        byte ucReason = response.messageContents[0];

                        if (ucReason == (byte)ANT_ReferenceLibrary.StartupMessage.RESET_POR_0x00)
                            Console.WriteLine("RESET_POR");
                        if (ucReason == (byte)ANT_ReferenceLibrary.StartupMessage.RESET_RST_0x01)
                            Console.WriteLine("RESET_RST");
                        if (ucReason == (byte)ANT_ReferenceLibrary.StartupMessage.RESET_WDT_0x02)
                            Console.WriteLine("RESET_WDT");
                        if (ucReason == (byte)ANT_ReferenceLibrary.StartupMessage.RESET_CMD_0x20)
                            Console.WriteLine("RESET_CMD");
                        if (ucReason == (byte)ANT_ReferenceLibrary.StartupMessage.RESET_SYNC_0x40)
                            Console.WriteLine("RESET_SYNC");
                        if (ucReason == (byte)ANT_ReferenceLibrary.StartupMessage.RESET_SUSPEND_0x80)
                            Console.WriteLine("RESET_SUSPEND");
                        break;
                    }
                case ANT_ReferenceLibrary.ANTMessageID.VERSION_0x3E:
                    {
                        Console.WriteLine("VERSION: " + new ASCIIEncoding().GetString(response.messageContents));
                        break;
                    }
                case ANT_ReferenceLibrary.ANTMessageID.RESPONSE_EVENT_0x40:
                    {
                        switch (response.getMessageID())
                        {
                            case ANT_ReferenceLibrary.ANTMessageID.CLOSE_CHANNEL_0x4C:
                                {
                                    if (response.getChannelEventCode() == ANT_ReferenceLibrary.ANTEventID.CHANNEL_IN_WRONG_STATE_0x15)
                                    {
                                        Console.WriteLine("Channel is already closed");
                                        Console.WriteLine("Unassigning Channel...");
                                        if (channel0.unassignChannel(500))
                                        {
                                            Console.WriteLine("Unassigned Channel");
                                            Console.WriteLine("Press enter to exit");
                                        }
                                    }
                                    break;
                                }
                            case ANT_ReferenceLibrary.ANTMessageID.NETWORK_KEY_0x46:
                            case ANT_ReferenceLibrary.ANTMessageID.ASSIGN_CHANNEL_0x42:
                            case ANT_ReferenceLibrary.ANTMessageID.CHANNEL_ID_0x51:
                            case ANT_ReferenceLibrary.ANTMessageID.CHANNEL_RADIO_FREQ_0x45:
                            case ANT_ReferenceLibrary.ANTMessageID.CHANNEL_MESG_PERIOD_0x43:
                            case ANT_ReferenceLibrary.ANTMessageID.OPEN_CHANNEL_0x4B:
                            case ANT_ReferenceLibrary.ANTMessageID.UNASSIGN_CHANNEL_0x41:
                                {
                                    if (response.getChannelEventCode() != ANT_ReferenceLibrary.ANTEventID.RESPONSE_NO_ERROR_0x00)
                                    {
                                        Console.WriteLine(String.Format("Error {0} configuring {1}", response.getChannelEventCode(), response.getMessageID()));
                                    }
                                    break;
                                }
                            case ANT_ReferenceLibrary.ANTMessageID.RX_EXT_MESGS_ENABLE_0x66:
                                {
                                    if (response.getChannelEventCode() == ANT_ReferenceLibrary.ANTEventID.INVALID_MESSAGE_0x28)
                                    {
                                        Console.WriteLine("Extended messages not supported in this ANT product");
                                        break;
                                    }
                                    else if (response.getChannelEventCode() != ANT_ReferenceLibrary.ANTEventID.RESPONSE_NO_ERROR_0x00)
                                    {
                                        Console.WriteLine(String.Format("Error {0} configuring {1}", response.getChannelEventCode(), response.getMessageID()));
                                        break;
                                    }
                                    Console.WriteLine("Extended messages enabled");
                                    break;
                                }
                            case ANT_ReferenceLibrary.ANTMessageID.REQUEST_0x4D:
                                {
                                    if (response.getChannelEventCode() == ANT_ReferenceLibrary.ANTEventID.INVALID_MESSAGE_0x28)
                                    {
                                        Console.WriteLine("Requested message not supported in this ANT product");
                                        break;
                                    }
                                    break;
                                }
                            default:
                                {
                                    Console.WriteLine("Unhandled response " + response.getChannelEventCode() + " to message " + response.getMessageID()); break;
                                }
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Shut down the ant device connection.
        /// </summary>
        public static void Shutdown()
        {
            // Clean up ANT
            Console.WriteLine("Disconnecting module...");
            ANT_Device.shutdownDeviceInstance(ref device0);  // Close down the device completely and completely shut down all communication
            Console.WriteLine("Demo has completed successfully!");
        }
    }
}
