using System;

namespace PimmsArchiveTool
{
    public class ArchiveSettings
    {
        public String SshUsername { get; set; }
        public String SshPassword { get; set; }
        public String DbUsername { get; set; }
        public String DbPassword { get; set; }
        public String DbPath { get; set; }
    }
}
