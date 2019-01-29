using System;
using System.Windows.Forms;
using System.Xml;

namespace Songify_Slim
{
    internal class ConfigHandler
    {
        public static void SaveConfig(string Path = "")
        {
            if (Path != "")
            {
                WriteXML(Path);
            }
            else
            {
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "XML (*.xml)|*.xml",
                    InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    Title = "Export Config"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    WriteXML(saveFileDialog.FileName);
                    Notification.ShowNotification("Config exported to " + saveFileDialog.FileName, "s");
                }
            }
        }

        public static void WriteXML(string Path)
        {
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                NewLineOnAttributes = false
            };

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
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }

        public static void LoadConfig()
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog
            {
                InitialDirectory = System.AppDomain.CurrentDomain.BaseDirectory,
                Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(openFileDialog.FileName);
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
                    }
                }
            }

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
