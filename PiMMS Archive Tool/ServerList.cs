using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace PimmsArchiveTool
{
    /// <summary>
    /// Classes to contain a list of PiMMS server configuration details as read from an xml file
    /// </summary>
    public class ServerList
    {
        [XmlElement("PimmsServer")]
        public List<PimmsServer> server = new List<PimmsServer>(); 
    }

    public class PimmsServer
    {
        public String Name { get; set; }
        public String IpAddress { get; set; }
        public String VideoStorePath { get; set; }

        public override string ToString()
        {
            String _string = Name + ", " + IpAddress + ", " + VideoStorePath;
            return _string;
        }
    }
}