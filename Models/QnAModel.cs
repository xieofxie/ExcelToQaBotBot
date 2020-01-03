using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.LanguageGeneration;
using QnABot.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace QnABot.Models
{
    public class QnAModel
    {
        public QnAModel(
            IStorage storage,
            BotStateSet botStateSet)
        {
            // TODO when saved to cosmos, it is long and can't be cast to int
            ResultNumber = new ModelPropertySame<long>(storage, botStateSet, 3L, "resultNumber");
            QnAs = new ModelPropertyAllEndpoint(storage, botStateSet, new Dictionary<string, QnAMakerEndpointEx>(), "qnAs");
            MinScore = new ModelPropertySame<float>(storage, botStateSet, 0.5f, "minScore");
            AnswerLg = new ModelPropertyTemplateEngine(storage, botStateSet, null, "answerLg");
            Debug = new ModelPropertySame<bool>(storage, botStateSet, !false, "debug");
        }

        public ModelPropertySame<long> ResultNumber { get; set; }

        public ModelPropertyAllEndpoint QnAs { get; set; }

        public ModelPropertySame<float> MinScore { get; set; }

        public ModelPropertyTemplateEngine AnswerLg { get; set; }

        public ModelPropertySame<bool> Debug { get; set; }
    }
}
