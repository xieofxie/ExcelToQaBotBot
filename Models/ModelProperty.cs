using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.LanguageGeneration;
using QnABot.Utils;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QnABot.Models
{
    // TODO hybird between in-memory singleton and in-db global state
    public abstract class ModelProperty<TGet, TSet>
    {
        private TGet tget;
        private readonly IStatePropertyAccessor<TSet> accessor;

        public ModelProperty(
            IStorage storage,
            BotStateSet botStateSet,
            TSet defaultValue,
            string name)
        {
            var state = new NamedState(storage, name);
            accessor = state.CreateProperty<TSet>(name);
            botStateSet.Add(state);

            var current = state.Load().Result;
            if (current == null || !current.ContainsKey(name))
            {
                var get = Convert(defaultValue);
                lock (this)
                {
                    tget = get;
                }
            }
            else
            {
                var get = Convert((TSet)current[name]);
                lock (this)
                {
                    tget = get;
                }
            }
        }

        // public static implicit operator TGet(ModelProperty<TGet, TSet> modelProperty) => modelProperty.Get();

        public TGet Get()
        {
            lock (this)
            {
                return tget;
            }
        }

        public async Task Set(ITurnContext turnContext, TSet value, CancellationToken cancellationToken)
        {
            // TODO do not synchronize
            await accessor.SetAsync(turnContext, value, cancellationToken);
            var newGet = Convert(value);
            lock (this)
            {
                tget = newGet;
            }
        }

        protected abstract TGet Convert(TSet value);
    }

    // for none reference
    public class ModelPropertySame<T> : ModelProperty<T, T>
    {
        public ModelPropertySame(
            IStorage storage,
            BotStateSet botStateSet,
            T defaultValue,
            string name)
            : base(storage, botStateSet, defaultValue, name)
        {
        }

        protected override T Convert(T value)
        {
            return value;
        }
    }

    // must not modify
    public class ModelPropertyAllEndpoint : ModelProperty<IReadOnlyDictionary<string, QnAMakerEndpointEx>, Dictionary<string, QnAMakerEndpointEx>>
    {
        public ModelPropertyAllEndpoint(
            IStorage storage,
            BotStateSet botStateSet,
            Dictionary<string, QnAMakerEndpointEx> defaultValue,
            string name)
            : base(storage, botStateSet, defaultValue, name)
        {
        }

        protected override IReadOnlyDictionary<string, QnAMakerEndpointEx> Convert(Dictionary<string, QnAMakerEndpointEx> value)
        {
            return value;
        }
    }

    public class ModelPropertyTemplateEngine : ModelProperty<TemplateEngine, string>
    {
        public static readonly ImportResolverDelegate importResolverDelegate = (s, r) => { return (string.Empty, string.Empty); };

        public ModelPropertyTemplateEngine(
            IStorage storage,
            BotStateSet botStateSet,
            string defaultValue,
            string name)
            : base(storage, botStateSet, defaultValue, name)
        {
        }

        protected override TemplateEngine Convert(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            var engine = new TemplateEngine();
            engine.AddText(value, importResolver: importResolverDelegate);
            return engine;
        }
    }
}
