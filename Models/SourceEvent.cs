using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QnABot.Models
{
    public class SourceEvent : Source
    {
        public string KnowledgeBaseId { get; set; }

        public List<QnADTO> QnaList { get; set; }
    }
}
