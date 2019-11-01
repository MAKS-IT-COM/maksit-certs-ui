using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Linq;

using Microsoft.Extensions.Options;

using LetsEncrypt.Services;
using LetsEncrypt.Helpers;
using LetsEncrypt.Entities;

namespace LetsEncrypt
{
    public class App {
 
        private readonly string AppPath = AppDomain.CurrentDomain.BaseDirectory;
        
        private readonly AppSettings _appSettings;
        private readonly ILetsEncryptService _letsEncryptService;
        private readonly IKeyService _keyService;

        public App(IOptions<AppSettings> appSettings, ILetsEncryptService letsEncryptService, IKeyService keyService) {
            _appSettings = appSettings.Value;
            _letsEncryptService = letsEncryptService;
            _keyService = keyService;
        }

        public void Run() {

            try
            {
                LetsEncrypt.Helpers.Environment env = _appSettings.environments.First(x => (x.name == _appSettings.active));

                Console.WriteLine(string.Format("Let's Encrypt C# .Net Core Client, environment: {0}", env.name));
        
                //loop all customers
                foreach(Customer customer in _appSettings.customers) {
                    try {
                        Console.WriteLine(string.Format("Managing customer: {0} - {1} {2}", customer.id, customer.name, customer.lastname));

                        //loop each customer website
                        foreach(Site site in customer.sites) {
                            Console.WriteLine(string.Format("Managing site: {0}", site.name));

                            try {
                                //define cache folder
                                string cache = Path.Combine(AppPath, "cache", customer.id);
                                if(!Directory.Exists(cache)) {
                                    Directory.CreateDirectory(cache);
                                }

                                //1. Client initialization
                                Console.WriteLine("1. Client Initialization...");
                                _letsEncryptService.Init(env.url, cache, site.name, customer.contacts).Wait();


                                Console.WriteLine(string.Format("Terms of service: {0}", _letsEncryptService.GetTermsOfServiceUri()));

                                //create folder for ssl
                                string ssl = Path.Combine(env.ssl, site.name);
                                if(!Directory.Exists(ssl)) {
                                    Directory.CreateDirectory(ssl);
                                }

                                // get cached certificate and check if it's valid
                                // if valid check if cert and key exists otherwise recreate
                                // else continue with new certificate request
                                CachedCertificateResult certRes = new CachedCertificateResult();
                                if (_letsEncryptService.TryGetCachedCertificate(site.name, out certRes)) {
                                    string cert = Path.Combine(ssl, site.name + ".crt");
                                    if(!File.Exists(cert))
                                        File.WriteAllText(cert, certRes.Certificate);
                                    
                                    string key = Path.Combine(ssl, site.name + ".key");
                                    if(!File.Exists(key)) {
                                        using (StreamWriter writer = File.CreateText(key))
                                            _keyService.ExportPrivateKey(certRes.PrivateKey, writer);
                                    }

                                    Console.WriteLine("Certificate and Key exists and valid.");
                                }
                                else {
                                    //new nonce
                                    _letsEncryptService.NewNonce().Wait();

                                    //try to make new order
                                    try {
                                        //create new orders
                                        Console.WriteLine("2. Client New Order...");
                                        Task<Dictionary<string, string>> orders = _letsEncryptService.NewOrder(site.hosts, site.challenge);
                                        orders.Wait();

                                        switch(site.challenge) {
                                            case "http-01": {
                                                //ensure to enable static file discovery on server in .well-known/acme-challenge
                                                //and listen on 80 port

                                                //check acme directory
                                                string acme = Path.Combine(env.www, env.acme);
                                                if(!Directory.Exists(acme)) {
                                                    throw new DirectoryNotFoundException(string.Format("Directory {0} wasn't created", acme));
                                                }

                                                foreach (FileInfo file in new DirectoryInfo(acme).GetFiles())
                                                    file.Delete();

                                                foreach (var result in orders.Result)
                                                {
                                                    Console.WriteLine("Key: " + result.Key + System.Environment.NewLine + "Value: " + result.Value);
                                                    string[] splitToken = result.Value.Split('~');

                                                    string token = Path.Combine(acme, splitToken[0]);
                                                    File.WriteAllText(token, splitToken[1]);
                                                }

                                                break;
                                            }

                                            case "dns-01": {
                                                //Manage DNS server MX record, depends from provider

                                                break;
                                            }

                                            default: {

                                                break;
                                            }
                                        }
                        
                                        //complete challanges
                                        Console.WriteLine("3. Client Complete Challange...");
                                        _letsEncryptService.CompleteChallenges().Wait();
                                        Console.WriteLine("Challanges comleted.");
                                    }
                                    catch (Exception ex) {
                                        Console.WriteLine(ex.Message.ToString());
                                        _letsEncryptService.GetOrder(site.hosts).Wait();
                                    }


                                    // Download new certificate
                                    Console.WriteLine("4. Download certificate...");
                                    _letsEncryptService.GetCertificate(site.name).Wait();

                                    // Write to filesystem
                                    certRes = new CachedCertificateResult();
                                    if (_letsEncryptService.TryGetCachedCertificate(site.name, out certRes)) {
                                        string cert = Path.Combine(ssl, site.name + ".crt");
                                        File.WriteAllText(cert, certRes.Certificate);
                                        
                                        string key = Path.Combine(ssl, site.name + ".key");
                                        using (StreamWriter writer = File.CreateText(key))
                                            _keyService.ExportPrivateKey(certRes.PrivateKey, writer);

                                        Console.WriteLine("Certificate saved.");
                                    }
                                    else {
                                        Console.WriteLine("Unable to get new cached certificate.");
                                    }

                                    
                                }

                                
                            }
                            catch (Exception ex) {
                                Console.WriteLine(ex.Message.ToString());
                            }
                        }
                    }
                    catch (Exception ex) {
                        Console.WriteLine(ex.Message.ToString());
                    }
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message.ToString());
            }
        }
    }
}
