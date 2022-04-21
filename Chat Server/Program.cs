#region Using Declarations
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml.Serialization;
#endregion

namespace Chat_Server
{
    public class Program
    {
        #region Global Variable Declarations
        // Increment this by one per major revision
        public static string revisionNumber = "8";
        public static string[] illegalUsernameCharacters = {"#", " ", "!"};
        public static List<User> connectedUsers = new List<User>();
        public static List<Profile> profileList = new List<Profile>();
        public static ServerSettings serverSettings = new ServerSettings();
        public static List<string> bannedIPs = new List<string>();
        #endregion

        static void Main(string[] args)
        {
            // Beginning message and infringement warning
            //Console.WriteLine("Developed by Jesse Wells - 2016 Revision " + revisionNumber + " Edition 1.0\r\n\r\n---THIS PROGRAM IS NOT TO BE DECOMPILED OR OTHERWISE INFRINGED UPON---\r\n");

            // XML processes
            XML_Functions.initializeSettingsFromXML();
            Console.WriteLine("Server settings initialized.");

            XML_Functions.initializeProfilesFromXML();
            Console.WriteLine("Profiles initialized to volatile profile list.");

            XML_Functions.initializeBannedIPsFromXML();

            // Main server and client sockets
            TcpListener serverSocket = new TcpListener(3400);
            try
            {
                serverSocket = new TcpListener(serverSettings.Port);
                if (serverSettings.Port != 3400)
                {
                    Console.WriteLine("Server socket set to port " + serverSettings.Port.ToString() + " rather than the default 3400.");
                }
            } catch
            {
                Console.WriteLine("Port failed to initialize from settings! Defaulting to 3400...");
                serverSocket = new TcpListener(3400);
            }
            
            TcpClient clientSocket = default(TcpClient);
            serverSocket.Start();

            // Apply settings
            Console.Title = serverSettings.ServerName;

            // Console command thread
            Thread commandThread = new Thread(listenForConsoleInput);
            commandThread.Start();

            // Final ready print before beginning server loop
            Console.WriteLine("Ready...");

            while (true)
            {
                // This only runs when a new client connects
                clientSocket = serverSocket.AcceptTcpClient();
                byte[] bytesFrom = new byte[4096];
                string dataFromClient = null;

                // Receive data
                NetworkStream serverStream = clientSocket.GetStream();
                clientSocket.ReceiveBufferSize = 4096;
                serverStream.Read(bytesFrom, 0, (int)clientSocket.ReceiveBufferSize);
                dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom);

                // Clean up received data
                dataFromClient = dataFromClient.Replace("\0", "");
                dataFromClient = dataFromClient.Replace("\r\n", "");

                if (connectedUsers.Count < serverSettings.UserLimit)
                {
                    if (Return_Methods.checkForMultipleDollarSigns(dataFromClient))
                    {
                        // Multiple dollar sings in username
                        Broadcasting.specificBroadcast(dataFromClient, "Invalid username.", false, true);
                    }
                    else
                    {
                        bool goodDecryption = true;

                        try
                        {
                            dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf("$"));
                            bool usernameAvailable = true;
                            bool serverPasswordValid = true;

                            // Check for server password
                            if (!String.IsNullOrWhiteSpace(serverSettings.Password))
                            {
                                // A password was found from settings
                                try
                                {
                                    dataFromClient = AES.Decrypt(dataFromClient, serverSettings.Password);
                                    string password = Return_Methods.getNthParameter(dataFromClient, 1, true);

                                    if (!String.IsNullOrWhiteSpace(serverSettings.Password) & !dataFromClient.Contains(" "))
                                    {
                                        // There is a password and the user's data does not have a space
                                        goodDecryption = false;
                                    }
                                    else
                                    {
                                        dataFromClient = dataFromClient.Replace(" " + password, "");
                                    }

                                    try
                                    {
                                        // This checks to make sure that the given password actually matches the server password
                                        if (goodDecryption)
                                        {
                                            // Action only needed if the password is invalid; the default value of the connection boolean is true
                                            if (password != serverSettings.Password)
                                            {
                                                // Disconnect client when password is incorrect
                                                goodDecryption = false;
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        // Failed to parse; bad password
                                        goodDecryption = false;
                                    }
                                } catch
                                {
                                    goodDecryption = false;
                                }
                                
                                if (!goodDecryption)
                                {
                                    Broadcasting.broadcastToSocket("Invalid password.", clientSocket, true, false);
                                    clientSocket.Close();
                                    serverPasswordValid = false;
                                }
                            }

                            if (serverPasswordValid) {
                                foreach (User user in connectedUsers)
                                {
                                    if (user.Username == dataFromClient)
                                    {
                                        usernameAvailable = false;
                                        break;
                                    }
                                }

                                if (usernameAvailable & Return_Methods.usernameValid(dataFromClient))
                                {
                                    // Main user objct
                                    User user = new User();

                                    // Booleans to be used later
                                    bool passwordValid = true;
                                    bool userInitialized = false;

                                    // Find the profile that matches the user, if there is one
                                    foreach (Profile profile in profileList)
                                    {
                                        if (profile.Username == dataFromClient)
                                        {
                                            if (String.IsNullOrWhiteSpace(profile.Password))
                                            {
                                                // No password
                                                user.PowerLevel = profile.PowerLevel;
                                                user.IsBanned = profile.IsBanned;
                                                user.IsShredded = profile.IsShredded;
                                                user.BanLength = profile.BanLength;
                                                user.KnownIPs = profile.KnownIPs;
                                                userInitialized = true;
                                                break;
                                            }
                                            else
                                            {
                                                // Password found - check for validity
                                                try
                                                {
                                                    // Get response from socket manually and decrypt the given password
                                                    string encryptedPassword = Return_Methods.receiveResponseFromSocket(clientSocket);
                                                    string decryptedPassword = AES.Decrypt(encryptedPassword, serverSettings.Password);

                                                    // Initialize profile
                                                    user.Password = profile.Password;
                                                    user.PowerLevel = profile.PowerLevel;
                                                    user.IsBanned = profile.IsBanned;
                                                    user.IsShredded = profile.IsShredded;
                                                    user.BanLength = profile.BanLength;
                                                    user.KnownIPs = profile.KnownIPs;

                                                    // Initialized, change boolean
                                                    userInitialized = true;

                                                    // Somewhat complicated piece of code - the user's retrieved password is encrypted in the xml, so it needs to be decrypted. It can only be decrypted by its unencrypted form, something only the user who claimed this profile knows.
                                                    // Not even the owner or an admin with physical access can see the password, as they are stored as hashes. This decrypts the password and checks to see if it is equal to what was given.
                                                    if (!(AES.Decrypt(user.Password, decryptedPassword) == decryptedPassword))
                                                    {
                                                        // The passwordValid variables is true by default, therefore action would only be needed if the password were wrong
                                                        passwordValid = false;
                                                    }
                                                } catch (Exception exce)
                                                {
                                                    // Password failed to decrypt - most likely invalid key
                                                    passwordValid = false;
                                                }
                                                break;
                                            }
                                        }
                                    }

                                    // The passwordValid variable is true by default; it is only changed with an incorrect password
                                    if (passwordValid)
                                    {
                                        // Only executes if the user's profile was not found
                                        if (!userInitialized)
                                        {
                                            // No profile found, creating default user
                                            user.PowerLevel = 0;
                                            user.IsBanned = false;
                                            user.IsShredded = false;
                                            user.BanLength = 0;
                                            user.KnownIPs = "";
                                        }

                                        // Set username and TcpClient to main user object
                                        user.Username = dataFromClient;
                                        user.TcpClient = clientSocket;

                                        setUser(user);

                                        // Get Epoch Time
                                        TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                                        int secondsSinceEpoch = (int)t.TotalSeconds;

                                        // Get IP of client
                                        string IP = ((IPEndPoint)clientSocket.Client.RemoteEndPoint).Address.ToString();
                                        List<string> tempIPs = Return_Methods.returnIPList(user.KnownIPs);

                                        // Checks if the ip is banned and not in the user's known IPs - this indicates a new user off a banned network
                                        // Had this not been patched someone could simply use a different username and connect
                                        if (bannedIPs.Contains(IP) & !(tempIPs.Contains(IP)))
                                        {
                                            // User connected from an IP other than the one that was banned
                                            Broadcasting.specificBroadcast(user.Username, "Your IP is banned. Connect from the username you were banned on.", false, true);
                                            flushOutClient(user.Username, false);
                                        } else if (bannedIPs.Contains(IP) & !user.IsBanned)
                                        {
                                            // The IP is banned, but the user is not - this is logically impossible as the IP is unbanned when the user is
                                            Broadcasting.specificBroadcast(user.Username, "Either another user of your network has gotten your IP banned or your network is blacklisted. Connect from the username you were banned from if you were banned by an admin. Otherwise, contact the server owner and ask to be unbanned.", false, true);
                                            flushOutClient(user.Username, false);
                                        }
                                        else
                                        {
                                            // Check to see if their ban has expired
                                            if (user.IsBanned)
                                            {
                                                if (user.BanLength < 0)
                                                {
                                                    // Any negative numbers denote a permaban
                                                    Broadcasting.specificBroadcast(user.Username, "You are permabanned! Whatever you did, you probably deserved it.", false, false);
                                                }
                                                else if (user.BanLength < secondsSinceEpoch)
                                                {
                                                    // The user's ban has expired and will be reset to the unbanned values
                                                    user.IsBanned = false;
                                                    user.BanLength = 0;

                                                    // Remove the user's details from the ban data
                                                    foreach (Profile item in profileList)
                                                    {
                                                        if (item.Username == user.Username)
                                                        {
                                                            foreach (string element in tempIPs)
                                                            {
                                                                if (bannedIPs.Contains(element))
                                                                {
                                                                    bannedIPs.Remove(element);
                                                                }
                                                            }

                                                            XML_Functions.setBannedIPsToXML();
                                                        }
                                                    }
                                                }
                                            }

                                            // Same evaluation done twice because it is potentially modified in first statement
                                            if (user.IsBanned)
                                            {
                                                if (!(user.BanLength <= 0))
                                                {
                                                    // Only executs if the user is banned and is not 0 (not banned) or any negative number (permabanned) - this denotes a normal ban with a timeout that HASN'T expired
                                                    // The user's ban would've been removed in the above statement had it expired and this would not execute as user.IsBanned would be false
                                                    Broadcasting.specificBroadcast(user.Username, "You are banned until " + user.BanLength.ToString() + " unix epoch time. Shame if you can't convert from epoch time; you deserve to not know.", false, true);
                                                }

                                                // Be gone!
                                                flushOutClient(user.Username, false);
                                            }
                                            else
                                            {
                                                // Log IP
                                                if (!user.KnownIPs.Contains(IP))
                                                {
                                                    // User has connected from new IP
                                                    user.KnownIPs += (IP + ";");
                                                }

                                                // The user connects here
                                                setUser(user);

                                                // Send user MOTD and broadcast to the server that a new user has connected
                                                Broadcasting.specificBroadcast(user.Username, serverSettings.MOTD, false, false);
                                                Broadcasting.broadcast(dataFromClient + " has connected.", "", true);
                                                Console.WriteLine(dataFromClient + " joined chat room.");

                                                // Start client thread
                                                Client_Routine clientRoutine = new Client_Routine();
                                                clientRoutine.startClient(user);
                                            }
                                        }
                                    } else {
                                        // User's username was passworded and the given password was invalid
                                        Broadcasting.broadcastToSocket("Invalid password.", clientSocket, true, false);
                                        clientSocket.Close();
                                    }
                                }
                                else
                                {
                                    // Invalid username - contained illegal characters, had a space, etc.
                                    Broadcasting.broadcastToSocket("Invalid username.", clientSocket, true, false);
                                    clientSocket.Close();
                                }
                            } else
                            {
                                Broadcasting.broadcastToSocket("Invalid server password.", clientSocket, true, false);
                                clientSocket.Close();
                            }
                        }
                        catch (Exception exc)
                        {
                            // Make sure a bad server password won't log to the console
                            if (goodDecryption)
                            {
                                // The most likely error was no $ was found and therefore could not be made a substring
                                // This usually indicates an outside source (not the client) pinged the server
                                Console.WriteLine("An abnormal connection was received from " + ((IPEndPoint)clientSocket.Client.RemoteEndPoint).Address.ToString() + " and will be closed.");
                                clientSocket.Close();
                                Console.WriteLine("\r\n---BEGIN DEBUG DUMP---\r\n\r\n" + exc.ToString());
                                // NOTICE There is a small chance this running is an indication of a larger problem
                            }
                        }
                    }
                } else {
                    // Server is full
                    Broadcasting.broadcastToSocket("Server full.", clientSocket, true, false);
                    clientSocket.Close();
                }
            }
        }

