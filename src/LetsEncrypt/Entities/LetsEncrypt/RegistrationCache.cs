using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using MaksIT.LetsEncrypt.Entities.Jws;

namespace MaksIT.LetsEncrypt.Entities;
public class CertificateCache {
  public string? Cert { get; set; }
  public byte[]? Private { get; set; }
}

public class RegistrationCache {
  public Dictionary<string, CertificateCache>? CachedCerts { get; set; }
  public byte[]? AccountKey { get; set; }
  public string? Id { get; set; }
  public Jwk? Key { get; set; }
  public Uri? Location { get; set; }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="subject"></param>
  /// <param name="value"></param>
  /// <returns></returns>
  public bool TryGetCachedCertificate(string subject, out CachedCertificateResult? value) {
    value = null;

    if (CachedCerts == null)
      return false;

    if (!CachedCerts.TryGetValue(subject, out var cache)) {
      return false;
    }

    var cert = new X509Certificate2(Encoding.ASCII.GetBytes(cache.Cert));

    // if it is about to expire, we need to refresh
    if ((cert.NotAfter - DateTime.UtcNow).TotalDays < 30)
      return false;

    var rsa = new RSACryptoServiceProvider(4096);
    rsa.ImportCspBlob(cache.Private);

    value = new CachedCertificateResult {
      Certificate = cache.Cert,
      PrivateKey = rsa
    };
    return true;
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="hostsToRemove"></param>
  public void ResetCachedCertificate(IEnumerable<string> hostsToRemove) {
    if (CachedCerts != null)
      foreach (var host in hostsToRemove)
        CachedCerts.Remove(host);
  }
}
