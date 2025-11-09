using MaksIT.LetsEncrypt.Models.Responses;


namespace MaksIT.LetsEncrypt.Exceptions;

public class LetsEncrytException : Exception {

  public Problem? Problem { get; }

  public HttpResponseMessage Response { get; }

  public LetsEncrytException(
    Problem? problem,
    HttpResponseMessage response
  ) : base(problem != null ? $"{problem.Type}: {problem.Detail}" : "") {

    Problem = problem;
    Response = response;
  }
}
