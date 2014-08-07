using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace PimmsArchiveTool
{
    // Type for list of servers as read from xml file
    public class ServerList
    {
        [XmlElement("PimmsServer")]
        public List<PimmsServer> server = new List<PimmsServer>(); 
    }

    // Type to define per-server settings
    public class PimmsServer
    {
        public String Name { get; set; }
        public String PKRD { get; set; }
        public String IpAddress { get; set; }
        public String VideoStorePath { get; set; }

        public override string ToString()
        {
            String _string = Name + ", " + IpAddress + ", " + VideoStorePath;
            return _string;
        }
    }

    // Type for global settings
    public class ArchiveSettings
    {
        public String SshUsername { get; set; }
        public String SshPassword { get; set; }
        public String DbUsername { get; set; }
        public String DbPassword { get; set; }
        public String DbPath { get; set; }
    }

    // Type for results from firebird db IMAGE_CAPTURE_REGISTER query
    public class DbResult
    {
        public String PictureReference { get; set; }
        public String RideId { get; set; }
        public String ArtefactId { get; set; }
        public String Status { get; set; }
    }
}