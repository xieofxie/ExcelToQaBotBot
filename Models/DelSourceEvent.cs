using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QnABot.Models
{
    public class DelSourceEvent
    {
        [JsonProperty(PropertyName = "knowledgeBaseId")]
        public string KnowledgeBaseId { get; set; }

        [JsonProperty(PropertyName = "ids")]
        public IList<string> Ids { get; set; }
    }
}
