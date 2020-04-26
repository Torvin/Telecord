using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DiscordParser
{
    public class Parser<TState>
        where TState : struct, IParserState
    {
        private readonly Rule<TState>[] _rules;

        public Parser(IEnumerable<Rule<TState>> rules)
        {
            _rules = rules
                .OrderBy(r => r.Order)
                .ThenBy(r => r.Quality == null ? 1 : 0)
                .ToArray();
        }

        public virtual Node Parse(string text)
        {
            return Parse(text, default);
        }

        public virtual Node Parse(string text, TState state)
        {
            if (text == "")
                return new ContainerNode(Enumerable.Empty<Node>());

            var list = new List<Node>();

            for (; ; )
            {
                Rule<TState> bestRule = null;
                double? bestQuality = null;
                Match bestMatch = null;

                foreach (var rule in _rules)
                {
                    if (rule.Order > bestRule?.Order)
                        break;

                    var match = rule.Match(text, this, state);
                    if (match?.Success != true)
                        continue;

                    var quality = rule.Quality == null ? 0 : rule.Quality(match, state);
                    if (bestQuality == null || quality > bestQuality)
                    {
                        bestRule = rule;
                        bestMatch = match;
                        bestQuality = quality;
                    }
                }

                if (bestRule == null)
                    throw new ParserException($"No matching rule was found while parsing `{text}`.");

                state.LastMatch = bestMatch;
                list.Add(bestRule.Parse(this, state));

                text = text.Substring(state.LastMatch.Length);
                if (text.Length == 0)
                    break;
            }

            if (list.Count == 1)
                return list.Single();

            return new ContainerNode(list);
        }
    }

    public interface IParserState
    {
        Match LastMatch { get; set; }
    }

    [System.Serializable]
    public class ParserException : System.Exception
    {
        public ParserException() { }
        public ParserException(string message) : base(message) { }
        public ParserException(string message, System.Exception inner) : base(message, inner) { }
        protected ParserException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
