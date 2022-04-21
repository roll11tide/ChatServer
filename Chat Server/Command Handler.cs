using System;
using System.Collections.Generic;

namespace Chat_Server
{
    class Command_Handler
    {
        #region Command List
        // A massive list of Commands - as in objects of the Command class
        public static List<Program.Command> commandList = new List<Program.Command>()
        {
            // Implemented
            {new Program.Command("!BROADCAST", "Broadcasts a message to the entire server. Syntax: !BROADCAST <message>", 2)},
            {new Program.Command("!KICK", "Kicks a user from the server. Syntax: !KICK <username>", 1)},
            {new Program.Command("!RELEASECLIENT", "Properly disconnects the client.", 0)},
            {new Program.Command("!STOP", "Kills the server.", 3)},
            {new Program.Command("!CLEAR", "Clears the server console.", 3)},
            {new Program.Command("!SETPROFILES", "Resets XML according to current volatile data.", 2)},
            {new Program.Command("!INTPROFILES", "Initializes profiles from XML during runtime.", 2)},
            {new Program.Command("!SETRANK", "Sets a user to a specified power level. Syntax: !SETRANK <username> <powerlevel>", 3)},
            {new Program.Command("!HELP", "Lists commands.", 0)},
            {new Program.Command("!DEBUG", "Lists connected users and initialized profiles.", 2)},
            {new Program.Command("!BAN", "Bans a user from the server for a specified amount of time. Syntax: !BAN <username> <time in minutes>", 2)},
            {new Program.Command("!PERMABAN", "Bans a user from the server permanently. Syntax: !PERMABAN <username>", 2)},
            {new Program.Command("!INTSETTINGS", "Initializes settings from XML during runtime.", 3)},
            {new Program.Command("!SETSETTINGS", "Writes current server settings to XML.", 3)},
            {new Program.Command("!UNBAN", "Unbans a user that isn't shredded. Syntax: !UNBAN <username>", 2)},
            {new Program.Command("!SETTINGS", "Lists current server settings from memory.", 2)},
            {new Program.Command("!CLAIMUSER", "Claims username using a set password. Syntax: !CLAIMUSER <password> <password again for confirmation>", 0)},
            {new Program.Command("!DEFAULT", "Defaults settings.", 3)},
            {new Program.Command("!CLEARPROFILES", "Clears all profiles - essentially nukes Profiles.xml. ONLY USE IF YOU ARE SURE YOU WANT TO BLEACH SERVER!", 3)},
            {new Program.Command("!MESSAGE", "Privately messages a user. Syntax: !MESSAGE <username> <message>", 1)},
            {new Program.Command("!GAG", "Disables a user's ability to send messages. Syntax: !GAG <username>", 1)},
            {new Program.Command("!UNGAG", "Enables a user's ability to send messages. Syntax: !UNGAG <username>", 1)},
            {new Program.Command("!MYINFO", "Lists the user's information.", 0)},
            {new Program.Command("!GAGALL", "Gags all users except the caller and superadmins.", 3)},
            {new Program.Command("!SHRED", "Demotes a user to the lowest power level and permanently bans all of their known IPs, which requires physical access to the server files to reverse. Syntax: !SHRED <username>", 3)},
            {new Program.Command("!CLEARIPS", "Clears all known IPs of given user. Be careful - if you manage to clear the IPs of a banned user it will be very hard to unban their IPs. Syntax: !CLEARIPS <username>", 3)},
            {new Program.Command("!BANIP", "Bans IP manually. This can be dangerous if misused because it permanently bans the IP, meaning no one from that network can connect. Syntax: !BANIP <IP address>", 2)},
            {new Program.Command("!UNBANIP", "Unbans IP manually. Syntax: !UNBANIP <IP address>", 2)},
            {new Program.Command("!ADMINCHAT", "Gags all users with a power level of 1 or lower.", 2)},
            {new Program.Command("!UNGAGALL", "Ungags all users with a power level of 1 or lower.", 2)},
            {new Program.Command("!CHANGEPASSWORD", "Changes server password. Requires a restart. Syntax: !CHANGEPASSWORD <password> <password again for confirmation>", 3)},
            {new Program.Command("!USERS", "Lists all connected users.", 0)}
        };
        #endregion

