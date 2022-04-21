using System;
using System.Net.Sockets;
using System.Threading;
using System.Timers;

namespace Chat_Server
{
    class Client_Routine
    {
        // Main user object (this script is run on a different thread for each connected user)
        Program.User user;

        // Main initialization method
        public void startClient(Program.User givenUser)
        {
            // Make the user the givenUser so the whole script can use it
            user = givenUser;

            // Start the actual chat thread
            Thread ctThread = new Thread(() => receiveMessages(givenUser));

            // Make sure the user isn't banned, although this should have been taken care of when the user initially connected
            if (user.IsBanned)
            {
                Broadcasting.specificBroadcast(user.Username, "You are banned!", false, true);
                Program.flushOutClient(user.Username, false);
            }
            else
            {
                // This is a rare example of using deDupe() - makes sure there are no duplicate usernames (see return methods for a better look at this method)
                if (Return_Methods.deDupe())
                {
                    // Go through each connected user
                    foreach (Program.User item in Program.connectedUsers)
                    {
                        // Identify by username
                        if (item.Username == user.Username)
                        {
                            // Set the chat thread created above as its thread property
                            user.Thread = ctThread;
                            break;
                        }
                    }
                }

                // Start the chat thread
                ctThread.Start();
            }
        }

        // This is the method that runs on the thread
        private void receiveMessages(Program.User givenUser)
        {
            // Get byte size of message
            byte[] bytesFrom = new byte[user.TcpClient.ReceiveBufferSize];

            // Make the variable used to transport data
            string dataFromClient = "";

            // Make current spamPenalty and the max at which the user is kicked
            int spamPenalty = 0;
            int spamPenaltyMax = Program.serverSettings.MaxSpamCap;

            // These are anti-spam measures
            // Change this for longer anti-spam
            System.Timers.Timer spamTimer = new System.Timers.Timer(750);
            spamTimer.Elapsed += new ElapsedEventHandler(spamTimer_Elapsed);
            double secondsOnSpamTimer = spamTimer.Interval / 1000;

            while (true)
            {
                try
                {
                    // Checks if TcpClient is open before running chat routine
                    if (user.TcpClient.Connected)
                    {
                        // Main user socket
                        NetworkStream networkStream = user.TcpClient.GetStream();

                        // Reads data from connection
                        networkStream.Read(bytesFrom, 0, (int)user.TcpClient.ReceiveBufferSize);

                        // Makes the bytes text
                        dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom);

                        // Remove whitespace and nullcharacters
                        dataFromClient = dataFromClient.Replace("\r\n", "");
                        dataFromClient = dataFromClient.Replace("\0", "");

                        // Delimit
                        dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf("$"));

                        // Prevents frequency spam under a given value set in the server settings
                        if (dataFromClient.Length <= Program.serverSettings.MessageCap)
                        {
                            // This is a different statement as to tell the user exactly what they did wrong
                            if (!String.IsNullOrWhiteSpace(dataFromClient))
                            {
                                // Checks for frequency spam
                                spamPenalty += 2;
                                if (spamTimer.Enabled)
                                {
                                    // If the user's spam penalty is great than that of the allotted
                                    if (spamPenalty >= spamPenaltyMax)
                                    {
                                        // Kick them and notify the server
                                        Console.WriteLine(givenUser.Username + " has been disconnected for spamming.");
                                        Broadcasting.broadcast(givenUser.Username + " has been disconnected for spamming.", "", true);
                                        Program.flushOutClient(givenUser.Username, false);
                                    }
                                    else
                                    {
                                        // Otherwise they have not reached the cap, but will be warned and their spam penalty value will be driven up
                                        Broadcasting.specificBroadcast(givenUser.Username, "Slow down! Your spam penalty is " + spamPenalty.ToString() + " and increases by two with every spam message sent. You will be disconnected at " + spamPenaltyMax.ToString() + ". To reduce this, send messages with at least " +
                                            secondsOnSpamTimer.ToString() + " second(s) between them. Your spam penalty will go down by one with every valid message.", false, false);
                                    }
                                } else {
                                    // Check to see if the user is muted
                                    if (user.IsGagged)
                                    {
                                        // User is gagged
                                        Broadcasting.specificBroadcast(user.Username, "You are gagged!", false, false);
                                    } else
                                    {
                                        // Whitespace is pointless, why even bother processing it
                                        if (!String.IsNullOrWhiteSpace(Program.serverSettings.Password))
                                        {
                                            // Decrypt it so everything can read it
                                            dataFromClient = AES.Decrypt(dataFromClient, Program.serverSettings.Password);
                                        }

                                        // Command interception from client
                                        if (dataFromClient[0] == '!')
                                        {
                                            // This logs the commands the user called along with its parameters
                                            // Console.WriteLine(user.Username + ": " + dataFromClient);

                                            // This passes the command to the main command handler (see the command handler script for more information)
                                            Command_Handler.executeCommands(dataFromClient, givenUser);
                                        }
                                        else {
                                            // Checked later
                                            bool IsGagged = false;

                                            // Goes through all connected users and finds the user by name
                                            foreach (Program.User item in Program.connectedUsers)
                                            {
                                                // If the username matches and the profile AND is gagged
                                                if (item.Username == user.Username & item.IsGagged)
                                                {
                                                    // User is muted
                                                    IsGagged = true;
                                                    break;
                                                }
                                            }

                                            // Otherwise, the user isn't gagged
                                            if (!IsGagged)
                                            {
                                                // Dollar signs are the default delimiter, so sending a message with one in it cuts it off where the dollar sign is
                                                // To get around this, the client converts all dollar signs in the message to (DOLLARSIGN), and the server then converts that AFTER delimiting it to a real dollra sign
                                                dataFromClient = dataFromClient.Replace("(DOLLARSIGN)", "$");

                                                // Message got through all the catches and will be broadcasted
                                                Console.WriteLine(user.Username + ": " + dataFromClient);
                                                Broadcasting.broadcast(dataFromClient, user.Username, false);

                                                // Negative spam penalties are avoided by checking its value against zero
                                                if (spamPenalty > 0)
                                                {
                                                    // User sent a non-spam message and will have their spam penalty reduced
                                                    spamPenalty--;
                                                }

                                                // Restart the spam timer
                                                spamTimer.Start();
                                            }
                                        }
                                    }
                                }
                            }
                        } else {
                            // Anti-spam caught a message longer than 204 characters
                            Broadcasting.specificBroadcast(givenUser.Username, "Message too long.", false, false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // NOTICE This handles all errors - so errors may cause duplicate console logs
                    Console.WriteLine(givenUser.Username + " has been disconnected due to the error catch.");
                    Program.flushOutClient(givenUser.Username, true);
                    Thread.CurrentThread.Abort();
                }
            }
        }

        private static void spamTimer_Elapsed(object source, ElapsedEventArgs e)
        {
            // Disable timer
            System.Timers.Timer timer = (System.Timers.Timer)source;
            timer.Stop();
        }
    }
}