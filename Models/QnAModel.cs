using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.LanguageGeneration;
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
        private List<QnAMakerEndpoint> qnAs = new List<QnAMakerEndpoint>();
        private int resultNumber = 3;
        private float minScore = 0.5f;
        private TemplateEngine answerLg;
        private bool debug = false;

        public static readonly ImportResolverDelegate importResolverDelegate = (s, r) => { return (string.Empty, string.Empty); };

        public QnAModel()
        {

        }

        public int ResultNumber
        {
            get
            { 
                lock(this)
                {
                    return resultNumber;
                }
            }
            set {
                lock(this)
                {
                    resultNumber = value;
                }
            }
        }

        public List<QnAMakerEndpoint> QnAs
        {
            get
            {
                lock(this)
                {
                    return qnAs;
                }
            }
            set
            {
                lock(this)
                {
                    if (qnAs == value)
                    {
                        qnAs = new List<QnAMakerEndpoint>(value);
                    }
                    else
                    {
                        qnAs = value;
                    }
                }
            }
        }

        public float MinScore
        {
            get
            {
                lock(this)
                {
                    return minScore;
                }
            }
            set
            {
                lock(this)
                {
                    minScore = value;
                }
            }
        }

        public TemplateEngine AnswerLg
        {
            get
            {
                lock (this)
                {
                    return answerLg;
                }
            }
        }

        public void SetAnswerLg(string template)
        {
            var engine = new TemplateEngine();
            engine.AddText(template, importResolver: importResolverDelegate);
            lock (this)
            {
                answerLg = engine;
            }
        }

        public bool Debug
        {
            get
            {
                lock (this)
                {
                    return debug;
                }
            }
            set
            {
                lock (this)
                {
                    debug = value;
                }
            }
        }
    }
}
