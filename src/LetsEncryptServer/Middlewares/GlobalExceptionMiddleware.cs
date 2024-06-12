using System.Net;

namespace MaksIT.LetsEncryptServer.Middlewares {
  public class GlobalExceptionMiddleware {

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger) {
      _next = next;
      _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context) {
      try {
        await _next(context);
      }
      catch (Exception ex) {
        _logger.LogError(ex, "An unhandled exception occurred.");
        await HandleExceptionAsync(context);
      }
    }

    private static Task HandleExceptionAsync(HttpContext context) {
      context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
      context.Response.ContentType = "application/json";

      var response = new { message = "An error occurred while processing your request." };
      return context.Response.WriteAsJsonAsync(response);
    }
  }
}
