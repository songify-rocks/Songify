using System;
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
    /// This class is for writing, exporting and importing the config file
    /// The config file is XML and has a single config tag with attributes
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
                if (saveFileDialog.ShowDialog() == true)
                {
                    WriteXml(saveFileDialog.FileName);
                }
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
                writer.WriteAttributeString("atuostart", Settings.Autostart.ToString());
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
                writer.WriteAttributeString("twrewardid", Settings.TwRewardID);
                writer.WriteAttributeString("twsrreward", Settings.TwSRReward.ToString());
                writer.WriteAttributeString("twsrcommand", Settings.TwSRCommand.ToString());
                writer.WriteAttributeString("twsrmaxreq", Settings.TwSRMaxReq.ToString());
                writer.WriteAttributeString("twsrcooldown", Settings.TwSRCooldown.ToString());
                writer.WriteAttributeString("msglogging", Settings.MsgLoggingEnabled.ToString());
                writer.WriteAttributeString("twautoconnect", Settings.TwAutoConnect.ToString());
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            if (hidden)
            {
                // Get file info
                // Put it back as hidden
                myFile.Attributes |= FileAttributes.Hidden;
            }
            else
            {
                // Remove the hidden attribute of the file
                myFile.Attributes &= ~FileAttributes.Hidden;
            }
        }

        public static void ReadXml(string path)
        {
            try
            {
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
                    Settings.Autostart = Convert.ToBoolean(node.Attributes["atuostart"]?.InnerText);
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
                    Settings.TwRewardID = node.Attributes["twrewardid"]?.InnerText;
                    Settings.TwSRReward = Convert.ToBoolean(node.Attributes["twsrreward"]?.InnerText);
                    Settings.TwSRCommand = Convert.ToBoolean(node.Attributes["twsrcommand"]?.InnerText);
                    Settings.TwSRMaxReq = int.Parse(node.Attributes["twsrmaxreq"]?.InnerText);
                    Settings.TwSRCooldown = int.Parse(node.Attributes["twsrcooldown"]?.InnerText);
                    Settings.MsgLoggingEnabled = Convert.ToBoolean(node.Attributes["msglogging"]?.InnerText);
                    Settings.TwAutoConnect = Convert.ToBoolean(node.Attributes["twautoconnect"]?.InnerText);
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
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    ReadXml(openFileDialog.FileName);
                }

                // This will iterate through all windows of the software, if the window is typeof 
                // Settingswindow (from there this class is called) it calls the method SetControls
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() == typeof(SettingsWindow))
                    {
                        ((SettingsWindow)window).SetControls();
                    }
                }
            }
        }
    }
}
