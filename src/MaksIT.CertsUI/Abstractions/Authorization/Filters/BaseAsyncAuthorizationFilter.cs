using Microsoft.AspNetCore.Mvc.Filters;


namespace MaksIT.CertsUI.Abstractions.Authorization.Filters;

public abstract class BaseAsyncAuthorizationFilter(
  ILogger logger
) : IAsyncAuthorizationFilter {

  protected readonly ILogger _logger = logger;

  // Derived classes must implement this method.
  public abstract Task OnAuthorizationAsync(AuthorizationFilterContext context);
}