using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QnABot.Models
{
    public class EnableQnAEvent
    {
        public string KnowledgeBaseId { get; set; }

        public bool Enable { get; set; }
    }
}
