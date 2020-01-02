using Microsoft.Bot.Builder;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QnABot.Utils
{
    public class NamedState : BotState
    {
        private string _name;
        private IStorage _storage;

        public NamedState(IStorage storage, string name)
            : base(storage, nameof(NamedState) + name)
        {
            _name = name;
            _storage = storage;
        }

        protected override string GetStorageKey(ITurnContext turnContext)
        {
            return _name;
        }

        // TODO bare read since key does not depend on turnContext
        public async Task<IDictionary<string, object>> Load()
        {
            var items = await _storage.ReadAsync(new[] { _name }).ConfigureAwait(false);
            items.TryGetValue(_name, out object val);
            if (val is IDictionary<string, object> asDictionary)
            {
                return asDictionary;
            }
            else if (val is JObject asJobject)
            {
                return asJobject.ToObject<IDictionary<string, object>>();
            }
            else if (val is null)
            {
                return null;
            }
            else
            {
                throw new InvalidOperationException("Data is not in the correct format for BotState.");
            }
        }

        public async Task Save(IDictionary<string, object> state)
        {
            var changes = new Dictionary<string, object>
            {
                { _name, state },
            };
            await _storage.WriteAsync(changes).ConfigureAwait(false);
        }
    }
}
