using System;
using System.Collections.Generic;
using System.IO;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using System.Threading.Tasks;



using ACMEv2;


using FS = System.IO;



namespace LetsEncrypt
{
    class Program
    {
        static void Main(string[] args)
        {
            // save to http://<YOUR_DOMAIN>/.well-known/acme-challenge/<TOKEN>
            var tokensPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".well-known/acme-challenge");
            if (!FS.Directory.Exists(tokensPath))
                FS.Directory.CreateDirectory(tokensPath);

            foreach (FileInfo file in new DirectoryInfo(tokensPath).GetFiles())
                file.Delete();


            var certsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "certs");
            if (!FS.Directory.Exists(certsPath))
                FS.Directory.CreateDirectory(certsPath);


            List<string> contacts = new List<string>();
            contacts.Add("maksym.sadovnychyy@gmail.com");

            List<string> hosts = new List<string>();
            hosts.Add("maks-it.com");
            hosts.Add("www.maks-it.com");

            Console.WriteLine("Let's Encrypt C# .Net Core Client");

            try
            {
                LetsEncryptClient client = new LetsEncryptClient(LetsEncryptClient.StagingV2, AppDomain.CurrentDomain.BaseDirectory);
                Console.WriteLine("1. Client Initialization...");

                // 1
                client.Init(contacts.ToArray()).Wait();
                Console.WriteLine(string.Format("Terms of service: {0}",client.GetTermsOfServiceUri()));
                client.NewNonce().Wait();


                // 2
                try
                {
                    Console.WriteLine("2. Client New Order...");
                    Task<Dictionary<string, string>> orders = client.NewOrder(hosts.ToArray(), "http-01");
                    orders.Wait();

                    foreach (var result in orders.Result)
                    {
                        Console.WriteLine("Key: " + result.Key + Environment.NewLine + "Value: " + result.Value);
                        string[] splitToken = result.Value.Split('~');
                        File.WriteAllText(FS.Path.Combine(tokensPath, splitToken[0]), splitToken[1]);
                    }

                    // 3
                    Console.WriteLine("3. Client Complete Challange...");
                    client.CompleteChallenges().Wait();
                    Console.WriteLine("Challanges comleted.");
                }
                catch (Exception ex) {
                    Console.WriteLine(ex.Message.ToString());
                    client.GetOrder(hosts.ToArray()).Wait();
                }
               

                // 4 Download certificate
                Console.WriteLine("4. Download certificate...");
                Task<(X509Certificate2 Cert, RSA PrivateKey)> certificate = client.GetCertificate();
                certificate.Wait();

                File.WriteAllText(Path.Combine(certsPath, "maks-it.com.crt"), Library.ExportToPEM(certificate.Result.Cert));
                Console.WriteLine("Certificate saved.");
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message.ToString());
            }




            Console.Read();
        }



    }


}
