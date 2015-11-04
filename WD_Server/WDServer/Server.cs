using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using WindowsDefender_WebApp;

namespace WDServer
{
    class Server
    { 
        // Set this to false for Release builds so we aren't wasting cycles by printing to the console
        public static bool DEBUG = true;

        private int _port = 4568;
        public static int CLIENT_TIMEOUT = 15; // Seconds

        private Socket socket = null;
        private IPAddress ip = null;
        private IPEndPoint localIpEndPoint = null;
        
        private ConcurrentDictionary<string, User> _users = new ConcurrentDictionary<string, User>(); // Keys are IP address' of users
        private ConcurrentDictionary<string, Match> _matches = new ConcurrentDictionary<string, Match>();

        private Thread _checkForTimeouts;

        /// <summary>
        /// Entry point
        /// </summary>
        public Server()
        {
            // Initialize server
            Init();

            // Create thread that continuously checks for timed out users
            _checkForTimeouts = new Thread(new ThreadStart(HeartBeatsThread));
            _checkForTimeouts.Start();

            // Start listening
            Listen();
        }

        /// <summary>
        /// Initializes the server socket and tells the user if everything worked
        /// </summary>
        private void Init()
        {
            try
            {
                Console.WriteLine("  _____                     ____      ___           _       ");
                Console.WriteLine(" |_   _|___ _ _ _ ___ ___  |    \\ ___|  _|___ _____| |___ ___");
                Console.WriteLine("   | | | . | | | | -_|  _| |  |  | -_|  _| -_|   | . | -_|  _|");
                Console.WriteLine("   |_| |___|_____|___|_|   |____/|___|_| |___|_|_|___|___|_|");
                ip = Dns.GetHostEntry("localhost").AddressList[0];
                socket = new Socket(ip.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                localIpEndPoint = new IPEndPoint(ip, _port);
                socket.Bind(localIpEndPoint);
                Console.WriteLine("\n Server started using port " + _port + "\n\n");
            }
            catch (SocketException se)
            {
                Console.WriteLine("Error starting server: " + se.ToString());
                Console.WriteLine("Press enter to quit.");
                Console.In.Read();
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Listens for packets and responds accordingly by passing
        /// the incoming data to other functions
        /// </summary>
        private void Listen()
        {
            while (true)
            {
                try
                {
                    byte[] received = new byte[256];
                    IPEndPoint ipEndPoint = new IPEndPoint(ip, _port);
                    EndPoint remoteEndPoint = (ipEndPoint);
                    int bytesReceived = socket.ReceiveFrom(received, ref remoteEndPoint);
                    string dataReceived = Encoding.ASCII.GetString(received);
                    string ipAddress = ipEndPoint.Address.ToString();

                    // Debug
                    if (DEBUG)
                        Console.WriteLine("Received: " + dataReceived + " from ip: " + ipAddress);

                    // Parse command and arguments
                    Instruction instruction = (Instruction)JsonConvert.DeserializeObject(dataReceived);

                    User user;
                    switch (instruction.Command)
                    {
                        case "J": // JOIN
                            user = new User(remoteEndPoint);
                            _users.TryAdd(ipAddress, user);
                            // JoinMatch(user); TODO JOIN USER'S MATCH
                            break;

                        case "H": // HEARTBEAT (resets client timeout)
                            _users.TryGetValue(ipAddress, out user);
                            user?.ResetTimeout();
                            break;

                        case "M": // MESSAGE (general command for everything else)
                            _users.TryGetValue(ipAddress, out user);
                            SendToMatch(user, instruction);
                            break;
                    }
                }
                catch (Exception)
                {
                    // Do nothing if an error occurred - just continue with next request.
                }
            }
        }

        /// <summary>
        /// Sends to all users in a match
        /// </summary>
        /// <param name="user"></param>
        /// <param name="instruction"></param>
        private void SendToMatch(User user, Instruction instruction)
        {
            // TODO
            // Find match
            // Send instruction to everyone in match

            foreach (KeyValuePair<string, User> u in _users) // This needs to be users in a match instead
            {
                byte[] returningByte = Encoding.ASCII.GetBytes(instruction.ToString().ToCharArray()); // Is ToCharArray necessary here?
                socket.SendTo(returningByte, u.Value.EndPoint);
            }
        }

        /// <summary>
        /// Sends to all users in a match except the current user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="instruction"></param>
        private void SendToMatchExcept(User user, Instruction instruction)
        {
            // TODO
            // Find match
            // Send instruction to everyone in match except current user
        }

        /// <summary>
        /// If we haven't received messages from users for awhile,
        /// we consider them disconnected and they can be removed
        /// from any match they were a part of.
        /// </summary>
        private void HeartBeatsThread()
        {
            while (true)
            {
                foreach (KeyValuePair<string, User> user in _users)
                {
                    user.Value.IncrementTimeout();
                    if (user.Value.IsConnectionExpired())
                        DisconnectUser(user.Key);
                }
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Disconnects a user based on their ip address
        /// </summary>
        /// <param name="ipaddress"></param>
        private void DisconnectUser(string ipaddress)
        {
            User dummy;
            _users.TryRemove(ipaddress, out dummy);
            // TODO - CHECK IF USER WAS LAST PERSON IN HIS/HER MATCH. IF SO, REMOVE THE MATCH FROM _matches
        }
    }
}
