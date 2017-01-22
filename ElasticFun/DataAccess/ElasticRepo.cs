using CsvHelper;
using ElasticFun.Models;
using Nest;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticFun.DataAccess
{
    public class ElasticRepo
    {
        private readonly ElasticClient client;

        public ElasticRepo()
        {
            var uri = "http://localhost:9200";
            client = new ElasticClient(new Uri(uri));
        }

        public async Task AddDataAsync(string indexName)
        {
            var path = @"C:\Users\RiperZR\Source\Repos\ElasticFun\ElasticFun\Data\companylist.csv";
            foreach (var company in LoadCompany(path))
            {
                await client.IndexAsync(company,id=>id.Index(indexName));
            }
        }

        public async Task<IEnumerable<Index>> GetAllIndex()
        {
            var mapping = await client.LowLevel.IndicesGetMappingForAllAsync<JObject>();

            return Enumerable.Empty<Index>();
        }


        public IEnumerable<Company> LoadCompany(string location)
        {
            var csv = new CsvReader(new StreamReader(File.OpenRead(location)));
            while (csv.Read())
            {
                yield return new Company
                {
                    Symbol = csv.GetField<string>("Symbol"),
                    Name = csv.GetField<string>("Name"),
                    Industry = csv.GetField<string>("Name"),
                    IPOyear = csv.GetField<string>("IPOyear"),
                    LastSale = csv.GetField<double?>("LastSale"),
                    MarketCap = csv.GetField<double?>("MarketCap"),
                    Sector = csv.GetField<string>("Sector"),
                    SummaryQuote = csv.GetField<string>("Sector")
                };
            }

        }
    }
}