        #region Class Constructors
        // The ServerSettings class is serialized to XML and used to store, well, server settings
        public class ServerSettings
        {
            // The properties are self expanatory
            public string ServerName { get; set; }
            public string Password { get; set; }
            public int Port { get; set; }
            public string MOTD { get; set; }
            public int MessageCap { get; set; }
            public int MaxSpamCap { get; set; }
            public int UserLimit { get; set; }
            public bool KeepConsoleLog { get; set; }
            // Hyperactive logging is not implemented - it is meant to increase what is logged
            public bool HyperactiveLogging { get; set; }

            // Null property initializer
            public ServerSettings() { }
        }

        // Commands are stored in this class - couldn't use a dictionary as there were more than three properties
        public class Command
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public int RequiredPowerLevel { get; set; }

            public Command() { }

            public Command(string Name, string Description, int RequiredPowerLevel)
            {
                this.Name = Name;
                this.Description = Description;
                this.RequiredPowerLevel = RequiredPowerLevel;
            }
        }

        /// <summary>
        /// It is somewhat difficult to explain why there is a "User" and a "Profile"
        /// Users have attributes that are not serializable, like the TcpClient and Thread
        /// Having a profiles class allows me to handpick what I want serialized
        /// If I serialize users with ignored attributes, it will be difficult to initialize them from the XML
        /// (connected users is also a list of connected users exlusively, where profile list is a list of profiles in the server)
        /// </summary>

