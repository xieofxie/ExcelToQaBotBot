using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace QnABot.Models
{
    public class FileHosted
    {
        public FileHosted()
        {
            UploadTime = DateTime.Now;
        }

        public string Name { get; set; }

        public string Type { get; set; }

        public Stream Stream { get; set; }

        public DateTime UploadTime { get; set; }

        public MemoryStream GetStreamCopy()
        {
            var stream = new MemoryStream();
            Stream.Position = 0;
            Stream.CopyTo(stream);
            stream.Position = 0;
            return stream;
        }

        public bool IsExpired()
        {
            return (DateTime.Now - UploadTime) >= new TimeSpan(0, 10, 0);
        }
    }

    public class FileHost : Dictionary<string, FileHosted>
    {
        public FileHosted GetNotExpired(string id)
        {
            var data = this[id];
            if (data.IsExpired())
            {
                Remove(id);
                // TODO Mock not found
                data = this[id];
            }
            return data;
        }
    }
}
