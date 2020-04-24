using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telecord.Tests
{
    class Expectation
    {
        private readonly Action<string> _expect;

        public Expectation(Action<string> expect)
        {
            _expect = expect;
        }

        public void ToBe(string expected)
        {
            _expect(expected);
        }
    }
}
