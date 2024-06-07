
using System.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using MaksIT.LetsEncrypt.Entities.Jws;

namespace MaksIT.LetsEncrypt.Entities;
public class CertificateCache {
  public string? Cert { get; set; }
  public byte[]? Private { get; set; }
}

public class RegistrationCache {

  /// <summary>
  /// Field used to identify cache by account id
  /// </summary>
  public Guid AccountId { get; set; }
  public string[]? Contacts { get; set; }


  public Dictionary<string, CertificateCache>? CachedCerts { get; set; }
  public byte[]? AccountKey { get; set; }
  public string? Id { get; set; }
  public Jwk? Key { get; set; }
  public Uri? Location { get; set; }

  /// <summary>
  /// Returns a list of hosts with upcoming SSL expiry
  /// </summary>
  public string[] GetHostsWithUpcomingSslExpiry(int days = 30) {
    var hostsWithUpcomingSslExpiry = new List<string>();

    if (CachedCerts == null)
      return hostsWithUpcomingSslExpiry.ToArray();

    foreach (var result in CachedCerts) {
      var (subject, cachedChert) = result;

      if (cachedChert.Cert != null) {
        var cert = new X509Certificate2(Encoding.ASCII.GetBytes(cachedChert.Cert));

        // if it is about to expire, we need to refresh
        if ((cert.NotAfter - DateTime.UtcNow).TotalDays < days)
          hostsWithUpcomingSslExpiry.Add(subject);
      }
    }

    return hostsWithUpcomingSslExpiry.ToArray();
  }

  /// <summary>
  /// Returns cached certificate. Certs older than 30 days are not returned
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
