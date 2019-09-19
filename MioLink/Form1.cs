using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Threading;
using SocketInterface;
using ANT;

namespace MioLink
{
    /// <summary>
    /// Basic C# Form which will display server state.
    /// </summary>
    public partial class Form1 : Form
    {
        Thread childThread;     ///< Create thread for ANT+ connection

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize socket nd ANT+ device listener.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                AsynchronousSocketListener.StartListening();
                ThreadStart childref = new ThreadStart(ANTInterface.Init);
                Console.WriteLine("In Main: Creating the Child thread");
                childThread = new Thread(childref);
                childThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Setting up failed with exception: \n" + ex.Message);
            }
        }

        /// <summary>
        /// Shutdown all processes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Closing(object sender, CancelEventArgs e)
        {
            // Close HRM device connection
            childThread.Abort();
            // Stop listening
            AsynchronousSocketListener.Shutdown();
            // Clean up ANT
            ANTInterface.Shutdown();
        }
    }
}
