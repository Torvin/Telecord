using System;
using System.Text.RegularExpressions;

namespace DiscordParser
{
    public class Rule<TState>
        where TState : struct, IParserState
    {
        public int Order { get; set; }
        public Func<Match, TState, double> Quality { get; set; }
        public Func<string, Parser<TState>, TState, Match> Match { get; set; }
        public Func<Parser<TState>, TState, Node> Parse { get; set; }
    }
}
