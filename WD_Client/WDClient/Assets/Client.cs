using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;

//  TODO
public class Client : MonoBehaviour {

    //Variables
    string ip = "127.0.0.1";
    //string ip = "40.78.157.137";
    int port = 25001;

    private UdpClient udpClient = new UdpClient();

    private delegate void EventDelegate(ListeningEventArgs e);



    // Empty default constructer
    private Client() { }

    // TODO
    private static Client instance;

    // TODO
    public static Client Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new Client();
            }

            return instance;
        }
    } 

    // Use this for initialization
    void Start () {
        Thread listeningThread = new Thread(() => this.ReceiveInstruction());
        listeningThread.Start();
    }
	
	// Update is called once per frame
	//void Update () {
	
	//}

    // Connect to server
    public void Connect()
    {
        try {
            Client.instance.udpClient.Connect(ip, port);

            byte[] connectionMsg = System.Text.Encoding.ASCII.GetBytes("Establishing connection");
            Client.instance.udpClient.Send(connectionMsg, 1024);

            Thread heartbeatThread = new Thread(() => this.HeartbeatFunction());
            heartbeatThread.Start();
        }
        catch (System.Exception e)
        {
            System.Console.WriteLine("Failed connection: " + e.ToString());
        }
    }

    // Request instruction from server
    public void SendInstruction(string instruction)
    {
        try
        {
            udpClient.Connect(ip, port);

            // Sends a message to the host to which you have connected.
            byte[] sendBytes = System.Text.Encoding.ASCII.GetBytes(instruction);

            Client.instance.udpClient.Send(sendBytes, sendBytes.Length);

        }
        catch (System.Exception e)
        {
            System.Console.WriteLine(e.ToString());
        }
    }

    // Receive instruction from 
    public void ReceiveInstruction()
    {
        while (true)
        {
            //IPEndPoint object will allow us to read datagrams sent from any source.
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, port);

            // Blocks until a message returns on this socket from a remote host.
            byte[] receiveBytes = Client.instance.udpClient.Receive(ref RemoteIpEndPoint);
            string returnData = System.Text.Encoding.ASCII.GetString(receiveBytes);

            // Uses the IPEndPoint object to determine which of these two hosts responded.
            System.Console.WriteLine("This is the message you received " +
                                            returnData.ToString());
            System.Console.WriteLine("This message was sent from " +
                                        RemoteIpEndPoint.Address.ToString() +
                                        " on their port number " +
                                        RemoteIpEndPoint.Port.ToString());

            //udpClient.Close();
        }
    }

    private void HeartbeatFunction()
    {
        while (true)
        {
            Thread.Sleep(5000);

            try
            {
                Client.instance.udpClient.Connect(ip, port);

                byte[] connectionMsg = System.Text.Encoding.ASCII.GetBytes("Boop");
                Client.instance.udpClient.Send(connectionMsg, 1024);
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine("Sending heartbeat failed: " + e.ToString());
            }
        }
    }

    public class ListeningEventArgs : System.EventArgs {}
}
