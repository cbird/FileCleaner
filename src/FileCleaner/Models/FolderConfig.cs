using System.Collections.Generic;

namespace FileCleaner.Models
{
    public class FolderConfig
    {
        public string Path { get; set; }
        public int NbrOfDaysOld { get; set; }
        public bool Recursive { get; set; }
        public List<string> ExcludeExtensions { get; set; }

        public FolderConfig Copy(string path)
        {
            return new FolderConfig
            {
                Path = path,
                ExcludeExtensions = this.ExcludeExtensions,
                Recursive = this.Recursive,
                NbrOfDaysOld = this.NbrOfDaysOld
            };
        }
    }
}