using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Xml;

namespace Songify_Slim
{
    /// <summary>
    /// This class is for writing, exporting and importing the config file
    /// The config file is XML and has a single config tag with attributes
    /// </summary>

    internal class ConfigHandler
    {

        public static void SaveConfig(string Path = "")
        {
            // Saving the Config file
            if (Path != "")
            {
                WriteXML(Path, false);
            }
            else
            {
                // Importing the SaveFileDialog and giving it filter, directory and window title
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "XML (*.xml)|*.xml",
                    InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    Title = "Export Config"
                };

                // Opneing the dialog and if the user clicked on "save" this code gets executed
                if (saveFileDialog.ShowDialog() == true)
                {
                    WriteXML(saveFileDialog.FileName);
                    Notification.ShowNotification("Config exported to " + saveFileDialog.FileName, "s");
                }
            }
        }

        public static void WriteXML(string Path, bool hidden = false)
        {
            FileInfo myFile = new FileInfo(Path);
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
            using (XmlWriter writer = XmlWriter.Create(Path, xmlWriterSettings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Songify_Config");
                writer.WriteStartElement("Config");
                writer.WriteAttributeString("directory", Settings.GetDirectory());
                writer.WriteAttributeString("color", Settings.GetColor());
                writer.WriteAttributeString("tehme", Settings.GetTheme());
                writer.WriteAttributeString("atuostart", Settings.GetAutostart().ToString());
                writer.WriteAttributeString("systray", Settings.GetSystray().ToString());
                writer.WriteAttributeString("customPause", Settings.GetCustomPauseTextEnabled().ToString());
                writer.WriteAttributeString("customPauseText", Settings.GetCustomPauseText());
                writer.WriteAttributeString("outputString", Settings.GetOutputString());
                writer.WriteAttributeString("uuid", Settings.GetUUID());
                writer.WriteAttributeString("telemetry", Settings.GetTelemetry().ToString());
                writer.WriteAttributeString("nbuser", Settings.GetNBUser().ToString());
                writer.WriteAttributeString("nbuserid", Settings.GetNBUserID().ToString());
                writer.WriteAttributeString("uploadSonginfo", Settings.GetUpload().ToString());
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

        public static void readXML(string path)
        {
            // reading the XML file, attributes get saved in Settings
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                if (node.Name == "Config")
                {
                    Settings.SetDirectory(node.Attributes["directory"]?.InnerText);
                    Settings.SetColor(node.Attributes["color"]?.InnerText);
                    Settings.SetTheme(node.Attributes["tehme"]?.InnerText);
                    Settings.SetAutostart(Convert.ToBoolean(node.Attributes["atuostart"]?.InnerText));
                    Settings.SetSystray(Convert.ToBoolean(node.Attributes["systray"]?.InnerText));
                    Settings.SetCustomPauseTextEnabled(Convert.ToBoolean(node.Attributes["customPause"]?.InnerText));
                    Settings.SetCustomPauseText(node.Attributes["customPauseText"]?.InnerText);
                    Settings.SetOutputString(node.Attributes["outputString"]?.InnerText);
                    Settings.SetUUID(node.Attributes["uuid"]?.InnerText);
                    Settings.SetTelemetry(Convert.ToBoolean(node.Attributes["telemetry"]?.InnerText));
                    Settings.SetNBUser(node.Attributes["nbuser"]?.InnerText);
                    Settings.SetNBUserID(node.Attributes["nbuserid"]?.InnerText);
                    Settings.SetUpload(Convert.ToBoolean(node.Attributes["uploadSonginfo"]?.InnerText));
                }
            }
        }

        public static void LoadConfig(string Path = "")
        {
            if (Path != "")
            {
                readXML(Path);
            }
            else
            {
                // OpenfileDialog with settings initialdirectory is the path were the exe is located
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*"
                };

                // Opening the dialog and when the user hits "OK" the following code gets executed
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    readXML(openFileDialog.FileName);
                }

                // This will iterate through all windows of the software, if the window is typeof 
                // Settingswindow (from there this class is called) it calls the method SetControls
                foreach (Window window in System.Windows.Application.Current.Windows)
                {
                    if (window.GetType() == typeof(SettingsWindow))
                    {
                        (window as SettingsWindow).SetControls();
                    }
                }
            }
        }
    }
}
