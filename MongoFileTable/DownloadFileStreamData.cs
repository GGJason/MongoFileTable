using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MongoFileTable
{
    public class DownloadFileStreamData
    {
        public DownloadFileStreamData(string Name, Stream stream, string Extension = "")
        {
            if (Extension == "")
            {
                this.Extension = Name.Split('.').Last();
            }
            this.Name = Name;
            this.Stream = stream;
        }
        public string Name;
        public Stream Stream;
        public string Extension = "";
    }
}