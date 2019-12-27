using AdaptiveCards;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using QnABot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QnABot.Responses
{
    public class Answer
    {
        public static readonly string Entrance = "CreateAnswer";

        public static Activity CreateAnswer(QnAModel model, QueryResult[] results)
        {
            var engine = model.AnswerLg;
            if (engine != null)
            {
                var data = new
                {
                    data = new
                    {
                        debug = model.Debug,
                        results,
                        // TODO wait for indicesAndValues
                        indices = Enumerable.Range(0, results.Length).ToArray()
                    }
                };
                var answer = engine.EvaluateTemplate(Answer.Entrance, data);
                var act = ActivityFactory.CreateActivity(answer);
                return act;
            }

            var activity = new Activity()
            {
                Type = ActivityTypes.Message
            };

            if (results.Length == 0)
            {
                activity.Text = "No QnA Maker answers are found.";
            }
            else
            {
                bool debug = model.Debug;

                var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 2));
                for (int i = 0;i < results.Length;++i)
                {
                    var id = $"Answer{i}";
                    card.Body.Add(new AdaptiveTextBlock
                    {
                        Id = id,
                        Wrap = true,
                        Text = debug ? $"Score: {results[i].Score}\r\n{results[i].Answer}" : results[i].Answer,
                        IsVisible = i == 0
                    });
                    if (i != 0)
                    {
                        card.Actions.Add(new AdaptiveToggleVisibilityAction
                        {
                            Title = $"Less relevant {i}",
                            TargetElements = new List<AdaptiveTargetElement> { new AdaptiveTargetElement(id) }
                        });
                    }
                }
                var attachment = new Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = card
                };
                activity.Attachments = new Attachment[] { attachment };
            }

            return activity;
        }
    }
}
