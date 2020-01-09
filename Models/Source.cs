using Newtonsoft.Json;
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
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "type")]
        public SourceType Type { get; set; }
    }
}
