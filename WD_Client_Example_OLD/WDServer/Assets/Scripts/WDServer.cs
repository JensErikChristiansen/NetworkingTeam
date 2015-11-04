using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using System.Text;

public class WDServer : MonoBehaviour
{

    void Start () {
        Debug.Log("test");

        int port = 4023;
        UdpClient udpClient = new UdpClient(port);
        try
        {
            udpClient.Connect("142.232.18.112", port);

            // Sends a message to the host to which you have connected.
            Byte[] sendBytes = Encoding.ASCII.GetBytes("Is anybody there?");

            udpClient.Send(sendBytes, sendBytes.Length);

            // Sends a message to a different host using optional hostname and port parameters.
            UdpClient udpClientB = new UdpClient();
            udpClientB.Send(sendBytes, sendBytes.Length, "AlternateHostMachineName", port);

            //IPEndPoint object will allow us to read datagrams sent from any source.
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

            // Blocks until a message returns on this socket from a remote host.
            Byte[] receiveBytes = udpClient.Receive(ref RemoteIpEndPoint);
            string returnData = Encoding.ASCII.GetString(receiveBytes);

            // Uses the IPEndPoint object to determine which of these two hosts responded.
            Console.WriteLine("This is the message you received " +
                                         returnData.ToString());
            Console.WriteLine("This message was sent from " +
                                        RemoteIpEndPoint.Address.ToString() +
                                        " on their port number " +
                                        RemoteIpEndPoint.Port.ToString());

            udpClient.Close();
            udpClientB.Close();

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }


        /*
        int myReliableChannelId; // above Start()
        myReliableChannelId = config.AddChannel(QosType.Reliable);
        int maxConnections = 10;
        HostTopology topology = new HostTopology(config, maxConnections);

        int socketId; // above Start()
        int socketPort = 8888; // Also a class member variable
        socketId = NetworkTransport.AddHost(topology, socketPort);
        Debug.Log("Socket Open. SocketId is: " + socketId);
       

        int myReliableChannelId; // above Start()
        myReliableChannelId = config.AddChannel(QosType.Reliable);
        int maxConnections = 10;
        HostTopology topology = new HostTopology(config, maxConnections);

        int socketId;
        int socketPort = 4023;
        byte error;
        socketId = NetworkTransport.AddHost(topology, socketPort);
        var connectionId = NetworkTransport.Connect(socketId, "127.0.0.1", socketPort, 0, out error);
        
        byte[] buffer = new byte[1024];
        Stream stream = new MemoryStream(buffer);
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.Serialize(stream, "HelloServer");

        int bufferSize = 1024;
        NetworkTransport.Send(socketId, connectionId, myReliableChannelId, buffer, bufferSize, out error);
 */
    }


}