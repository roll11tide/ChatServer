using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Chat_Server
{
    class Return_Methods
    {
        // Used to test if the delimiter is used more than once on received messages
        public static bool checkForMultipleDollarSigns(string line)
        {
            int counter = 0;

            // Literally goes through every character and adds one if it finds a dollar sign
            foreach (char character in line)
            {
                if (character == '$')
                {
                    counter++;
                }

                // Returns true if there is more than one dollar sign
                if (counter > 1)
                {
                    return true;
                }
            }

            // If it never returned true, then the counter never reached more than 1 - good data
            return false;
        }

        // getNthParameter is a very used method - it finds the next parameter in a command based on spaces
        public static string getNthParameter(string line, int parameterNumber, bool excludePastSpace)
        {
            // Check to see if message contains a space
            for (int i = parameterNumber; i > 0; i--)
            {
                if (line.Contains(" "))
                {
                    // Remove up to the space if there is one
                    int spaceIndex = line.IndexOf(" ");
                    line = line.Replace(line.Substring(0, spaceIndex + 1), "");
                }
                else
                {
                    // Never good - it was passed something with no spaces; it can't find a parameter in a line with no spaces
                    Console.WriteLine("Failed to find parameter!");
                    return "";
                }
            }

            try
            {
                if (excludePastSpace & line.Contains(" "))
                {
                    // Removes everything past last space
                    line = line.Substring(0, line.IndexOf(" "));
                }
            }
            catch (Exception ex)
            {
                // Also never good - I phased this error out by adding the check before taking the substringent by space after checking if there is a space left in the line
                Console.WriteLine("Exclusive space command substringent requested with no ending space.\r\nLine: " + line + "\r\nParameter Number: " + parameterNumber.ToString());
            }


            return line;
        }

        // Simply returns an integer which respresents the number of spaces in the given string
        public static int enumerateSpaces(string line)
        {
            int spaceCount = 0;

            // Go through each character in the line
            foreach (char character in line)
            {
                if (character == ' ')
                {
                    // Add one to a variable if the character is a space
                    spaceCount++;
                }
            }

            // Return spaceCount - the number of spaces found in the line
            return spaceCount;
        }

        // Used to verify username validity, just as the name implies
        public static bool usernameValid(string username)
        {
            // Goes through every character in the given username
            foreach (string item in Program.illegalUsernameCharacters)
            {
                // Checks a string array to see if the character is in it
                if (username.Contains(item))
                {
                    // Return false if it is
                    return false;
                }
            }

            // Check for username length
            if (username.Length > 10)
            {
                // If the username is longer than 10 characters, return false
                return false;
            }

            // No problems - return true
            return true;
        }

        // Used in the command handler to check if a user has the correct permissions to perform a given command
        public static bool checkPowerLevel(string command, Program.User givenUser)
        {
            // Go through every command
            foreach (Program.Command listCommand in Command_Handler.commandList)
            {
                try
                {
                    // Locate command by name
                    if (listCommand.Name == command)
                    {
                        // This is written in as close to english as possible - self-explanatory
                        if (givenUser.PowerLevel >= listCommand.RequiredPowerLevel)
                        {
                            // User has correct permissions
                            return true;
                        }
                        else
                        {
                            // User is lacking permissions - inform them as to such and return false
                            Broadcasting.specificBroadcast(givenUser.Username, "Insufficient power level!", false, false);
                            return false;
                        }
                    }
                }
                catch (Exception cmdEx)
                {
                    // I am yet to encounter an error that leads to this being executed - ever (but you never know)
                    Console.WriteLine("Error while executing command" + command + ":\r\n" + cmdEx.ToString());
                    return false;
                }
            }

            // If nothing was returned then the command wasn't found
            Broadcasting.specificBroadcast(givenUser.Username, "Command not found.", false, false);

            // Return false because the command doesn't exist to begin with
            return false;
        }

        // A seldom used method used to purify the data link between profiles and connected users
        public static bool deDupe()
        {
            // List used to store all connected usernames
            List<string> tempUsernameList = new List<string>();
            bool returnBool = true;

            foreach (Program.User user in Program.connectedUsers.ToArray())
            {
                if (tempUsernameList.Contains(user.Username))
                {
                    // For whatever reason, there are two users with the same username - this should never happen to begin with
                    Console.WriteLine("CATASTROPHIC FAILURE: DUPLICATE USERNAME FOUND - BREAKING CALLING OPERATION!\r\n" + "Username: " + user.Username);

                    // Go through every connected user
                    foreach (Program.User item in Program.connectedUsers)
                    {
                        // Identify offending username by name
                        if (item.Username == user.Username)
                        {
                            // Remove that user from connected users
                            Program.connectedUsers.Remove(item);

                            // Log that this operation was performed
                            Console.WriteLine("Removed " + "\"" + item.Username + "\" from connected users list.");
                        }
                    }

                    // This will break whatever operation the caller is performing by returning false (this method is almost always used in an if statement)
                    returnBool = false;
                }
                else
                {
                    // This list will never have any duplicates in it, as usernames are only added if the list does not contain them
                    // Keep this in mind as it can be used to purify the data if so desired
                    tempUsernameList.Add(user.Username);
                }
            }

            return returnBool;
        }

        // This is used to manually levy data from a socket and is exclusively used to get the password from a connection with a protected username
        public static string receiveResponseFromSocket(TcpClient tcpClient)
        {
            // Inform the user that we are requesting the protected username's password
            Broadcasting.broadcastToSocket("Enter your password:", tcpClient, false, true);

            // Reset dataFromClient in case it had something stored in it before
            string dataFromClient = "";

            // Typical broadcasting format (see broadcasting for a morth in-depth look at how this works)
            byte[] bytesFrom = new byte[tcpClient.ReceiveBufferSize];
            NetworkStream networkStream = tcpClient.GetStream();
            networkStream.Read(bytesFrom, 0, (int)tcpClient.ReceiveBufferSize);
            dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom);

            // Purify data of returns and null characters
            dataFromClient = dataFromClient.Replace("\r\n", "");
            dataFromClient = dataFromClient.Replace("\0", "");

            // Take delimiter using standard substringent character ($)
            dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf("$"));

            // Replace the formate for a dollar sign with an actual dollar sign
            string messageWithDollarSigns = dataFromClient.Replace("(DOLLARSIGN)", "$");

            // Return the data to the caller
            return dataFromClient;
        }

        // Checks if the user is connected - this is imperative to program.cs because it prevents duplicate usernames and thus a host of problems later
        // Also very simple in design
        public static bool userConnected (string username, string usernameIfError)
        {
            // Go through all connected users
            foreach (Program.User item in Program.connectedUsers)
            {
                // Return true (user is already connected) if the username matches
                if (item.Username == username)
                {
                    // Username matches one already connected
                    return true;
                }
            }

            // Automatically writes error using given caller's username (saves lines)
            Broadcasting.specificBroadcast(usernameIfError, "User is not connected.", false, false);
            return false;
        }

        // Returns a list of all known IPs by separating by the delimiter (;)
        public static List<string> returnIPList(string line)
        {
            // List used to add to and return later
            List<string> tempIPList = new List<string>();

            // Go through every character in the line (the line should be a string of IPs)
            // Works somewhat similarly in design to getNthParameter()
            foreach (char character in line)
            {
                // Find the delimiter
                if (character == ';' & line.IndexOf(character) <= line.Length)
                {
                    // Take the index
                    int index = line.IndexOf(';');

                    // Take substring to index, inclusively to exclusively (just the IP)
                    string IP = line.Substring(0, index);

                    // Add it to the list
                    tempIPList.Add(IP);

                    // Modify the line to remove the IP and its semicolon
                    line = line.Replace(line.Substring(0, index + 1), "");
                }
            }

            // Return the list of IPs
            return tempIPList;
        }
    }
}