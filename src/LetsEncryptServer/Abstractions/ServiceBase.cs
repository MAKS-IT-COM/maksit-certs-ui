using MaksIT.Results;

namespace LetsEncryptServer.Abstractions;

public abstract class ServiceBase {


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
