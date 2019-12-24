﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
using Microsoft.Bot.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using QnABot.Models;

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
            var qnAs = _model.QnAs;
            var resultNumber = _model.ResultNumber;
            var minScore = _model.MinScore;

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

            if (results.Length == 0)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(_model.NoResultResponse), cancellationToken);
            }
            else
            {
                var response = new StringBuilder();
                foreach (var result in results)
                {
                    response.Append(result.Answer);
                    if (_model.Debug)
                    {
                        response.AppendLine();
                        response.Append(result.Score);
                    }

                    response.AppendLine();
                }
                await turnContext.SendActivityAsync(MessageFactory.Text(response.ToString()), cancellationToken);
            }
        }

        protected override Task OnEventAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            switch (turnContext.Activity.Name)
            {
                case QnAEvent.SetMinScore:
                    {
                        _model.MinScore = Convert.ToSingle(turnContext.Activity.Value);
                        Send(turnContext, $"SetMinScore {_model.MinScore}");
                        break;
                    }
                case QnAEvent.SetQnA:
                    {
                        _model.QnAs = (turnContext.Activity.Value as JArray).ToObject<List<QnAMakerEndpoint>>();
                        Send(turnContext, $"SetQnA {_model.QnAs.Count}");
                        break;
                    }
                case QnAEvent.SetResultNumber:
                    {
                        _model.ResultNumber = Convert.ToInt32(turnContext.Activity.Value);
                        Send(turnContext, $"SetResultNumber {_model.ResultNumber}");
                        break;
                    }
                case QnAEvent.SetNoResultResponse:
                    {
                        _model.NoResultResponse = turnContext.Activity.Value.ToString();
                        Send(turnContext, $"SetNoResultResponse {_model.NoResultResponse}");
                        break;
                    }
                case QnAEvent.SetDebug:
                    {
                        _model.Debug = Convert.ToBoolean(turnContext.Activity.Value);
                        Send(turnContext, $"SetDebug {_model.Debug}");
                        break;
                    }
            }

            return Task.CompletedTask;
        }

        private void Send(ITurnContext context, string debug)
        {
            if (_model.Debug)
            {
                context.SendActivityAsync(debug);
            }
        }
    }
}