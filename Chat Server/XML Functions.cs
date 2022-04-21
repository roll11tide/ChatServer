using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Chat_Server
{
    class XML_Functions
    {
        /// <summary>
        /// The reason I did not make a universal "serializeTypeToFilename" was simple - there are other operations involved other than just serializing it
        /// Also, I have had minimal success with passing types to methods; it's messy and requires strange references to make it work
        /// I find it easier to custom tailer each "serialize(insert_class_here)ToXml" than to try to make an ambiguous one
        /// 
        /// Also - I will not comment-guide each XML serialization part, as it is explained under setSettingsToXML() and initializeSettingsFromXML()
        /// Only the differences between the methods will be explained in the comments
        /// </summary>
        
        #region Server Settings
        // Some classes have this type method, others don't - it's used to quickly set and read XML; code is self-explanatory
        public static void setAndReadSettings(bool writeToConsole)
        {
            setSettingsToXML();
            initializeSettingsFromXML();
            if (writeToConsole)
            {
                Console.WriteLine("Server settings written and initialized.");
            }
        }

        // Takes current volatile settings and persists them to Settings.xml
        public static void setSettingsToXML()
        {
            if (!File.Exists("Settings.xml"))
            {
                // The file doesn't exist - these "missing file" messages always come up on the first run, as none of the files are present
                Console.WriteLine("Settings.xml not found, defaulting settings and creating new Settings.xml...");
                File.Create("Settings.xml").Close();

                // Set defaults here
                Program.defaultServerSettings();
            }

            /// <summary>
            /// The way I decided to serialize XML was by simply nuking the xml document and rewriting it from scratch
            /// The reason I did this was logically the same as trying to "append" to the xml
            /// If you are going to set all volatile memory to xml, why would you bother appending? You would need to find the differences then serialize them.
            /// Why not just completely re-write it; it saves lines, looks better, and works better as far as I can tell.
            /// </summary>
            
            // The file will always exist at this point
            // Create files stream (this also nukes the document for some reason)
            FileStream file = File.Create(Environment.CurrentDirectory + "//Settings.xml");

            // Create the serializer object, pass it the settings class because it is serializing settings
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Program.ServerSettings));

            // Serialize given object of XmlSerializer's class type to the file stream
            xmlSerializer.Serialize(file, Program.serverSettings);

            // Close the filestream, because it will cause problems if it is open later
            file.Close();
        }

        // This is used to read settings from the persisted xml file
        public static void initializeSettingsFromXML()
        {
            if (!File.Exists("Settings.xml"))
            {
                // If the file doesn't exist, that means that the settings don't exist. In this situation, they are defaulted anyway. Calling setSettings simply makes the file, defaults, and persists.
                // Saves lines in a clever way
                setSettingsToXML();
            }

            XmlDocument xmlDocument = new XmlDocument();

            // Unnecessary, but who knows what can happen
            if (File.Exists("Settings.xml"))
            {
                try
                {
                    // Load document from path
                    xmlDocument.Load("Settings.xml");

                    // Set serializer class
                    XmlSerializer serializer = new XmlSerializer(typeof(Program.ServerSettings));

                    // De-serialize to string
                    string xmlString = xmlDocument.OuterXml.ToString();

                    // Using statement for disposing this after use
                    using (XmlNodeReader reader = new XmlNodeReader(xmlDocument))
                    {
                        // Deserialize XML string to serverSettings
                        Program.serverSettings = (Program.ServerSettings)serializer.Deserialize(reader);
                    }
                }
                catch (Exception exce)
                {
                    // Just in case something goes wrong - missing root element, missing file (which shouldn't be possible), improper XML - most of these errors occur when the user improperly edits the XML
                    // Most of these errors can be fixed by defaulting the server settings
                    Console.WriteLine("Failed to initialize settings from XML! It may be blank.");
                }
            }
            else
            {
                // This executes on the first run and whenever the settings file can't be found
                Console.WriteLine("Settings.xml not found, creating one...");
                File.Create("Settings.xml").Close();
            }
        }
        #endregion

        #region Profiles
        // Simply combines both methods with an optional output message
        public static void setAndReadProfiles(bool writeToConsole)
        {
            setProfilesToXML();
            initializeProfilesFromXML();

            if (writeToConsole)
            {
                Console.WriteLine("Profiles written and initialized.");
            }
        }

        // Works very similarly to setSettingsToXML()
        public static void setProfilesToXML()
        {
            if (!File.Exists("Profiles.xml"))
            {
                File.Create("Profiles.xml").Close();
            }

            FileStream file = File.Create(Environment.CurrentDirectory + "//Profiles.xml");

            // tempProfileList is used to hold the profile values in between serialization
            List<Program.Profile> tempProfileList = Program.profileList;

            List<string> connectedUsernames = new List<string>();
            foreach (Program.User item in Program.connectedUsers)
            {
                connectedUsernames.Add(item.Username);
            }

            // Manually add every profile to tempProfileList
            foreach (Program.User user in Program.connectedUsers)
            {
                Program.Profile tempProfile = new Program.Profile();
                tempProfile.Username = user.Username;
                tempProfile.Password = user.Password;
                tempProfile.PowerLevel = user.PowerLevel;
                tempProfile.IsBanned = user.IsBanned;
                tempProfile.IsShredded = user.IsShredded;
                tempProfile.IsGagged = user.IsGagged;
                tempProfile.BanLength = user.BanLength;
                tempProfile.KnownIPs = user.KnownIPs;

                List<string> profileUsernames = new List<string>();
                foreach (Program.Profile item in Program.profileList)
                {
                    profileUsernames.Add(item.Username);
                }

                if (profileUsernames.Contains(user.Username))
                {
                    // A connected user that is already in the profiles was found
                    foreach (Program.Profile profile in Program.profileList)
                    {
                        if (profile.Username == user.Username)
                        {
                            // Removes old version of profile
                            tempProfileList.Remove(profile);
                            break;
                        }
                    }
                }

                // Adds final, correct version of the profile
                tempProfileList.Add(tempProfile);
            }

            // Serialize
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<Program.Profile>));
            xmlSerializer.Serialize(file, tempProfileList);

            // Close file stream
            file.Close();
        }

        // Works very similarly to initializeSettingsFromXML()
        public static void initializeProfilesFromXML()
        {
            XmlDocument xmlDocument = new XmlDocument();

            if (File.Exists("Profiles.xml"))
            {
                try
                {
                    // Load XML document
                    xmlDocument.Load("Profiles.xml");

                    // Set serializer type
                    XmlSerializer serializer = new XmlSerializer(typeof(List<Program.Profile>));

                    // De-serialize XML to string
                    string xmlString = xmlDocument.OuterXml.ToString();

                    // Disposable nodereader
                    using (XmlNodeReader reader = new XmlNodeReader(xmlDocument))
                    {
                        // De-serialize XML string to profileList
                        Program.profileList = (List<Program.Profile>)serializer.Deserialize(reader);
                    }
                }
                catch (Exception exce)
                {
                    Console.WriteLine("Failed to initialize profiles from XML! It may be blank.");
                }
            }
            else
            {
                // Runs on first run / user edited something or deleted the file
                Console.WriteLine("Profiles.xml not found, creating one...");
                File.Create("Profiles.xml").Close();
            }
        }
        #endregion

        #region Banned IPs
        public static void initializeBannedIPsFromXML()
        {
            XmlDocument xmlDocument = new XmlDocument();

            if (File.Exists("BannedIPs.xml"))
            {
                try
                {
                    // Load XML document
                    xmlDocument.Load("BannedIPs.xml");

                    // Set serializer type/class
                    XmlSerializer serializer = new XmlSerializer(typeof(List<string>));

                    // De-serialize to string
                    string xmlString = xmlDocument.OuterXml.ToString();

                    // Disiposable nodereader
                    using (XmlNodeReader reader = new XmlNodeReader(xmlDocument))
                    {
                        // De-serialize XML string to bannedIPs
                        Program.bannedIPs = (List<string>)serializer.Deserialize(reader);
                    }
                }
                catch (Exception exce)
                {
                    Console.WriteLine("Failed to initialize banned IPs from XML! It may be blank.");
                }
            }
            else
            {
                // Missing bannedIPs - must create one
                Console.WriteLine("BannedIPs.xml not found, creating one...");
                File.Create("BannedIPs.xml").Close();
            }
        }

        // Works like all the other serialization methods, but even simpler
        public static void setBannedIPsToXML()
        {
            // Make sure the file exists
            if (!File.Exists("BannedIPs.xml"))
            {
                // Create it if it doesn't exist
                File.Create("BannedIPs.xml").Close();
            }

            // Make filestreeam
            FileStream file = File.Create(Environment.CurrentDirectory + "//BannedIPs.xml");

            // Set serializer class
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<string>));

            // Serialize list
            xmlSerializer.Serialize(file, Program.bannedIPs);

            // Close filestream
            file.Close();
        }
        #endregion

        // A seldom used method used to bind all known profiles to every connected user - it essentially asserts all changes made to the profiles into the server
        public static void bindProfilesToConnectedUsers()
        {
            // Goes through every user
            foreach (Program.User user in Program.connectedUsers.ToArray())
            {
                // Matches eveery connected user to its profile
                foreach (Program.Profile profile in Program.profileList)
                {
                    // Identifies by username
                    if (profile.Username == user.Username)
                    {
                        // Profile matches connected user - manually set all settings
                        user.Password = profile.Password;
                        user.PowerLevel = profile.PowerLevel;
                        user.IsBanned = profile.IsBanned;
                        user.IsShredded = profile.IsShredded;
                        user.BanLength = profile.BanLength;
                        user.KnownIPs = profile.KnownIPs;

                        // Modify list by index
                        Program.connectedUsers[Program.connectedUsers.IndexOf(user)] = user;
                    }
                }
            }
        }
    }
}