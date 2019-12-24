using Microsoft.Bot.Builder.AI.QnA;
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
        private string noResultResponse = "No QnA Maker answers are found.";
        private bool debug = false;

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

        public string NoResultResponse
        {
            get
            {
                lock (this)
                {
                    return noResultResponse;
                }
            }
            set
            {
                lock (this)
                {
                    noResultResponse = value;
                }
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
