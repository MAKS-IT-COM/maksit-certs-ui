using MaksIT.LetsEncryptServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace LetsEncryptServer.Controllers;

[ApiController]
[Route("api")]
public class CacheController(ICacheService cacheService) : ControllerBase {
  private readonly ICacheService _cacheService = cacheService;

  [HttpGet("caches/download")]
  public async Task<IActionResult> GetCaches() {
    var result = await _cacheService.DownloadCacheZipAsync();
    if (!result.IsSuccess || result.Value == null) {
      return result.ToActionResult();
    }

    var bytes = result.Value;

    return File(bytes, "application/zip", "caches.zip");
  }

  [HttpPost("caches/upload")]
  public async Task<IActionResult> PostCaches([FromBody] byte[] zipBytes) {
    var result = await _cacheService.UploadCacheZipAsync(zipBytes);
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
