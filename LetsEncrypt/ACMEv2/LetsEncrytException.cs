using System;
using System.Net.Http;

namespace ACMEv2
{
    public class LetsEncrytException : Exception
    {
        public LetsEncrytException(Problem problem, HttpResponseMessage response)
            : base($"{problem.Type}: {problem.Detail}")
        {
            Problem = problem;
            Response = response;
        }

        public Problem Problem { get; }

        public HttpResponseMessage Response { get; }
    }
}
