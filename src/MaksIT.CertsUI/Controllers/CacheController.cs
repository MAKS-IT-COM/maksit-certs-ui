using MaksIT.CertsUI.Authorization.Filters;
using MaksIT.CertsUI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LetsEncryptServer.Controllers;

[ApiController]
[Route("api")]
[ServiceFilter(typeof(JwtOrApiKeyAuthorizationFilter))]
public class CacheController(ICacheService cacheService) : ControllerBase {
  private readonly ICacheService _cacheService = cacheService;

  [HttpGet("cache/download")]
  public async Task<IActionResult> GetCache() {
    var result = await _cacheService.DownloadCacheZipAsync();
    if (!result.IsSuccess || result.Value == null) {
      return result.ToActionResult();
    }

    var bytes = result.Value;

    return File(bytes, "application/zip", "cache.zip");
  }

  [HttpPost("cache/upload")]
  //[RequestSizeLimit(200_000_000)]
  public async Task<IActionResult> PostCache(IFormFile file) {
    if (file is null || file.Length == 0) return BadRequest("No file.");

    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);
    var result = await _cacheService.UploadCacheZipAsync(ms.ToArray());
    return result.ToActionResult();
  }

  [HttpDelete("cache")]
  public async Task<IActionResult> DeleteCache() {
    var result = await _cacheService.DeleteCacheAsync();
    return result.ToActionResult();
  }

  [HttpGet("cache/{accountId:guid}/download")]
  public async Task<IActionResult> GetCache(Guid accountId) {
    var result = await _cacheService.DownloadAccountCacheZipAsync(accountId);
    if (!result.IsSuccess || result.Value == null) {
      return result.ToActionResult();
    }
    
    var bytes = result.Value;

    return File(bytes, "application/zip", $"cache-{accountId}.zip");
  }

  [HttpPost("cache/{accountId:guid}/upload")]
  public async Task<IActionResult> PostAccountCache(Guid accountId, [FromBody] byte[] zipBytes) {
    var result = await _cacheService.UploadAccountCacheZipAsync(accountId, zipBytes);
    return result.ToActionResult();
  }


}
