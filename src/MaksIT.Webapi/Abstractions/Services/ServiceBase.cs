using MaksIT.Webapi;
using MaksIT.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MaksIT.Webapi.Abstractions.Services;

public abstract class ServiceBase(ILogger logger, IOptions<Configuration> appSettings) {

  protected readonly ILogger _logger = logger;
  protected readonly Configuration _appSettings = appSettings.Value;

  protected Result UnsupportedPatchOperationResponse() {
    return Result.BadRequest("Unsupported operation");
  }

  protected Result<T?> UnsupportedPatchOperationResponse<T>() {
    return Result<T?>.BadRequest(default, "Unsupported operation");
  }

  protected Result PatchFieldIsNotDefined(string fieldName) {
    return Result.BadRequest($"It's not possible to set non defined field {fieldName}.");
  }

  protected Result<T?> PatchFieldIsNotDefined<T>(string fieldName) {
    return Result<T?>.BadRequest(default, $"It's not possible to set non defined field {fieldName}.");
  }
}
