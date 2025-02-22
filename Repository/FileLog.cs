using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace filerename.v1.Repository
{
    internal class FileLog
    {
        public string OriginalName { get; set; }
        public string NewName { get; set; }
        public DateTime Timestamp { get; set; }
        public string Filepath { get; set; }
    }
}
