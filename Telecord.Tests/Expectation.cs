using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telecord.Tests
{
    class Expectation<T>
    {
        private readonly Action<T> _expect;

        public Expectation(Action<T> expect)
        {
            _expect = expect;
        }

        public void ToBe(T expected)
        {
            _expect(expected);
        }
    }

    static class ExpectationExtensions
    {
        public static void ToBe<T>(this Expectation<T[]> expectation, params T[] expected)
        {
            expectation.ToBe(expected);
        }
    }
}
