using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using bridge.api.bclogia.cz;
using IdentityModel.Client;

namespace test.bridge.api.bclogia.cz
{
    internal class Program
    {

        static void Main(string[] args)
        {
            string ClientId = "ClientId";
            string ClientSecret = "ClientSecret";
            string Company = "Company";
            int SpolID = 999;

            var client = new HttpClient();
            var disco = client.GetDiscoveryDocumentAsync("https://identity.bclogia.cz").Result;
            if (disco.IsError)
            {
                Console.WriteLine(disco.Error);
                return;
            }

            var request = new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,

                ClientId = ClientId,

                ClientSecret = ClientSecret,

                Scope = "bridge",

                Parameters = {
                    new KeyValuePair<string, string>("company", Company)
                }
            };

            var tokenResponse = client.RequestClientCredentialsTokenAsync(request).Result;
            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                Console.WriteLine(tokenResponse.ErrorDescription);
                return;
            }

            Console.WriteLine(tokenResponse.Json);
            Console.WriteLine("\n\n");

            var apiClient = new HttpClient();
            apiClient.SetBearerToken(tokenResponse.AccessToken);

            var c = new Bridge("https://bridge.api.bclogia.cz", apiClient);

            int count = 0;
            int start = 0;
            int limit = 20;

            BCPredpis2ListModelBCPaging p_l;
            int i = 0;

            do
            {
                p_l = c.SearchAsync(SpolID, null, null, null, null, null, null, start, limit).Result;

                if (p_l != null)
                {
                    start += 20;
                    count += p_l.Data.Count;

                    foreach (var p in p_l.Data)
                    {
                        var p_d = c.PredpisAsync(SpolID, p.PredID).Result;
                        if (p_d != null && p_d.Predpis != null)
                        {
                            string f1 = Serialize(p_d.Firma);

                            var f = c.FirmyAsync(p_d.Predpis.PredFirID).Result;

                            string f2 = Serialize(f);

                            if (string.CompareOrdinal(f1, f2) != 0)
                            {
                                throw new InvalidDataException("ERROR");
                            }

                            Console.WriteLine("PredID: {0} ({1}/{2})", p.PredID, ++i, p_l.TotalRecords);
                        }
                    }
                }
            }
            while (p_l != null && count < p_l.TotalRecords);

            Console.WriteLine("Hotovo...");
            Console.ReadLine(); 
        }

        static string Serialize(object o)
        {
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(o.GetType());

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Encoding = new UnicodeEncoding(false, false),
                Indent = true,
                OmitXmlDeclaration = true
            };

            using (StringWriter textWriter = new StringWriter())
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(textWriter, settings))
                {
                    x.Serialize(xmlWriter, o);
                }
                return textWriter.ToString();
            }
        }
    }
}
