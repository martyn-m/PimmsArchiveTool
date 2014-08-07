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
            PicRefRadio.IsChecked = true;
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

        private void DbQueryButton_Click(object sender, RoutedEventArgs e)
        {
            // Build the DB query
            String dbQuery = "SELECT r.PICTUREREFERENCE, r.RIDEID, r.ARTEFACTID, r.VIDEOSTATUS " +
                            "FROM IMAGE_CAPTURE_REGISTER r ";
            if (PicRefRadio.IsChecked == true)
            {
                dbQuery += "WHERE r.PICTUREREFERENCE containing '" + GetQueryPicRef() + "'";
            }
            else
            {
                dbQuery += "WHERE r.PICTUREREFERENCE CONTAINING '" + GetQueryId() +
                            "' AND r.PICTUREREFERENCE CONTAINING '" + GetQueryDate() + "'";  
            }
            
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

                DebugTextBox.AppendText(dbQuery + "\n");
                
                FbCommand readCommand = new FbCommand(dbQuery, connection);

                using (FbDataReader reader = readCommand.ExecuteReader())
                {
                    List<DbResult> results = new List<DbResult>();
                    while (reader.Read())
                    {
                        results.Add(new DbResult() {
                                PictureReference = reader.GetString(0), 
                                RideId = reader.GetString(1),
                                ArtefactId = reader.GetString(2), 
                                Status = reader.GetString(3)
                        });
                    }
                    
                    // Push the results to the datagrid
                    DbQueryDataGrid.ItemsSource = results;
                    DbQueryDataGrid.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                DebugTextBox.AppendText(ex.ToString());
                DebugTextBox.ScrollToEnd();
            }
        }

        private string GetQueryPicRef()
        {
            // TO DO: check for SQL injection nastiness
            return PicRefTextBox.Text;
        }

        private string GetQueryId()
        {
            int picId = 0;

            // check that text contains only an integer number
            try
            {
                picId = Int32.Parse(PicIdTextBox.Text);
            }
            catch (Exception)
            {
                MessageBox.Show("Please enter an integer Picture ID");
            }

            // pad to 4 characters and return
            return picId.ToString("d4");
        }

        private string GetQueryDate()
        {
            DateTime? date = PicIdDatePicker.SelectedDate;
            // TO DO: catch null date

            String queryDate = date.Value.Month.ToString("X") +
                date.Value.Day.ToString("d2");
             
            return queryDate;
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            DbResult result = (DbResult)DbQueryDataGrid.SelectedItem;
            

            try
            {
                String searchCommand = "ls -1 " + currentServer.VideoStorePath + "/Ride." +
                                    result.RideId + "." + result.ArtefactId + ".*";

                DebugTextBox.AppendText(searchCommand + "\n");
                
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
                    SshCommand cmd = ssh.CreateCommand(searchCommand);
                    IAsyncResult asynch = cmd.BeginExecute(delegate(IAsyncResult ar)
                    {
                        //DebugTextBox.AppendText("Finished\n");
                        //DebugTextBox.ScrollToEnd();
                    }, null);

                    StreamReader reader = new StreamReader(cmd.OutputStream);
                    List<String> files = new List<String>();

                    while (!asynch.IsCompleted)
                    {
                        String sshOutput;
                        while ((sshOutput = reader.ReadLine()) != null)
                        {
                            files.Add(sshOutput);
                        }
                    }
                    cmd.EndExecute(asynch);
                    FilesListBox.ItemsSource = files;
                }
            }
            catch (NullReferenceException)
            {
                // No results were selected
                MessageBox.Show("Please select a query result");
            }
            
        }

        private void PicRefRadio_Checked(object sender, RoutedEventArgs e)
        {
            // Enable the picref textbox
            PicRefTextBox.IsEnabled = true;
            // Disable the pic id textbox and date picker
            PicIdTextBox.IsEnabled = false;
            PicIdDatePicker.IsEnabled = false;
            //DebugTextBox.AppendText("PicRefRadio_Checked\n");
        }

        private void DateIdRadio_Checked(object sender, RoutedEventArgs e)
        {
            // Enable the pic id / date controls
            PicIdDatePicker.IsEnabled = true;
            PicIdTextBox.IsEnabled = true;
            // Disable the picref textbox
            PicRefTextBox.IsEnabled = false;
        }

        // Run DB Query if enter is pressed in the picture reference text box
        private void PicRefTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                DbQueryButton_Click(sender, e);
            }
        }

        // Run DB Query if enter is pressed in the picture reference text box
        private void PicIdTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                DbQueryButton_Click(sender, e);
            }
        }
    }
}
