using Microsoft.AspNetCore.Mvc.Filters;


namespace MaksIT.CertsUI.Abstractions.Authorization.Filters;

public abstract class BaseAsyncAuthorizationFilter : IAsyncAuthorizationFilter {

  public abstract Task OnAuthorizationAsync(AuthorizationFilterContext context);
}