using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using WindowsDefender_WebApp;

namespace WDServer
{
    class Server
    { 
        // Set this to false for Release builds so we aren't wasting cycles by printing to the console
        public static bool DEBUG = true;

        private int _port = 25001;
        public static int CLIENT_TIMEOUT = 15; // Seconds

        private Socket socket = null;
        private IPAddress ip = null;
        private IPEndPoint localIpEndPoint = null;
        
        private ConcurrentDictionary<string, User> _users = new ConcurrentDictionary<string, User>(); // Keys are IP address' of users
        private ConcurrentDictionary<string, Match> _matches = new ConcurrentDictionary<string, Match>();

        private Thread _checkForTimeouts;

        public enum InstructionType { JOIN, CMD };

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
                    string dataReceived = Encoding.ASCII.GetString(received).TrimEnd('\0');
                    string ipAddress = ipEndPoint.Address.ToString();

                    // Debug
                    if (DEBUG)
                        Console.WriteLine("Received: " + dataReceived + " from ip: " + ipAddress);

                    // Parse command and arguments
                    Instruction instruction = (Instruction)JsonConvert.DeserializeObject(dataReceived);

                    // Reset the user's timeout counter
                    ResetUserTimeout(ipAddress);

                    // Parse instruction
                    switch (instruction.Command)
                    {
                        case InstructionType.JOIN:
                            OnJoin(remoteEndPoint, ipAddress, instruction);
                            break;

                        case InstructionType.CMD:
                            OnCommand(ipAddress, instruction);
                            break;

                        default:
                            if (DEBUG)
                            {
                                // Just echo back (for testing)
                                byte[] returningByte = Encoding.ASCII.GetBytes(dataReceived);
                                socket.SendTo(returningByte, remoteEndPoint);
                            }
                            break;
                    }
                }
                catch (Exception)
                {
                    // Do nothing if an error occurred - just continue with next request.
                }
            }
        }

        private void OnJoin(EndPoint remoteEndPoint, string ipAddress, Instruction instruction)
        {
            User user;
            user = new User(remoteEndPoint);

            // Set username
            user.Username = instruction.Arg1;

            // Set match id
            user.MatchId = instruction.Arg2;

            // Add user
            _users.TryAdd(ipAddress, user);

            // Find the user's match and add him/her to it
            FindMatch(user);
        }

        private void FindMatch(User user)
        {
            // TODO
        }

        private void ResetUserTimeout(string ipAddress)
        {
            User user;
            _users.TryGetValue(ipAddress, out user);
            user?.ResetTimeout();
        }

        private void OnCommand(string ipAddress, Instruction instruction)
        {
            User user;

            if (!_users.TryGetValue(ipAddress, out user))
            {
                return;
            }

            SendToMatch(user, instruction);
        }

        /// <summary>
        /// Sends to all users in a match
        /// </summary>
        /// <param name="user"></param>
        /// <param name="instruction"></param>
        private void SendToMatch(User user, Instruction instruction)
        {
            byte[] bytes = ObjectToByteArray(instruction);
            socket.SendTo(bytes, user.EndPoint);

            // TODO
            // Find match
            // Send instruction to everyone in match

            foreach (KeyValuePair<string, User> u in _users) // This needs to be users in a match instead
            {
                //byte[] returningByte = Encoding.ASCII.GetBytes(instruction.ToString().ToCharArray()); // Is ToCharArray necessary here?
                //socket.SendTo(returningByte, u.Value.EndPoint);
            }
        }

        /// <summary>
        /// Taken from
        /// https://stackoverflow.com/questions/1446547/how-to-convert-an-object-to-a-byte-array-in-c-sharp
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] ObjectToByteArray(object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
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
            // Remove user from match
            //todo

            // Remove user
            User dummy;
            _users.TryRemove(ipaddress, out dummy);
            // TODO - CHECK IF USER WAS LAST PERSON IN HIS/HER MATCH. IF SO, REMOVE THE MATCH FROM _matches
        }
    }
}
