using Microsoft.Bot.Builder.AI.QnA;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QnABot.Models
{
    public class QnAMakerEndpointEx : QnAMakerEndpoint
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("enable")]
        public bool Enable { get; set; }

        [JsonProperty("sources")]
        public Dictionary<string, Source> Sources { get; set; }
    }
}
