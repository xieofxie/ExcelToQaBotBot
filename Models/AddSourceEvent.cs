using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QnABot.Models
{
    public class AddSourceEvent : UpdateKbOperationDTOAdd
    {
        [JsonProperty(PropertyName = "knowledgeBaseId")]
        public string KnowledgeBaseId { get; set; }

        [JsonProperty(PropertyName = "qnaListId")]
        public string QnaListId { get; set; }

        [JsonProperty(PropertyName = "qnaListDescription")]
        public string QnaListDescription { get; set; }

        [JsonProperty(PropertyName = "urlsDescription")]
        public IList<string> UrlsDescription { get; set; }

        [JsonProperty(PropertyName = "filesDescription")]
        public IList<string> FilesDescription { get; set; }
    }
}
