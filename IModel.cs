using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTLocalToAzure
{
    public class ISettings
    {
        public string FilePath { get; set; }
        public string SFTPConnString { get; set; }
        public string SFTPUsername { get; set; }
        public string SFTPPassword { get; set; }
    }
}