        public class User
        {
            // Self-explanatory
            public string Username { get; set; }
            public string Password { get; set; }
            public int PowerLevel { get; set; }
            public bool IsBanned { get; set; }
            public bool IsShredded { get; set; }
            public bool IsGagged { get; set; }
            public int BanLength { get; set; }
            public string KnownIPs { get; set; }

            [XmlIgnoreAttribute]
            public TcpClient TcpClient { get; set; }
            [XmlIgnoreAttribute]
            public Thread Thread { get; set; }

            public User() { }

            public User(string Username, string Password, int PowerLevel, bool IsBanned, bool IsShredded, bool IsGagged, TcpClient TcpClient, Thread Thread, string KnownIPs)
            {
                this.Username = Username;
                this.Password = Password;
                this.PowerLevel = PowerLevel;
                this.IsBanned = IsBanned;
                this.IsShredded = IsShredded;
                this.TcpClient = TcpClient;
                this.Thread = Thread;
                this.IsGagged = IsGagged;
                this.KnownIPs = KnownIPs;
            }
        }

        public class Profile
        {
            // Self-explanatory
            public string Username { get; set; }
            public string Password { get; set; }
            public int PowerLevel { get; set; }
            public bool IsBanned { get; set; }
            public bool IsShredded { get; set; }
            public bool IsGagged { get; set; }
            public int BanLength { get; set; }
            public string KnownIPs { get; set; }

