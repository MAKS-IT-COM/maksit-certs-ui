using System;
using System.Collections.Generic;
using MaksIT.LetsEncrypt.Entities.Jws;

namespace MaksIT.LetsEncrypt.Entities {
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
  }
}
