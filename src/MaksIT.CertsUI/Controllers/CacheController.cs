using MaksIT.CertsUI.Authorization.Extensions;
using MaksIT.CertsUI.Authorization.Filters;
using MaksIT.CertsUI.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaksIT.CertsUI.Controllers;

[ApiController]
[Route("api/cache")]
public class CacheController(
  ICacheService cacheService
) : ControllerBase {
  private readonly ICacheService _cacheService = cacheService;

  #region Read
  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpGet("download")]
  [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
  public async Task<IActionResult> GetCache() {
    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    var certsUIAuthorizationData = certsUIAuthorizationDataResult.Value;

    var result = await _cacheService.DownloadCacheZipAsync(certsUIAuthorizationData);
    if (!result.IsSuccess || result.Value == null) {
      return result.ToActionResult();
    }

    var bytes = result.Value;

    return File(bytes, "application/zip", "cache.zip");
  }

  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpGet("{accountId:guid}/download")]
  [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
  public async Task<IActionResult> GetAccountCache(Guid accountId) {
    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    var certsUIAuthorizationData = certsUIAuthorizationDataResult.Value;

    var result = await _cacheService.DownloadAccountCacheZipAsync(certsUIAuthorizationData, accountId);
    if (!result.IsSuccess || result.Value == null) {
      return result.ToActionResult();
    }

    var bytes = result.Value;

    return File(bytes, "application/zip", $"cache-{accountId}.zip");
  }
  #endregion

  #region Create
  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpPost("upload")]
  //[RequestSizeLimit(200_000_000)]
  public async Task<IActionResult> PostCache(IFormFile file) {
    if (file is null || file.Length == 0) return BadRequest("No file.");

    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    var certsUIAuthorizationData = certsUIAuthorizationDataResult.Value;

    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);
    var result = await _cacheService.UploadCacheZipAsync(certsUIAuthorizationData, ms.ToArray());
    return result.ToActionResult();
  }

  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpPost("{accountId:guid}/upload")]
  public async Task<IActionResult> PostAccountCache(Guid accountId, [FromBody] byte[] zipBytes) {
    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    var certsUIAuthorizationData = certsUIAuthorizationDataResult.Value;

    var result = await _cacheService.UploadAccountCacheZipAsync(certsUIAuthorizationData, accountId, zipBytes);
    return result.ToActionResult();
  }
  #endregion

  #region Delete
  [ServiceFilter(typeof(CertsUIAuthorizationFilter))]
  [HttpDelete]
  [ProducesResponseType(StatusCodes.Status200OK)]
  public async Task<IActionResult> DeleteCache() {
    var certsUIAuthorizationDataResult = HttpContext.GetCertsUIAuthorizationData();
    if (!certsUIAuthorizationDataResult.IsSuccess || certsUIAuthorizationDataResult.Value == null)
      return certsUIAuthorizationDataResult.ToActionResult();

    var certsUIAuthorizationData = certsUIAuthorizationDataResult.Value;

    var result = await _cacheService.DeleteCacheAsync(certsUIAuthorizationData);
    return result.ToActionResult();
  }
  #endregion
}