            public Profile() { }

            public Profile(string Username, string Password, int PowerLevel, bool IsBanned, bool IsShredded, bool IsGagged, string KnownIPs)
            {
                this.Username = Username;
                this.Password = Password;
                this.PowerLevel = PowerLevel;
                this.IsBanned = IsBanned;
                this.IsShredded = IsShredded;
                this.IsGagged = IsGagged;
                this.KnownIPs = KnownIPs;
            }
        }
        #endregion

        #region Methods
        // This method with default all the servers settings and set them to the XML file that stores settings
        public static void defaultServerSettings()
        {
            // These are all the default values
            serverSettings.ServerName = "Chat Server";
            serverSettings.Password = "";
            serverSettings.Port = 3400;
            serverSettings.MOTD = "A chat server.";
            serverSettings.MessageCap = 500;
            serverSettings.MaxSpamCap = 8;
            serverSettings.UserLimit = 20;
            serverSettings.KeepConsoleLog = false;
            serverSettings.HyperactiveLogging = false;

            // Persist volatile settings
            XML_Functions.setSettingsToXML();

            // Set console title to the volatile setting
            Console.Title = serverSettings.ServerName;
        }

        // This method is the main method used to disconnect clients - the only exception is TcpClients that haven't been put into the connected users list and must be disconnected manually (TcpClient.Close())
        public static void flushOutClient(string username, bool logAndBroadcast)
        {
            // Finds user by username
            foreach (User user in connectedUsers)
            {
                if (user.Username == username)
                {
                    // Flush the client and remove it from connectUsers
                    TcpClient clientToFlush = user.TcpClient;
                    clientToFlush.Close();
                    connectedUsers.Remove(user);

                    // Flag can be passed to log to the console
                    if (logAndBroadcast)
                    {
                        Broadcasting.broadcast(username + " has disconnected.", "", true);
                        Console.WriteLine(username + " has disconnected.");
                    }
                    break;
                }
            }
        }

