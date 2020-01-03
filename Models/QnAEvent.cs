using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QnABot.Models
{
    public class QnAEvent
    {
        // QnA
        public const string GetQnA = "GetQnA";

        public const string EnableQnA = "EnableQnA";

        public const string CreateQnA = "CreateQnA";

        public const string AddQnA = "AddQnA";

        public const string DelQnA = "DelQnA";

        // Source
        public const string AddSource = "AddSource";

        public const string DelSource = "DelSource";

        // Configs
        public const string SetMinScore = "SetMinScore";

        public const string SetResultNumber = "SetResultNumber";

        // Answer Lg
        public const string SetAnswerLg = "SetAnswerLg";

        public const string TestAnswerLg = "TestAnswerLg";

        // Others
        public const string SetDebug = "SetDebug";
    }
}
