using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileCleaner.Models
{
    public class Config
    {
        public int MaxThreadCount { get; set; }
        public List<FolderConfig> Folders { get; set; }
    }
}