        // This is the method that executes commands - it simply takes the entire line of a command as well as the user that called it
        public static void executeCommands(string line, Program.User user)
        {
            // This string will hold the command itself, which is used in a switch statement to determine which operation to perform
            string command = "";
            if (line.Contains(" "))
            {
                // This is a multiparameter command and means that the first space denotes the end of the actual command (no command itself has a space in it)
                command = line.Substring(0, line.IndexOf(" ")).ToUpper();
            }
            else
            {
                // No spaces means simply a command
                command = line;
            }
            command = command.ToUpper();

            // 100 - Console, 3 - SuperAdmin, 2 - Admin, 1 - Moderators, 0 - Users
            // This evaluates whether the user can perform the command even before executing the switch statement
            if (Return_Methods.checkPowerLevel(command, user))
            {
                switch (command)
                {
                    case "!BROADCAST":
                        // If the line doesn't contain a space, how will this possibly determine where the message is
                        if (line.Contains(" "))
                        {
                            // Cut message from command and broadcast it as a user of "#", or the console
                            string message = Return_Methods.getNthParameter(line, 1, false);
                            Console.WriteLine("# (you, the console) to all: " + message);
                            Broadcasting.broadcast(message, "#", false);
                        }
                        else
                        {
                            // This simply writes a message about invalid syntax to a given username
                            writeInvalidSyntax(user.Username);
                        }
                        break;
                    case "!KICK":
                        // Same deal with broadcast, a space is used to determine the user and if there isn't one, invalid syntax
                        if (line.Contains(" "))
                        {
                            // Separate the username from the command
                            string username = Return_Methods.getNthParameter(line, 1, false);

                            // Make sure the user is connected
                            if (Return_Methods.userConnected(username, user.Username))
                            {
                                // Disconnect them
                                Broadcasting.broadcast(username + " has been kicked.", "", true);
                                Program.flushOutClient(username, false);
                            }
                        }
                        else
                        {
                            writeInvalidSyntax(user.Username);
                        }
                        break;
                    case "!RELEASECLIENT":
                        // Clients call this to disconnect - it's just a cleanliness thing
                        if (Return_Methods.userConnected(user.Username, "#"))
                        {
                            Program.flushOutClient(user.Username, true);
                        }
                        break;
                    case "!BAN":
                        // Enumerate spaces does just that - it returns the number of spaces. !BAN must have a username and a ban time, two spaces
                        if (Return_Methods.enumerateSpaces(line) == 2)
                        {
                            // Cut username from command and time
                            string usernameToBan = Return_Methods.getNthParameter(line, 1, true);

                            // Make sure the user is connected before continuing
                            if (Return_Methods.userConnected(usernameToBan, user.Username))
                            {
                                // Cut time from the rest of the line
                                string time = Return_Methods.getNthParameter(line, 2, false);
                                int timeInt;

                                // Temporary user
                                Program.User userToBan = new Program.User();
                                if (int.TryParse(time, out timeInt))
                                {
                                    // A value was specified in numbers
                                    foreach (Program.User item in Program.connectedUsers.ToArray())
                                    {
                                        if (item.Username == usernameToBan)
                                        {
                                            userToBan = item;

                                            // Get current Epoch Time
                                            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                                            int secondsSinceEpoch = (int)t.TotalSeconds;

                                            // Set user's ban length to the given ban time multiplied by 60 to make it minutes, and then added to the current Epoch Time
                                            userToBan.BanLength = (timeInt * 60) + secondsSinceEpoch;
                                            userToBan.IsBanned = true;

                                            // Good example of the setUser method
                                            Program.setUser(userToBan);

                                            // This bans all the user's known IPs
                                            List<string> tempIPs = Return_Methods.returnIPList(userToBan.KnownIPs);
                                            foreach (string element in tempIPs)
                                            {
                                                if (!Program.bannedIPs.Contains(element))
                                                {
                                                    Program.bannedIPs.Add(element);
                                                }
                                            }
                                            XML_Functions.setBannedIPsToXML();

                                            // Broadcast the ban message and give them the boot
                                            Broadcasting.broadcast(userToBan.Username + " has been banned!", "", true);
                                            Program.flushOutClient(userToBan.Username, false);
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    writeInvalidSyntax(user.Username);
                                }
                            }
                        }
                        else
                        {
                            writeInvalidSyntax(user.Username);
                        }
                        break;
                    case "!PERMABAN":
                        // This operates in much the same way as the !BAN method, it just makes the ban length -1 and prints a comical ASCII gravestone
                        if (Return_Methods.enumerateSpaces(line) == 1)
                        {
                            string usernameToPermaBan = Return_Methods.getNthParameter(line, 1, true);

                            if (Return_Methods.userConnected(usernameToPermaBan, user.Username))
                            {
                                Program.User userToBan = new Program.User();
                                foreach (Program.User item in Program.connectedUsers.ToArray())
                                {
                                    if (item.Username == usernameToPermaBan)
                                    {
                                        userToBan = item;
                                        userToBan.BanLength = -1;
                                        userToBan.IsBanned = true;
                                        Program.setUser(userToBan);
                                        Console.WriteLine("                    _______");
                                        Console.WriteLine("              _____/       \\_____");
                                        Console.WriteLine("             |  _     ___   _   ||");
                                        Console.WriteLine("             | | \\     |   | \\  ||");
                                        Console.WriteLine("             | |  |    |   |  | ||");
                                        Console.WriteLine("             | |_/     |   |_/  ||");
                                        Console.WriteLine("             | | \\     |   |    ||");
                                        Console.WriteLine("             | |  \\    |   |    ||");
                                        Console.WriteLine("             | |   \\. _|_. | .  ||");
                                        Console.WriteLine("             |                  ||");
                                        Console.WriteLine("             |    Another One   ||");
                                        Console.WriteLine("             |     Bites The    ||");
                                        Console.WriteLine("             |        Dust      ||");
                                        Console.WriteLine("R.I.P. " + userToBan.Username + " - they have been perma-banned!");
                                        Broadcasting.broadcast("R.I.P. " + userToBan.Username + " - they have been perma-banned!", "", true);
                                        List<string> tempIPs = Return_Methods.returnIPList(userToBan.KnownIPs);
                                        foreach (string element in tempIPs)
                                        {
                                            if (!Program.bannedIPs.Contains(element))
                                            {
                                                Program.bannedIPs.Add(element);
                                            }
                                        }
                                        XML_Functions.setBannedIPsToXML();
                                        Program.flushOutClient(userToBan.Username, false);
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            writeInvalidSyntax(user.Username);
                        }
                        break;
                    case "!STOP":
                        // Simply kills the server
                        Broadcasting.broadcast("---SERVER ORDERED TO STOP---", "", true);
                        Environment.Exit(Environment.ExitCode);
                        break;
                    case "!SETPROFILES":
                        // Serialize current volatile profile list to Profiles.xml
                        XML_Functions.setProfilesToXML();
                        Broadcasting.specificBroadcast(user.Username, "Profiles set to Profiles.xml.", true, false);
                        break;
                    case "!INTPROFILES":
                        // Initialize the profiles from Profiles.xml
                        XML_Functions.initializeProfilesFromXML();
                        Broadcasting.specificBroadcast(user.Username, "Profiles initialized from Profiles.xml.", true, false);
                        break;
                    case "!CLEAR":
                        // Clear the console
                        Console.Clear();
                        break;
                    case "!HELP":
                        // Prints out all commands by looping through them
                        List<string> commandNames = new List<string>();
                        foreach (Program.Command item in commandList)
                        {
                            string name = item.Name;
                            name = name.Replace("!", "");
                            commandNames.Add(item.Name);
                        }
                        commandNames.Sort();

                        string dataPackageHelp = "Syntax is listed only for commands with multiple parameters.\r\n";
                        foreach (string item in commandNames)
                        {
                            foreach (Program.Command commandItem in commandList)
                            {
                                if (commandItem.Name == item & commandItem.RequiredPowerLevel <= user.PowerLevel)
                                {
                                    dataPackageHelp += "Name: " + commandItem.Name + "\r\nDescription: " + commandItem.Description + "\r\nRequired Power Level: " + commandItem.RequiredPowerLevel.ToString() + "\r\n---\r\n";
                                }
                            }
                        }

                        dataPackageHelp += "\r\nIf you believe a command is missing, it is probably because only the commands you can use are listed.";
                        Broadcasting.specificBroadcast(user.Username, dataPackageHelp, false, false);
                        /*
                        string dataPackageHelp = "Syntax is listed only for commands with multiple parameters.\r\n";

                        foreach (Program.Command item in commandList)
                        {
                            // Prints command only if the user can execute it - what use is knowing a command you can't use
                            if (item.RequiredPowerLevel <= user.PowerLevel)
                            {
                                dataPackageHelp += "Name: " + item.Name + "\r\nDescription: " + item.Description + "\r\nRequired Power Level: " + item.RequiredPowerLevel.ToString() + "\r\n---\r\n";
                            }
                        }
                        */
                        break;
                    case "!SETRANK":
                        if (Return_Methods.enumerateSpaces(line) == 2)
                        {
                            string userToSet = Return_Methods.getNthParameter(line, 1, true);

                            if (Return_Methods.userConnected(userToSet, user.Username))
                            {
                                string intString = Return_Methods.getNthParameter(line, 2, false);
                                int powerLevelToRaiseTo;

                                // Identifies user by username
                                foreach (Program.User item in Program.connectedUsers.ToArray())
                                {
                                    if (item.Username == userToSet)
                                    {
                                        if (userToSet == "#")
                                        {
                                            Console.WriteLine("The console will always be power level 100.");
                                        } else
                                        {
                                            if (int.TryParse(intString, out powerLevelToRaiseTo))
                                            {
                                                // Rank can only be set between 0 and 3, inclusively
                                                if (powerLevelToRaiseTo > 3 | powerLevelToRaiseTo < 0)
                                                {
                                                    Broadcasting.specificBroadcast(user.Username, "Specified power level is out of range.", false, false);
                                                    break;
                                                }
                                                else
                                                {
                                                    // Get index of user before modifying it - easier than making a new temporary user
                                                    int index = Program.connectedUsers.IndexOf(item);
                                                    item.PowerLevel = powerLevelToRaiseTo;
                                                    Program.connectedUsers[index] = item;
                                                    XML_Functions.setAndReadProfiles(false);
                                                    Broadcasting.specificBroadcast(user.Username, "Power level of " + item.Username + " set to " + powerLevelToRaiseTo.ToString() + ".", false, false);
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                // The string failed to parse and is therefore invalid syntax
                                                writeInvalidSyntax(user.Username);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            writeInvalidSyntax(user.Username);
                        }
                        break;
                    case "!DEBUG":
                        // This simply prints all connected users and some of their properties (some properties obviously can't be strings - threads, tcpclients, etc)
                        string dataPackage = "";
                        dataPackage += "Connected Users:\r\n/////\r\n";
                        foreach (Program.User item in Program.connectedUsers)
                        {
                            // A user requested debug information
                            dataPackage += "Username: " + item.Username + "\r\n";
                            dataPackage += "Power Level: " + item.PowerLevel.ToString() + "\r\n";
                            dataPackage += "Banned: " + item.IsBanned.ToString() + "\r\n";
                            dataPackage += "Shredded: " + item.IsShredded.ToString() + "\r\n";
                            dataPackage += "Ban Length (0 if not banned): " + item.BanLength.ToString() + "\r\n";
                            dataPackage += "/////\r\n";
                        }

                        Broadcasting.specificBroadcast(user.Username, dataPackage, false, false);
                        break;
                    case "!UNBAN":
                        // Does exactly what it says and works in practically the opposite way was !PERMABAN - also prints a funny ASCII tombstone with a zombie hand
                        if (Return_Methods.enumerateSpaces(line) == 1)
                        {
                            string usernameToUnban = Return_Methods.getNthParameter(line, 1, false);

                            foreach (Program.Profile item in Program.profileList.ToArray())
                            {
                                if (item.Username == usernameToUnban)
                                {
                                    item.IsBanned = false;
                                    item.BanLength = 0;
                                    Program.profileList[Program.profileList.IndexOf(item)] = item;
                                    XML_Functions.setProfilesToXML();
                                    Console.WriteLine("                    _______");
                                    Console.WriteLine("              _____/       \\_____");
                                    Console.WriteLine("             |  _     ___   _   ||");
                                    Console.WriteLine("             | | \\     |   | \\  ||");
                                    Console.WriteLine("             | |  |    |   |  | ||");
                                    Console.WriteLine("             | |_/     |   |_/  ||");
                                    Console.WriteLine("             | | \\     |   |    ||");
                                    Console.WriteLine("             | |  \\    |   |    ||");
                                    Console.WriteLine("             | |   \\. _|_. | .  ||");
                                    Console.WriteLine("             |    _   _   _     ||");
                                    Console.WriteLine("             |   | | | | | |  _ ||");
                                    Console.WriteLine("             | _ | |_| |_| |_| |||");
                                    Console.WriteLine("             || ||             |||");
                                    Console.WriteLine(item.Username + " is back from the 9th ring of the banned!");
                                    Broadcasting.broadcast(item.Username + " is back from the 9th ring of the banned!", "", true);

                                    // Remove banned IPs from storage mechanisms
                                    List<string> tempIPs = Return_Methods.returnIPList(item.KnownIPs);
                                    foreach (string element in tempIPs)
                                    {
                                        if (Program.bannedIPs.Contains(element))
                                        {
                                            Program.bannedIPs.Remove(element);
                                        }
                                    }
                                    XML_Functions.setBannedIPsToXML();
                                    break;
                                }
                            }
                        }
                        else
                        {
                            writeInvalidSyntax(user.Username);
                        }
                        break;
                    case "!INTSETTINGS":
                        // This simply calls the method - see XML_Functions for a definition of this method
                        XML_Functions.initializeSettingsFromXML();
                        Broadcasting.specificBroadcast(user.Username, "Settings have been initialized from Settings.xml.", true, false);
                        break;
                    case "!SETSETTINGS":
                        // This simply calls the method - see XML_Functions for a definition of this method
                        XML_Functions.setSettingsToXML();
                        Broadcasting.specificBroadcast(user.Username, "Settings have been serialized to Settings.xml.", true, false);
                        break;
                    case "!SETTINGS":
                        // Prints settings to caller and censors password
                        string censoredPassword = "";
                        foreach (char character in Program.serverSettings.Password)
                        {
                            censoredPassword += "*";
                        }

                        string dataPackageSettings = "";
                        dataPackageSettings += "/////\r\n";
                        dataPackageSettings += "Server Name: " + Program.serverSettings.ServerName + "\r\n";
                        dataPackageSettings += "Password: " + censoredPassword + "\r\n";
                        dataPackageSettings += "MOTD: " + Program.serverSettings.MOTD + "\r\n";
                        dataPackageSettings += "User Limit: " + Program.serverSettings.UserLimit.ToString() + "\r\n";
                        dataPackageSettings += "/////";
                        Broadcasting.specificBroadcast(user.Username, dataPackageSettings, false, false);
                        break;
                    case "!CLAIMUSER":
                        // This claims the username using a password and will from hereforth require a password to connect as that user
                        if (Return_Methods.enumerateSpaces(line) == 2)
                        {
                            if (user.Username == "#")
                            {
                                // The console is not technically a user and therefore cannot be claimed - plus it requires physical access to control
                                Console.WriteLine("You cannot claim the console.");
                            }
                            else
                            {
                                // An actual user is claiming their name
                                string password = Return_Methods.getNthParameter(line, 1, true);
                                string passwordConfirm = Return_Methods.getNthParameter(line, 2, true);

                                if (password == passwordConfirm)
                                {
                                    // Passwords match
                                    foreach (Program.User item in Program.connectedUsers.ToArray())
                                    {
                                        if (item.Username == user.Username)
                                        {
                                            // Encrypt the password so the owner of the server can't password phish
                                            string encryptedPassword = AES.Encrypt(password, password);

                                            // Set user's password property
                                            item.Password = encryptedPassword;
                                            Program.connectedUsers[Program.connectedUsers.IndexOf(item)] = item;

                                            // Persist to XML
                                            XML_Functions.setAndReadProfiles(false);
                                            XML_Functions.initializeProfilesFromXML();

                                            // Censor password
                                            string asteriskPassword = "";
                                            foreach (char character in password)
                                            {
                                                asteriskPassword += "*";
                                            }

                                            // Provide confirmation that the username was claimed
                                            Broadcasting.specificBroadcast(user.Username, user.Username + " claimed with password (censored): " + asteriskPassword, false, false);
                                        }
                                    }
                                } else
                                {
                                    // Passwords do not match
                                    Broadcasting.specificBroadcast(user.Username, "Passwords do not match.", false, false);
                                }
                            }
                        } else
                        {
                            writeInvalidSyntax(user.Username);
                        }
                        break;
                    case "!DEFAULT":
                        // Calls method - see Program for more information
                        Program.defaultServerSettings();
                        Broadcasting.specificBroadcast(user.Username, "Server settings defaulted.", true, false);
                        break;
                    case "!CLEARPROFILES":
                        // Clears out profiles by creating an empty profile list and serializing that to XML
                        Program.profileList = new List<Program.Profile>();
                        XML_Functions.setAndReadProfiles(false);
                        Broadcasting.specificBroadcast(user.Username, "Profiles cleared and Profiles.xml nuked.", true, false);
                        break;
                    case "!MESSAGE":
                        // Sends a message from one user to another
                        if (Return_Methods.enumerateSpaces(line) >= 2)
                        {
                            // See Return_Methods for more information about the getNthParameter method
                            string recipient = Return_Methods.getNthParameter(line, 1, true);
                            string message = Return_Methods.getNthParameter(line, 2, false);

                            // Make sure user is connected
                            if (Return_Methods.userConnected(recipient, user.Username))
                            {
                                // TODO Add anti-spam - the current solution right now is to make this an elevated permission command (power level 1 required)
                                Broadcasting.specificBroadcast(recipient, user.Username + " to you: " + message, false, false);
                                Broadcasting.specificBroadcast(user.Username, "You to " + recipient + ": " + message, false, false);
                            }
                        } else
                        {
                            writeInvalidSyntax(user.Username);
                        }
                        break;
                    case "!GAG":
                        // This silences users that are being disruptive - it also carries over between connections
                        if (Return_Methods.enumerateSpaces(line) == 1)
                        {
                            // Separate username
                            string userToGag = Return_Methods.getNthParameter(line, 1, true);

                            // Verify user is connected
                            if (Return_Methods.userConnected(userToGag, user.Username))
                            {
                                // Identify user by username
                                foreach (Program.User item in Program.connectedUsers.ToArray())
                                {
                                    if (item.Username == userToGag)
                                    {
                                        // Gag them
                                        item.IsGagged = true;
                                        Program.connectedUsers[Program.connectedUsers.IndexOf(item)] = item;
                                        XML_Functions.setAndReadProfiles(false);
                                    }
                                }

                                Broadcasting.broadcast(userToGag + " has been gagged.", "", true);
                            }
                        }
                        break;
                    case "!UNGAG":
                        // Allows a user to speak once more - acts opposite to the !GAG command
                        if (Return_Methods.enumerateSpaces(line) == 1)
                        {
                            string userToUngag = Return_Methods.getNthParameter(line, 1, true);

                            foreach (Program.User item in Program.connectedUsers.ToArray())
                            {
                                if (item.Username == userToUngag)
                                {
                                    item.IsGagged = false;
                                    Program.connectedUsers[Program.connectedUsers.IndexOf(item)] = item;
                                    XML_Functions.setAndReadProfiles(false);
                                }
                            }

                            Broadcasting.broadcast(userToUngag + " has been ungagged.", "", true);
                        }
                        break;
                    case "!MYINFO":
                        Broadcasting.specificBroadcast(user.Username, "", false, false);
                        bool passworded = false;
                        if (!String.IsNullOrWhiteSpace(user.Password))
                        {
                            passworded = true;
                        }
                        Broadcasting.specificBroadcast(user.Username, "\r\nYour User Information:\r\nUsername: " + user.Username + "\r\nPassworded: " + passworded.ToString() + "\r\nPower Level: " + user.PowerLevel.ToString() + "\r\n", false, false);
                        break;
                    case "!ADMINCHAT":
                        foreach (Program.User item in Program.connectedUsers.ToArray())
                        {
                            if (item.PowerLevel <= 1)
                            {
                                int index = Program.connectedUsers.IndexOf(item);
                                item.IsGagged = true;
                                Program.connectedUsers[index] = item;
                            }
                        }
                        break;
                    case "!GAGALL":
                        // Gags all users under a power level of 3
                        foreach (Program.User item in Program.connectedUsers.ToArray())
                        {
                            // Cannot gag superadmins using !GAGALL
                            if (item.PowerLevel < 3)
                            {
                                int index = Program.connectedUsers.IndexOf(item);
                                item.IsGagged = true;
                                Program.connectedUsers[index] = item;
                                XML_Functions.setAndReadProfiles(false);
                            }
                        }
                        break;
                    case "!UNGAGALL":
                        // Ungags everyone regardless of powerlevel
                        foreach (Program.User item in Program.connectedUsers.ToArray())
                        {
                            int index = Program.connectedUsers.IndexOf(item);
                            item.IsGagged = false;
                            Program.connectedUsers[index] = item;
                            XML_Functions.setAndReadProfiles(false);
                        }
                        break;
                    case "!SHRED":
                        // Shred is used to really torch someone and their network off the server - it can exclusively be undone with physical access to the server
                        if (Return_Methods.enumerateSpaces(line) == 1)
                        {
                            string userToShred = Return_Methods.getNthParameter(line, 1, true);

                            if (Return_Methods.userConnected(userToShred, user.Username))
                            {
                                foreach (Program.User item in Program.connectedUsers.ToArray())
                                {
                                    if (item.Username == userToShred)
                                    {
                                        // Unique properties set here
                                        int index = Program.connectedUsers.IndexOf(item);
                                        item.IsBanned = true;
                                        item.BanLength = -1;
                                        item.IsShredded = true;
                                        item.PowerLevel = 0;
                                        Program.connectedUsers[index] = item;
                                    }
                                }

                                Broadcasting.broadcast(userToShred + " has been SHREDDED! They are permanently banned from the server and all their power has been stripped.", "", true);
                                Program.flushOutClient(userToShred, false);
                            }
                        }
                        break;
                    case "!CLEARIPS":
                        // Removes all known IPs - essentially clears stored IPs from user, including the one they are currently connected from
                        if (Return_Methods.enumerateSpaces(line) == 1)
                        {
                            // Find username to clear using a return method
                            string usernameToClear = Return_Methods.getNthParameter(line, 1, true);

                            // Ensure user is connected
                            if (Return_Methods.userConnected(usernameToClear, user.Username))
                            {
                                // Find connected user...
                                foreach (Program.User item in Program.connectedUsers)
                                {
                                    // ...by username
                                    if (item.Username == usernameToClear)
                                    {
                                        // Save index temporarily, blank out IPs, reset user to volate list
                                        int index = Program.connectedUsers.IndexOf(item);
                                        item.KnownIPs = "";
                                        Program.connectedUsers[index] = item;
                                    }
                                }

                                // Persist users
                                XML_Functions.setProfilesToXML();
                            }
                        }
                        break;
                    case "!BANIP":
                        // The !BANIP command requires a parameter, meaning at least one space, and IPs don't have spaces
                        if (Return_Methods.enumerateSpaces(line) == 1)
                        {
                            // Get IP to ban, add it, broadcast to caller it was banned, persist banned IP
                            string IP = Return_Methods.getNthParameter(line, 1, true);
                            Program.bannedIPs.Add(IP);
                            Broadcasting.specificBroadcast(user.Username, IP + " added to blacklist.", false, false);
                            XML_Functions.setBannedIPsToXML();
                        } else
                        {
                            // Missing a space
                            writeInvalidSyntax(user.Username);
                        }
                        break;
                    case "!UNBANIP":
                        // Works inversely to !BANIP
                        if (Return_Methods.enumerateSpaces(line) == 1)
                        {
                            string IP = Return_Methods.getNthParameter(line, 1, true);
                            if (Program.bannedIPs.Contains(IP))
                            {
                                // List contains specified IP - remove it, broadcast to caller, persist
                                Program.bannedIPs.Remove(IP);
                                Broadcasting.specificBroadcast(user.Username, IP + " removed from blacklist.", false, false);
                                XML_Functions.setBannedIPsToXML();
                            }
                        } else
                        {
                            // Missing a space
                            writeInvalidSyntax(user.Username);
                        }
                        break;
                    case "!CHANGEPASSWORD":
                        // Changes server password with restart
                        if (Return_Methods.enumerateSpaces(line) == 2)
                        {
                            string password = Return_Methods.getNthParameter(line, 1, true);
                            string passwordConfirmation = Return_Methods.getNthParameter(line, 2, true);

                            if (password == passwordConfirmation & !password.Contains(" ") & !password.Contains("$"))
                            {
                                Program.serverSettings.Password = password;
                                XML_Functions.setSettingsToXML();
                                Broadcasting.broadcast("---PASSWORD CHANGED - SERVER HALTING---", "", true);
                                Environment.Exit(Environment.ExitCode);
                            } else
                            {
                                Broadcasting.specificBroadcast(user.Username, "Passwords don't match.", false, false);
                            }
                        }
                        break;
                    case "!USERS":
                        string dataPackageUsers = "Connected Users: ";
                        bool firstName = true;
                        foreach (Program.User item in Program.connectedUsers)
                        {
                            if (firstName)
                            {
                                dataPackageUsers += item.Username;
                                firstName = false;
                            } else
                            {
                                dataPackageUsers += ", " + item.Username;
                            }
                        }

                        Broadcasting.specificBroadcast(user.Username, dataPackageUsers, false, false);
                        break;
                    default:
                        writeInvalidSyntax(user.Username);
                        break;
                }
            }
        }

        // This is used to quickly tell a caller the syntax was improper
        public static void writeInvalidSyntax(string username)
        {
            if (username == "#")
            {
                // Invalid console syntax
                Console.WriteLine("Invalid syntax.");
            }
            else
            {
                // Invalid client syntax
                Broadcasting.specificBroadcast(username, "Invalid syntax.", true, false);
            }
        }
    }
}