using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ACMEv2;

namespace LetsEncrypt
{
    class Program
    {
        private static readonly string AppPath = AppDomain.CurrentDomain.BaseDirectory;



        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Let's Encrypt C# .Net Core Client");

                Settings settings  = (new SettingsProvider(null)).settings;
        
                //loop all customers
                foreach(Customer customer in settings.customers) {
                    try {
                        Console.WriteLine(string.Format("Managing customer: {0} - {1} {2}", customer.id, customer.name, customer.lastname));

                        //loop each customer website
                        foreach(Site site in customer.sites) {
                            Console.WriteLine(string.Format("Managing site: {0}", site.name));

                            try {
                                //define cache folder
                                string cache = Path.Combine(AppPath, "cache", customer.id, site.name);
                                if(!Directory.Exists(cache)) {
                                    Directory.CreateDirectory(cache);
                                }

                                LetsEncryptClient client = new LetsEncryptClient(settings.url, cache);

                                //1. Client initialization
                                Console.WriteLine("1. Client Initialization...");
                                client.Init(customer.contacts).Wait();
                                Console.WriteLine(string.Format("Terms of service: {0}", client.GetTermsOfServiceUri()));

                                //create folder for ssl
                                string ssl = Path.Combine(settings.ssl, site.name);
                                if(!Directory.Exists(ssl)) {
                                    Directory.CreateDirectory(ssl);
                                }

                                // get cached certificate and check if it's valid
                                // if valid check if cert and key exists otherwise recreate
                                // else continue with new certificate request
                                CachedCertificateResult certRes = new CachedCertificateResult();
                                if (client.TryGetCachedCertificate(site.hosts, out certRes))
                                {
                                    string cert = Path.Combine(ssl, site.name + ".crt");
                                    if(!File.Exists(cert))
                                        File.WriteAllText(cert, certRes.Certificate);
                                    
                                    string key = Path.Combine(ssl, site.name + ".key");
                                    if(!File.Exists(key)) {
                                        using (StreamWriter writer = File.CreateText(key))
                                            Library.ExportPrivateKey(certRes.PrivateKey, writer);
                                    }

                                    Console.WriteLine("Certificate and Key exists and valid.");
                                }
                                else {
                                    if(!Directory.Exists(Path.Combine(settings.www, site.name))) {
                                        throw new DirectoryNotFoundException(string.Format("Site {0} wasn't initialized", site.name));
                                    }

                                    //new nonce
                                    client.NewNonce().Wait();

                                    //try to make new order
                                    try
                                    {
                                        //create new orders
                                        Console.WriteLine("2. Client New Order...");
                                        Task<Dictionary<string, string>> orders = client.NewOrder(site.hosts, site.challenge);
                                        orders.Wait();

                                        switch(site.challenge) {
                                            case "http-01": {
                                                //ensure to enable static file discovery on server in .well-known/acme-challenge
                                                //and listen on 80 port

                                                //create acme directory for web site
                                                string acme = Path.Combine(settings.www, site.name, settings.acme);
                                                if(!Directory.Exists(acme)) {
                                                    Directory.CreateDirectory(acme);
                                                }

                                                foreach (FileInfo file in new DirectoryInfo(acme).GetFiles())
                                                    file.Delete();

                                                foreach (var result in orders.Result)
                                                {
                                                    Console.WriteLine("Key: " + result.Key + Environment.NewLine + "Value: " + result.Value);
                                                    string[] splitToken = result.Value.Split('~');

                                                    string token = Path.Combine(acme, splitToken[0]);
                                                    File.WriteAllText(token, splitToken[1]);

                                                    //for Selinux on centos7
                                                    Console.WriteLine(Library.RestoreCon(token));
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
                                        client.CompleteChallenges().Wait();
                                        Console.WriteLine("Challanges comleted.");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message.ToString());
                                        client.GetOrder(site.hosts).Wait();
                                    }


                                    // Download new certificate
                                    Console.WriteLine("4. Download certificate...");
                                    client.GetCertificate().Wait();

                                    // Write to filesystem
                                    certRes = new CachedCertificateResult();
                                    if (client.TryGetCachedCertificate(site.hosts, out certRes)) {
                                        string cert = Path.Combine(ssl, site.name + ".crt");
                                        File.WriteAllText(cert, certRes.Certificate);
                                        
                                        string key = Path.Combine(ssl, site.name + ".key");
                                        using (StreamWriter writer = File.CreateText(key))
                                            Library.ExportPrivateKey(certRes.PrivateKey, writer);

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