        // This is a little used method - it essentially binds a user to the connected users and sets their properties to the XML file that holds profiles
        public static void setUser(User user)
        {
            bool userFound = false;
            foreach (User item in connectedUsers.ToArray())
            {
                if (user.Username == item.Username)
                {
                    // Found the given user in volatile data list
                    connectedUsers[connectedUsers.IndexOf(item)] = user;
                    XML_Functions.setAndReadProfiles(false);
                    userFound = true;
                }
            }

            if (!userFound)
            {
                // The user is not already connected and therefore needs to be added manually
                connectedUsers.Add(user);
                XML_Functions.setAndReadProfiles(false);
            }
        }
        #endregion

        // This is the method that runs on a separate thread and waits for commands typed in the console
        public static void listenForConsoleInput()
        {
            while (true)
            {
                string input = Console.ReadLine();
                try
                {
                    if (input[0] == '!')
                    {
                        // Creates a user named "#" with a power level of 100 - # is an illegal character in usernames and denotes the console
                        User console = new User("#", "", 100, false, false, false, null, null, null);
                        Command_Handler.executeCommands(input, console);
                    }
                    else
                    {
                        // The only thing that could be typed into the console is a command, and all commands begin with a "!" - this runs if a command was not entered
                        Console.WriteLine("Command unrecognized.");
                    }
                }
                catch (Exception ex)
                {
                    // Just an overall catch - haven't run into problems with an error here
                    Console.WriteLine("Syntax invalid or command unrecognized.");
                }
            }
        }
    }
}