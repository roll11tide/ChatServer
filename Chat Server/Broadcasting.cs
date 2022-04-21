using System;
using System.Net.Sockets;
using System.Text;

namespace Chat_Server
{
    class Broadcasting
    {
        // Used to broadcast to the entire server
        // The flag isServerMessage, when true, simple removes the username and colon from the beginning of the message
        public static void broadcast(string message, string uName, bool isServerMessage)
        {
            // Goes through all connected users
            foreach (Program.User user in Program.connectedUsers)
            {
                try
                {
                    if (user.Username != "#")
                    {
                        // Trying to broadcast to the console (#) causes an error
                        // Main client socket (keep in mind this loops through every connected user)
                        TcpClient broadcastSocket = user.TcpClient;

                        // Retrieve client's stream
                        NetworkStream broadcastStream = broadcastSocket.GetStream();

                        // Retrieve stream's buffer size
                        Byte[] broadcastBytes = new byte[broadcastSocket.ReceiveBufferSize];

                        // Clean up data
                        message = message.Replace("\r\n", "");
                        message = message.Replace("$", "(DOLLARSIGN)");

                        // dataPackage is what everything is added into, then it is later encrypted and sent (this makes things cleaner and easier than sending in one big line)
                        string dataPackage = message;

                        if (!isServerMessage)
                        {
                            // User message - the console will see the message regardless, so an identifier only needs to be added if it is a user
                            dataPackage = uName + ": " + message;
                        }

                        // There is a server password - the encryption must be done right before the message is sent and cannot have things added to it after it is encrypted or else it won't decrypt correctly
                        if (!String.IsNullOrWhiteSpace(Program.serverSettings.Password))
                        {
                            // Encrypt the dataPackage
                            dataPackage = AES.Encrypt(dataPackage, Program.serverSettings.Password);
                        }

                        // Turn dataPackge to bytes
                        broadcastBytes = Encoding.ASCII.GetBytes(dataPackage);

                        // Write it to client socket
                        broadcastStream.Write(broadcastBytes, 0, broadcastBytes.Length);

                        // Flush stream so that it doesn't cause a problem later
                        broadcastStream.Flush();
                    }
                }
                catch (Exception brdexc)
                {
                    // This shouldn't really happen
                    Console.WriteLine("CATASTROPHIC FAILURE - Could not broadcast.\r\n---BEGIN DEBUG DUMP---" + brdexc.ToString());
                }
            }
        }

        // specificBroadcast is just that, it sends a message to a specified user
        // The flag logToConsole does just as it says and logs the message to the console
        // The flag requestDisconnect informs the client that its socket has been closed so it may close its socket instead of timing out from null characters
        public static void specificBroadcast(string username, string message, bool logToConsole, bool requestDisconnect)
        {
            // The only user with a username of # is the console
            if (username == "#")
            {
                // The user is the console
                Console.WriteLine(message);
            } else
            {
                // An actual user
                foreach (Program.User user in Program.connectedUsers)
                {
                    // Find user by username
                    if (user.Username == username)
                    {
                        // Client socket
                        TcpClient broadcastSocket = user.TcpClient;

                        // Socket stream
                        NetworkStream broadcastStream = broadcastSocket.GetStream();

                        // Adds disconnection sequence if specified
                        if (requestDisconnect)
                        {
                            // The three dollar signs is simply a commodity to tell the client that it's socket was ended
                            // The client interprets this as a message to close its own socket, so it doesn't have to time out from null characters
                            message += "$$$";
                        }

                        // There is a server password - the encryption must be done right before the message is sent
                        if (!String.IsNullOrWhiteSpace(Program.serverSettings.Password))
                        {
                            // Encrypt the message
                            message = AES.Encrypt(message, Program.serverSettings.Password);
                        }

                        // Message to bytes
                        Byte[] broadcastBytes = Encoding.ASCII.GetBytes(message);

                        // Write bytes to stream
                        broadcastStream.Write(broadcastBytes, 0, broadcastBytes.Length);

                        // Flush stream for cleanliness
                        broadcastStream.Flush();

                        // Self explanatory variable
                        if (logToConsole)
                        {
                            // Informs the console that it has sent a message to a user
                            Console.WriteLine("# " + "to " + username + ": " + message);
                        }
                    }
                }
            }
        }

        // broadcastToSocket is used when the user has not been integrated into the connectedUsers list, and therefore must be manually broadcasted to via its socket
        // This method is used primarily in the beginnings of a connection, mainly as an error feedback measure
        // The flag requestDisconnect does the same as in specificBroadcast and tells the client its socket has been closed
        // The flag encryptMessage is used when the socket has progressed past the server password validation and can successfully decrypt messages coming from the server
        public static void broadcastToSocket(string message, TcpClient tcpClient, bool requestDisconnect, bool encryptMessage)
        {
            // Gets stream from passed socket
            NetworkStream broadcastStream = tcpClient.GetStream();

            // Adds disconnection sequence if specified
            if (requestDisconnect)
            {
                message += "$$$";
            }

            // There is a server password - the encryption must be done right before the message is sent
            if (!String.IsNullOrWhiteSpace(Program.serverSettings.Password) & encryptMessage)
            {
                // Encrypt message
                message = AES.Encrypt(message, Program.serverSettings.Password);
            }

            // Message to bytes
            Byte[] broadcastBytes = Encoding.ASCII.GetBytes(message);

            // Write bytes to stream
            broadcastStream.Write(broadcastBytes, 0, broadcastBytes.Length);

            // Flush stream for cleanliness
            broadcastStream.Flush();
        }
    }
}