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
            ResultNumber = new ModelPropertySame<int>(storage, botStateSet, 3, "resultNumber");
            QnAs = new ModelPropertyListEndpoint(storage, botStateSet, new List<QnAMakerEndpoint>(), "qnAs");
            MinScore = new ModelPropertySame<float>(storage, botStateSet, 0.5f, "minScore");
            AnswerLg = new ModelPropertyTemplateEngine(storage, botStateSet, null, "answerLg");
            Debug = new ModelPropertySame<bool>(storage, botStateSet, false, "debug");
        }

        public ModelPropertySame<int> ResultNumber { get; set; }

        public ModelPropertyListEndpoint QnAs { get; set; }

        public ModelPropertySame<float> MinScore { get; set; }

        public ModelPropertyTemplateEngine AnswerLg { get; set; }

        public ModelPropertySame<bool> Debug { get; set; }
    }
}
