using Songify_Slim.Util.Settings;
using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Xml;
using Application = System.Windows.Application;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace Songify_Slim
{
    /// <summary>
    ///     This class is for writing, exporting and importing the config file
    ///     The config file is XML and has a single config tag with attributes
    /// </summary>
    internal class ConfigHandler
    {
        public static void SaveConfig(string path = "")
        {
            // Saving the Config file
            if (path != "")
            {
                WriteXml(path);
            }
            else
            {
                // Importing the SaveFileDialog and giving it filter, directory and window title
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "XML (*.xml)|*.xml",
                    InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    Title = "Export Config"
                };

                // Opneing the dialog and if the user clicked on "save" this code gets executed
                if (saveFileDialog.ShowDialog() == true) WriteXml(saveFileDialog.FileName);
            }
        }

        public static void WriteXml(string path, bool hidden = false)
        {
            if (!File.Exists(path))
                File.Create(path).Close();
            FileInfo myFile = new FileInfo(path);
            // Remove the hidden attribute of the file
            myFile.Attributes &= ~FileAttributes.Hidden;

            // XML-Writer settings
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                NewLineOnAttributes = true
            };

            // Writing the XML, Attributnames are somewhat equal to Settings.
            using (XmlWriter writer = XmlWriter.Create(path, xmlWriterSettings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Songify_Config");
                writer.WriteStartElement("Config");
                writer.WriteAttributeString("directory", Settings.Directory);
                writer.WriteAttributeString("color", Settings.Color);
                writer.WriteAttributeString("tehme", Settings.Theme);
                writer.WriteAttributeString("autostart", Settings.Autostart.ToString());
                writer.WriteAttributeString("systray", Settings.Systray.ToString());
                writer.WriteAttributeString("customPause", Settings.CustomPauseTextEnabled.ToString());
                writer.WriteAttributeString("customPauseText", Settings.CustomPauseText);
                writer.WriteAttributeString("outputString", Settings.OutputString);
                writer.WriteAttributeString("uuid", Settings.Uuid);
                writer.WriteAttributeString("telemetry", Settings.Telemetry.ToString());
                writer.WriteAttributeString("nbuser", Settings.NbUser);
                writer.WriteAttributeString("nbuserid", Settings.NbUserId);
                writer.WriteAttributeString("uploadSonginfo", Settings.Upload.ToString());
                writer.WriteAttributeString("uploadhistory", Settings.UploadHistory.ToString());
                writer.WriteAttributeString("savehistory", Settings.SaveHistory.ToString());
                writer.WriteAttributeString("downloadcover", Settings.DownloadCover.ToString());
                writer.WriteAttributeString("refreshtoken", Settings.RefreshToken);
                writer.WriteAttributeString("splitoutput", Settings.SplitOutput.ToString());
                writer.WriteAttributeString("accesstoken", Settings.AccessToken);
                writer.WriteAttributeString("twacc", Settings.TwAcc);
                writer.WriteAttributeString("twoauth", Settings.TwOAuth);
                writer.WriteAttributeString("twchannel", Settings.TwChannel);
                writer.WriteAttributeString("twrewardid", Settings.TwRewardId);
                writer.WriteAttributeString("twsrreward", Settings.TwSrReward.ToString());
                writer.WriteAttributeString("twsrcommand", Settings.TwSrCommand.ToString());
                writer.WriteAttributeString("twsrmaxreq", Settings.TwSrMaxReq.ToString());
                writer.WriteAttributeString("twsrcooldown", Settings.TwSrCooldown.ToString());
                writer.WriteAttributeString("msglogging", Settings.MsgLoggingEnabled.ToString());
                writer.WriteAttributeString("twautoconnect", Settings.TwAutoConnect.ToString());
                writer.WriteAttributeString("artistblacklist", Settings.ArtistBlacklist);
                writer.WriteAttributeString("userblacklist", Settings.UserBlacklist);
                writer.WriteAttributeString("posx", Settings.PosX.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("posy", Settings.PosY.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("autoclearqueue", Settings.AutoClearQueue.ToString());
                writer.WriteAttributeString("spotifydeviceid", Settings.SpotifyDeviceId);
                writer.WriteAttributeString("lang", Settings.Language);
                writer.WriteAttributeString("spacesenabled", Settings.AppendSpaces.ToString());
                writer.WriteAttributeString("Spacecount", Settings.SpaceCount.ToString());
                writer.WriteAttributeString("ownApp", Settings.UseOwnApp.ToString());
                writer.WriteAttributeString("clientid", Settings.ClientId);
                writer.WriteAttributeString("clientsecret", Settings.ClientSecret);
                writer.WriteAttributeString("maxsonglength", Settings.MaxSongLength.ToString());
                writer.WriteAttributeString("announceinchat", Settings.AnnounceInChat.ToString());
                writer.WriteAttributeString("botcmdnext", Settings.BotCmdNext.ToString());
                writer.WriteAttributeString("botcmdpos", Settings.BotCmdPos.ToString());
                writer.WriteAttributeString("botcmdsong", Settings.BotCmdSong.ToString());

                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            myFile.Attributes &= ~FileAttributes.Hidden;
        }

        public static void ReadXml(string path)
        {
            try
            {
                if (new FileInfo(path).Length == 0)
                {
                    WriteXml(path);
                    return;
                }

                // reading the XML file, attributes get saved in Settings
                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                if (doc.DocumentElement == null) return;
                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    if (node.Name != "Config") continue;
                    Settings.Directory = node.Attributes["directory"]?.InnerText;
                    Settings.Color = node.Attributes["color"]?.InnerText;
                    Settings.Theme = node.Attributes["tehme"]?.InnerText;
                    Settings.Autostart = Convert.ToBoolean(node.Attributes["autostart"]?.InnerText);
                    Settings.Systray = Convert.ToBoolean(node.Attributes["systray"]?.InnerText);
                    Settings.CustomPauseTextEnabled = Convert.ToBoolean(node.Attributes["customPause"]?.InnerText);
                    Settings.CustomPauseText = node.Attributes["customPauseText"]?.InnerText;
                    Settings.OutputString = node.Attributes["outputString"]?.InnerText;
                    Settings.Uuid = node.Attributes["uuid"]?.InnerText;
                    Settings.Telemetry = Convert.ToBoolean(node.Attributes["telemetry"]?.InnerText);
                    Settings.NbUser = node.Attributes["nbuser"]?.InnerText;
                    Settings.NbUserId = node.Attributes["nbuserid"]?.InnerText;
                    Settings.Upload = Convert.ToBoolean(node.Attributes["uploadSonginfo"]?.InnerText);
                    Settings.UploadHistory = Convert.ToBoolean(node.Attributes["uploadhistory"]?.InnerText);
                    Settings.SaveHistory = Convert.ToBoolean(node.Attributes["savehistory"]?.InnerText);
                    Settings.DownloadCover = Convert.ToBoolean(node.Attributes["downloadcover"]?.InnerText);
                    Settings.RefreshToken = node.Attributes["refreshtoken"].InnerText;
                    Settings.SplitOutput = Convert.ToBoolean(node.Attributes["splitoutput"]?.InnerText);
                    Settings.AccessToken = node.Attributes["accesstoken"]?.InnerText;
                    Settings.TwAcc = node.Attributes["twacc"]?.InnerText;
                    Settings.TwOAuth = node.Attributes["twoauth"]?.InnerText;
                    Settings.TwChannel = node.Attributes["twchannel"]?.InnerText;
                    Settings.TwRewardId = node.Attributes["twrewardid"]?.InnerText;
                    Settings.TwSrReward = Convert.ToBoolean(node.Attributes["twsrreward"]?.InnerText);
                    Settings.TwSrCommand = Convert.ToBoolean(node.Attributes["twsrcommand"]?.InnerText);
                    if (int.TryParse(node.Attributes["twsrmaxreq"]?.InnerText, out int value))
                        Settings.TwSrMaxReq = value;
                    if (int.TryParse(node.Attributes["twsrcooldown"]?.InnerText, out value))
                        Settings.TwSrCooldown = value;
                    Settings.MsgLoggingEnabled = Convert.ToBoolean(node.Attributes["msglogging"]?.InnerText);
                    Settings.TwAutoConnect = Convert.ToBoolean(node.Attributes["twautoconnect"]?.InnerText);
                    Settings.ArtistBlacklist = node.Attributes["artistblacklist"]?.InnerText;
                    Settings.UserBlacklist = node.Attributes["userblacklist"]?.InnerText;
                    if (int.TryParse(node.Attributes["posx"]?.InnerText, out value))
                        Settings.PosX = value;
                    if (int.TryParse(node.Attributes["posy"]?.InnerText, out value))
                        Settings.PosY = value;
                    Settings.AutoClearQueue = Convert.ToBoolean(node.Attributes["autoclearqueue"]?.InnerText);
                    Settings.SpotifyDeviceId = node.Attributes["spotifydeviceid"]?.InnerText;
                    Settings.UserBlacklist = node.Attributes["userblacklist"]?.InnerText;
                    Settings.Language = node.Attributes["lang"]?.InnerText;
                    Settings.AppendSpaces = Convert.ToBoolean(node.Attributes["spacesenabled"]?.InnerText);
                    if (int.TryParse(node.Attributes["Spacecount"]?.InnerText, out value))
                        Settings.SpaceCount = value;
                    Settings.UseOwnApp = Convert.ToBoolean(node.Attributes["ownApp"]?.InnerText);
                    Settings.ClientId = node.Attributes["clientid"]?.InnerText;
                    Settings.ClientSecret = node.Attributes["clientsecret"]?.InnerText;
                    if (int.TryParse(node.Attributes["maxsonglength"]?.InnerText, out value))
                        Settings.MaxSongLength = value;
                    Settings.AnnounceInChat = Convert.ToBoolean(node.Attributes["announceinchat"]?.InnerText);
                    Settings.BotCmdNext = Convert.ToBoolean(node.Attributes["botcmdnext"]?.InnerText);
                    Settings.BotCmdPos = Convert.ToBoolean(node.Attributes["botcmdpos"]?.InnerText);
                    Settings.BotCmdSong = Convert.ToBoolean(node.Attributes["botcmdsong"]?.InnerText);

                }
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
            }
        }

        public static void LoadConfig(string path = "")
        {
            if (path != "")
            {
                ReadXml(path);
            }
            else
            {
                // OpenfileDialog with settings initialdirectory is the path were the exe is located
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    Filter = @"XML files (*.xml)|*.xml|All files (*.*)|*.*"
                };

                // Opening the dialog and when the user hits "OK" the following code gets executed
                if (openFileDialog.ShowDialog() == DialogResult.OK) ReadXml(openFileDialog.FileName);

                // This will iterate through all windows of the software, if the window is typeof 
                // Settingswindow (from there this class is called) it calls the method SetControls
                foreach (Window window in Application.Current.Windows)
                    if (window.GetType() == typeof(Window_Settings))
                        ((Window_Settings)window).SetControls();
            }
        }
    }
}