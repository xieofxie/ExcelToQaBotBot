// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
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
        private readonly Knowledgebase _knowledgebase;
        private readonly Operations _operations;
        private readonly QnAMakerEndpoint _qnAMakerEndpoint;
        private readonly QnAModel _model;

        public QnABot(
            IConfiguration configuration,
            ILogger<QnABot> logger,
            IHttpClientFactory httpClientFactory,
            Knowledgebase knowledgebase,
            Operations operations,
            QnAMakerEndpoint qnAMakerEndpoint,
            QnAModel model)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _knowledgebase = knowledgebase;
            _operations = operations;
            _qnAMakerEndpoint = qnAMakerEndpoint;
            _model = model;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var qnAs = _model.QnAs.Get();
            var resultNumber = (int)_model.ResultNumber.Get();
            var minScore = _model.MinScore.Get();

            var tasks = new List<Task<QueryResult[]>>();

            var options = new QnAMakerOptions {
                ScoreThreshold = minScore,
                Top = resultNumber
            };

            foreach (var qnA in qnAs)
            {
                if (!qnA.Value.Enable)
                {
                    continue;
                }

                var httpClient = _httpClientFactory.CreateClient();

                var qnaMaker = new QnAMaker(qnA.Value, null, httpClient);

                // The actual call to the QnA Maker service.
                tasks.Add(qnaMaker.GetAnswersAsync(turnContext, options));
            }

            Task.WaitAll(tasks.ToArray());

            var results = tasks.Select(task => task.Result).SelectMany(result => result).OrderByDescending(result => result.Score).Take(resultNumber).ToArray();
            await turnContext.SendActivityAsync(Answer.CreateAnswer(_model, results), cancellationToken);
        }

        protected override async Task OnEventAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            switch (turnContext.Activity.Name)
            {
                // QnA
                case QnAEvent.GetQnA:
                    {
                        await HandleGetQnA(turnContext, cancellationToken);
                        break;
                    }
                case QnAEvent.EnableQnA:
                    {
                        var value = (turnContext.Activity.Value as JObject).ToObject<QnAMakerEndpointEx>();
                        var qnA = new Dictionary<string, QnAMakerEndpointEx>(_model.QnAs.Get());
                        qnA[value.KnowledgeBaseId].Enable = value.Enable;
                        await _model.QnAs.Set(turnContext, qnA, cancellationToken);
                        await SendDebug(turnContext, $"{QnAEvent.EnableQnA} {value.Enable}", cancellationToken);

                        await HandleGetQnA(turnContext, cancellationToken);
                        break;
                    }
                case QnAEvent.CreateQnA:
                    {
                        var value = (turnContext.Activity.Value as JObject).ToObject<CreateKbDTO>();
                        var operation = await _knowledgebase.CreateAsync(value, cancellationToken);
                        operation = await WaitForOperation(operation, QnAEvent.CreateQnA, cancellationToken);

                        var id = operation.ResourceLocation.Split('/')[2];
                        var ex = new QnAMakerEndpointEx
                        {
                            KnowledgeBaseId = id,
                            Host = _qnAMakerEndpoint.Host,
                            EndpointKey = _qnAMakerEndpoint.EndpointKey,
                            Name = value.Name,
                            Enable = false,
                            Sources = new Dictionary<string, Source>(),
                        };
                        await HandleAddQnA(turnContext, ex, cancellationToken);

                        await HandleGetQnA(turnContext, cancellationToken);
                        break;
                    }
                case QnAEvent.AddQnA:
                    {
                        var value = (turnContext.Activity.Value as JObject).ToObject<QnAMakerEndpointEx>();
                        await HandleAddQnA(turnContext, value, cancellationToken);

                        await HandleGetQnA(turnContext, cancellationToken);
                        break;
                    }
                case QnAEvent.DelQnA:
                    {
                        var value = (turnContext.Activity.Value as JObject).ToObject<QnAMakerEndpoint>();
                        await _knowledgebase.DeleteAsync(value.KnowledgeBaseId, cancellationToken);

                        var qnA = new Dictionary<string, QnAMakerEndpointEx>(_model.QnAs.Get());
                        qnA.Remove(value.KnowledgeBaseId);
                        await _model.QnAs.Set(turnContext, qnA, cancellationToken);
                        await SendDebug(turnContext, $"{QnAEvent.DelQnA} {value.KnowledgeBaseId}", cancellationToken);

                        await HandleGetQnA(turnContext, cancellationToken);
                        break;
                    }
                case QnAEvent.UpdateQnA:
                    {
                        var value = (turnContext.Activity.Value as JObject).ToObject<QnAMakerEndpointEx>();
                        var operation = await _knowledgebase.UpdateAsync(value.KnowledgeBaseId, new UpdateKbOperationDTO(update: new UpdateKbOperationDTOUpdate(value.Name)), cancellationToken);
                        operation = await WaitForOperation(operation, QnAEvent.UpdateQnA, cancellationToken);
                        await SendDebug(turnContext, $"{QnAEvent.UpdateQnA} {value.Name}", cancellationToken);

                        var qnA = new Dictionary<string, QnAMakerEndpointEx>(_model.QnAs.Get());
                        qnA[value.KnowledgeBaseId].Name = value.Name;
                        await _model.QnAs.Set(turnContext, qnA, cancellationToken);

                        await HandleGetQnA(turnContext, cancellationToken);
                        break;
                    }
                // Source
                // TODO if same, will delete first
                case QnAEvent.AddSource:
                    {
                        var value = (turnContext.Activity.Value as JObject).ToObject<SourceEvent>();
                        if (value.Type == SourceType.Editorial)
                        {
                            foreach (var qna in value.DTOAdd.QnaList)
                            {
                                qna.Source = value.Id;
                            }
                        }

                        var qnA = new Dictionary<string, QnAMakerEndpointEx>(_model.QnAs.Get());
                        if (qnA[value.KnowledgeBaseId].Sources.ContainsKey(value.Id))
                        {
                            await HandleDelSource(turnContext, value, cancellationToken);
                        }

                        var operation = await _knowledgebase.UpdateAsync(value.KnowledgeBaseId, new UpdateKbOperationDTO(value.DTOAdd), cancellationToken);
                        operation = await WaitForOperation(operation, QnAEvent.AddSource, cancellationToken);

                        await _knowledgebase.PublishAsync(value.KnowledgeBaseId, cancellationToken);

                        qnA = new Dictionary<string, QnAMakerEndpointEx>(_model.QnAs.Get());
                        var source = new Source
                        {
                            Id = value.Id,
                            Description = value.Description,
                            Type = value.Type
                        };
                        qnA[value.KnowledgeBaseId].Sources.Add(source.Id, source);
                        await _model.QnAs.Set(turnContext, qnA, cancellationToken);
                        await SendDebug(turnContext, $"{QnAEvent.AddSource} {value.Id}", cancellationToken);

                        await HandleGetQnA(turnContext, cancellationToken);
                        break;
                    }
                case QnAEvent.DelSource:
                    {
                        var value = (turnContext.Activity.Value as JObject).ToObject<SourceEvent>();
                        await HandleDelSource(turnContext, value, cancellationToken);

                        await _knowledgebase.PublishAsync(value.KnowledgeBaseId, cancellationToken);

                        await HandleGetQnA(turnContext, cancellationToken);
                        break;
                    }
                // Configs
                case QnAEvent.SetMinScore:
                    {
                        var value = Convert.ToSingle(turnContext.Activity.Value);
                        await _model.MinScore.Set(turnContext, value, cancellationToken);
                        await SendDebug(turnContext, $"SetMinScore {value}", cancellationToken);
                        break;
                    }
                case QnAEvent.SetResultNumber:
                    {
                        var value = Convert.ToInt32(turnContext.Activity.Value);
                        await _model.ResultNumber.Set(turnContext, value, cancellationToken);
                        await SendDebug(turnContext, $"SetResultNumber {value}", cancellationToken);
                        break;
                    }
                // Answer Lg
                case QnAEvent.SetAnswerLg:
                    {
                        var template = turnContext.Activity.Value.ToString();
                        await _model.AnswerLg.Set(turnContext, template, cancellationToken);
                        await SendDebug(turnContext, $"SetAnswerLg {template}", cancellationToken);
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
                        await turnContext.SendActivityAsync(activity, cancellationToken);

                        data.data.results.Add(new QueryResult { Answer = "Answer 0" });
                        data.data.indices.Add(0);
                        answer = engine.EvaluateTemplate(Answer.Entrance, data);
                        activity = ActivityFactory.CreateActivity(answer);
                        await turnContext.SendActivityAsync(activity, cancellationToken);

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
                            await turnContext.SendActivityAsync(activity, cancellationToken);
                        }
                        break;
                    }
                // Others
                case QnAEvent.SetDebug:
                    {
                        var value = Convert.ToBoolean(turnContext.Activity.Value);
                        await _model.Debug.Set(turnContext, value, cancellationToken);
                        await SendDebug(turnContext, $"SetDebug {value}", cancellationToken);
                        break;
                    }
            }
        }

        private async Task<Operation> WaitForOperation(Operation operation, string name, CancellationToken cancellationToken)
        {
            while (operation.OperationState == OperationStateType.NotStarted || operation.OperationState == OperationStateType.Running)
            {
                await Task.Delay(500);
                operation = await _operations.GetDetailsAsync(operation.OperationId, cancellationToken);
            }
            if (operation.OperationState == OperationStateType.Failed)
            {
                throw new Exception($"{name} failed!");
            }
            return operation;
        }

        private async Task HandleAddQnA(ITurnContext<IEventActivity> turnContext, QnAMakerEndpointEx value, CancellationToken cancellationToken)
        {
            var qnA = new Dictionary<string, QnAMakerEndpointEx>(_model.QnAs.Get());
            if (qnA.ContainsKey(value.KnowledgeBaseId))
            {
                return;
            }
            qnA.Add(value.KnowledgeBaseId, value);
            await _model.QnAs.Set(turnContext, qnA, cancellationToken);
            await SendDebug(turnContext, $"{QnAEvent.AddQnA} {value.KnowledgeBaseId}", cancellationToken);
        }

        // TODO do not optimize now!!
        private async Task HandleGetQnA(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            var value = _model.QnAs.Get();
            var result = Activity.CreateEventActivity();
            result.Name = QnAEvent.GetQnA;
            result.Value = value;
            await turnContext.SendActivityAsync(result, cancellationToken);
            await SendDebug(turnContext, $"{QnAEvent.GetQnA} {value.Count}", cancellationToken);
        }

        private async Task HandleDelSource(ITurnContext<IEventActivity> turnContext, SourceEvent value, CancellationToken cancellationToken)
        {
            var operation = await _knowledgebase.UpdateAsync(value.KnowledgeBaseId, new UpdateKbOperationDTO(delete: new UpdateKbOperationDTODelete
            {
                Sources = new string[] { value.Id }
            }), cancellationToken);
            operation = await WaitForOperation(operation, QnAEvent.DelSource, cancellationToken);

            var qnA = new Dictionary<string, QnAMakerEndpointEx>(_model.QnAs.Get());
            qnA[value.KnowledgeBaseId].Sources.Remove(value.Id);
            await _model.QnAs.Set(turnContext, qnA, cancellationToken);

            await SendDebug(turnContext, $"{QnAEvent.DelSource} {value.Id}", cancellationToken);
        }

        private async Task SendDebug(ITurnContext context, string debug, CancellationToken cancellationToken)
        {
            if (_model.Debug.Get())
            {
                await context.SendActivityAsync(debug, cancellationToken: cancellationToken);
            }
        }
    }
}
