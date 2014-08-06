using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Renci.SshNet;
using System.IO;
using System.Xml.Serialization;
using FirebirdSql.Data.FirebirdClient;

namespace PimmsArchiveTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ConnectionInfo connectionInfo;
        ServerList serverList;
        ArchiveSettings settings;
        PimmsServer currentServer;

        public MainWindow()
        {
            InitializeComponent();
            
            settings = ReadSettings();
            serverList = ReadServers();
        }

        /// <summary>
        /// Reads the list of PiMMS servers from xml file 'servers.xml' in the same directory as the application
        /// </summary>
        /// <returns>A ServerList object containing a list of PimmsServer objects (ServerList.server)</returns>
        private ServerList ReadServers()
        {
            try
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(ServerList));
                ServerList serverList;
                using (TextReader reader = new StreamReader("servers.xml"))
                {
                    object obj = deserializer.Deserialize(reader);
                    serverList = (ServerList)obj;
                }

                return serverList;
            }
            catch (Exception ex)
            {
                DebugTextBox.AppendText(ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Reads in global settings from an xml file 'settings.xml' in the same directory as the application
        /// </summary>
        /// <returns>An ArchiveSettings object</returns>
        private ArchiveSettings ReadSettings()
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(ArchiveSettings));
            ArchiveSettings settings;
            using (TextReader reader = new StreamReader("settings.xml"))
            {
                object obj = deserializer.Deserialize(reader);
                settings = (ArchiveSettings)obj;
            }

            return settings;
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            // Set up the connection details
            // TO DO: sanity check the input
            connectionInfo = new PasswordConnectionInfo(currentServer.IpAddress, settings.SshUsername, settings.SshPassword);
            
            using (SshClient ssh = new SshClient(connectionInfo))
            {
                try
                {
                    ssh.Connect();
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    // Couldn't connect to remote host, either a wrong IP or network down
                    DebugTextBox.AppendText(ex.ToString());
                    DebugTextBox.ScrollToEnd();
                }
                catch (Renci.SshNet.Common.SshAuthenticationException ex)
                {
                    // Failed Authentication
                    DebugTextBox.AppendText(ex.ToString());
                    DebugTextBox.ScrollToEnd();
                }
                catch (Exception ex)
                {
                    // Something else went wrong
                    DebugTextBox.AppendText(ex.ToString());
                    DebugTextBox.ScrollToEnd();
                }
                
                // TO DO: check that the command isn't blank, otherwise we get a System.ArgumentException
                // TO DO: read up on asynchronous execution
                SshCommand cmd = ssh.CreateCommand(CommandTextBox.Text);
                IAsyncResult asynch = cmd.BeginExecute(delegate(IAsyncResult ar)
                {
                    //DebugTextBox.AppendText("Finished\n");
                    //DebugTextBox.ScrollToEnd();
                }, null);

                StreamReader reader = new StreamReader(cmd.OutputStream);

                while (!asynch.IsCompleted)
                {
                    String result = reader.ReadToEnd();
                    if (string.IsNullOrEmpty(result))
                        continue;
                    DebugTextBox.AppendText(result);
                    DebugTextBox.ScrollToEnd();
                }
                cmd.EndExecute(asynch);
            }
        }

        private void ReadSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void ReadServersButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void ServerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentServer = serverList.server[ServerComboBox.SelectedIndex];
            //DebugTextBox.AppendText(currentServer.ToString() + "\n");
            //DebugTextBox.ScrollToEnd();
        }

        private void ServerComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            ServerComboBox.ItemsSource = serverList.server;
            ServerComboBox.DisplayMemberPath = "Name";
            ServerComboBox.SelectedIndex = 0;
        }

        private void CommandTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ConnectButton_Click(sender, e);
            }
        }

        private void DbQueryButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a connection string using the stored DB details and the current server IP
            FbConnectionStringBuilder builder = new FbConnectionStringBuilder();
            builder.DataSource = currentServer.IpAddress;
            builder.Database = settings.DbPath;
            builder.UserID = settings.DbUsername;
            builder.Password = settings.DbPassword;

            // Create a new firebird connection
            FbConnection connection = new FbConnection(builder.ConnectionString);
            try
            {
                connection.Open();
           
                FbCommand readCommand = new FbCommand(
                        "SELECT r.ID, r.SSID, r.ENCRYPTION, r.WIFIKEY, r.IPADDRESS" +
                        " FROM AP_CONFIG r", connection);
                using (FbDataReader reader = readCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DebugTextBox.AppendText(reader[1].ToString());
                        DebugTextBox.ScrollToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                DebugTextBox.AppendText(ex.ToString());
                DebugTextBox.ScrollToEnd();
            }
        }
    }
}
