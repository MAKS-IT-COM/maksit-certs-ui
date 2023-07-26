using System;
using System.Net.Http;

namespace MaksIT.LetsEncrypt.Exceptions {
  public class LetsEncrytException : Exception {
    public LetsEncrytException(Problem problem, HttpResponseMessage response)
        : base($"{problem.Type}: {problem.Detail}") {
      Problem = problem;
      Response = response;
    }

    public Problem Problem { get; }

    public HttpResponseMessage Response { get; }
  }


  public class Problem {
    public string Type { get; set; }

    public string Detail { get; set; }

    public string RawJson { get; set; }
  }
}
