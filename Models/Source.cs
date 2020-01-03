using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QnABot.Models
{
    public class Source
    {
        /// <summary>
        /// Editorial: Id is name, description is original file (Excel), custom etc.
        /// File: Id is file name, description is full path etc.
        /// Url: Id is url, description is title etc.
        /// </summary>
        public string Id { get; set; }

        public string Description { get; set; }

        public SourceType Type { get; set; }
    }
}
