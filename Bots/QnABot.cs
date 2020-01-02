// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Schema;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using QnABot.Models;
using QnABot.Responses;

namespace Microsoft.BotBuilderSamples
{
    public class QnABot : ActivityHandler
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<QnABot> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly QnAModel _model;

        public QnABot(
            IConfiguration configuration,
            ILogger<QnABot> logger,
            IHttpClientFactory httpClientFactory,
            QnAModel model)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _model = model;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var qnAs = _model.QnAs.Get();
            var resultNumber = _model.ResultNumber.Get();
            var minScore = _model.MinScore.Get();

            var tasks = new List<Task<QueryResult[]>>();

            var options = new QnAMakerOptions {
                ScoreThreshold = minScore,
                Top = resultNumber
            };

            foreach (var qnA in qnAs)
            {
                var httpClient = _httpClientFactory.CreateClient();

                var qnaMaker = new QnAMaker(qnA, null, httpClient);

                // The actual call to the QnA Maker service.
                tasks.Add(qnaMaker.GetAnswersAsync(turnContext, options));
            }

            Task.WaitAll(tasks.ToArray());

            var results = tasks.Select(task => task.Result).SelectMany(result => result).OrderByDescending(result => result.Score).Take(resultNumber).ToArray();
            await turnContext.SendActivityAsync(Answer.CreateAnswer(_model, results));
        }

        protected override async Task OnEventAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            switch (turnContext.Activity.Name)
            {
                case QnAEvent.SetMinScore:
                    {
                        var value = Convert.ToSingle(turnContext.Activity.Value);
                        await _model.MinScore.Set(turnContext, value);
                        Send(turnContext, $"SetMinScore {value}");
                        break;
                    }
                case QnAEvent.SetQnA:
                    {
                        var value = (turnContext.Activity.Value as JArray).ToObject<List<QnAMakerEndpoint>>();
                        await _model.QnAs.Set(turnContext, value);
                        Send(turnContext, $"SetQnA {value.Count}");
                        break;
                    }
                case QnAEvent.SetResultNumber:
                    {
                        var value = Convert.ToInt32(turnContext.Activity.Value);
                        await _model.ResultNumber.Set(turnContext, value);
                        Send(turnContext, $"SetResultNumber {value}");
                        break;
                    }
                case QnAEvent.SetAnswerLg:
                    {
                        var template = turnContext.Activity.Value.ToString();
                        await _model.AnswerLg.Set(turnContext, template);
                        Send(turnContext, $"SetAnswerLg {template}");
                        break;
                    }
                case QnAEvent.SetDebug:
                    {
                        var value = Convert.ToBoolean(turnContext.Activity.Value);
                        await _model.Debug.Set(turnContext, value);
                        Send(turnContext, $"SetDebug {value}");
                        break;
                    }
                case QnAEvent.TestAnswerLg:
                    {
                        var template = turnContext.Activity.Value.ToString();
                        var engine = new TemplateEngine();
                        engine.AddText(template, importResolver: ModelPropertyTemplateEngine.importResolverDelegate);
                        var data = new
                        {
                            data = new
                            {
                                debug = _model.Debug.Get(),
                                results = new List<QueryResult>(),
                                indices = new List<int>()
                            }
                        };
                        var answer = engine.EvaluateTemplate(Answer.Entrance, data);
                        var activity = ActivityFactory.CreateActivity(answer);
                        await turnContext.SendActivityAsync(activity);

                        data.data.results.Add(new QueryResult { Answer = "Answer 0" });
                        data.data.indices.Add(0);
                        answer = engine.EvaluateTemplate(Answer.Entrance, data);
                        activity = ActivityFactory.CreateActivity(answer);
                        await turnContext.SendActivityAsync(activity);

                        var total = _model.ResultNumber.Get();
                        if (total != 1)
                        {
                            for (int i = 1; i < total;++i)
                            {
                                data.data.results.Add(new QueryResult { Answer = $"Answer {i}" });
                                data.data.indices.Add(i);
                            }
                            answer = engine.EvaluateTemplate(Answer.Entrance, data);
                            activity = ActivityFactory.CreateActivity(answer);
                            await turnContext.SendActivityAsync(activity);
                        }
                        break;
                    }
            }
        }

        private void Send(ITurnContext context, string debug)
        {
            if (_model.Debug.Get())
            {
                context.SendActivityAsync(debug);
            }
        }
    }
}
