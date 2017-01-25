using CsvHelper;
using ElasticFun.Models;
using HtmlAgilityPack;
using Microsoft.Practices.ObjectBuilder2;
using Nest;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ElasticFun.DataAccess
{
    public class ElasticRepo
    {
        private readonly ElasticClient client;

        public ElasticRepo()
        {
            var uri = "http://localhost.fiddler:9200";
            client = new ElasticClient(new Uri(uri));
        }

        public async Task AddDataAsync(string indexName,IProgress<string> progress)
        {
            //var path = @"Data/companylist.csv";
            var destination = @"Data/companyFulllist.csv";

            var companies = LoadFullCompany(destination);

            await client.DeleteIndexAsync(indexName);

            var items = companies.ToArray();
            for (double i = 0; i < items.Length; i++)
            {
                var company = items[(int)i];
                var p = (i / items.Length) * 100;
                progress.Report(Math.Round(p,2) + "%");

                await client.IndexAsync(company, id => id.Index(indexName));
            }
            progress.Report(null);
        }

        public async Task CreateListCSVAsync(string fileFrom, string fileTo, IProgress<string> progress)
        {
            var path = fileFrom;

            var companies = LoadCompany(path);
            using (CsvWriter csv = new CsvWriter(new StreamWriter(File.Open(fileTo, FileMode.OpenOrCreate))))
            {
                csv.WriteHeader<Company>();
                var items = companies.ToArray();
                for (double i = 0; i < items.Length; i++)
                {
                    var company = items[(int)i];
                    var p = (i / items.Length) * 100;
                    progress.Report(p + "%");
                    HtmlDocument doc = new HtmlDocument();
                    var responce = await WebRequest.Create(company.SummaryQuote).GetResponseAsync();

                    doc.Load(responce.GetResponseStream());

                    var data = doc.DocumentNode.QuerySelectorAll(@"#left-column-div div p");
                    if (data.Count > 1)
                    {
                        company.Text = data[1].InnerText;
                    }

                    csv.WriteRecord(company);
                }
                progress.Report(null);
            }
        }

        public async Task<IEnumerable<Index>> GetAllIndex()
        {
            var mappings = await client.GetMappingAsync(new GetMappingRequest());

            return mappings.Mappings.Select(map => new Index { Name = map.Key, Types = map.Value.Select(t => new Index() { IndexName = map.Key, Name = t.Key}) }).ToArray();
        }


        public IEnumerable<Company> LoadCompany(string location)
        {
            var csv = new CsvReader(new StreamReader(File.OpenRead(location)), new CsvHelper.Configuration.CsvConfiguration
            {
                IgnoreQuotes = false,
                QuoteAllFields = true,
                QuoteNoFields= false
            });
            csv.Configuration.QuoteAllFields = true;
            csv.Configuration.IgnoreQuotes = false;
            while (csv.Read())
            {
                if (csv.FieldHeaders.Length == csv.CurrentRecord.Length)
                {
                    double lastSale;
                    Double.TryParse(csv.GetField<string>("LastSale"), out lastSale);

                    int iPOyear;
                    Int32.TryParse(csv.GetField<string>("IPOyear"), out iPOyear);

                    double marketCap;
                    Double.TryParse(csv.GetField<string>("MarketCap"), out marketCap);
                    
                    yield return new Company
                    {
                        Symbol = csv.GetField<string>("Symbol"),
                        Name = csv.GetField<string>("Name"),
                        Industry = csv.GetField<string>("Industry"),
                        IPOyear = iPOyear,
                        LastSale = lastSale,
                        MarketCap = marketCap,
                        Sector = csv.GetField<string>("Sector"),
                        SummaryQuote = csv.GetField<string>("Summary Quote")
                    };
                }
            }
        }

        public IEnumerable<Company> LoadFullCompany(string location)
        {
            var csv = new CsvReader(new StreamReader(File.OpenRead(location)));

            while (csv.Read())
            {
                yield return csv.GetRecord<Company>();
            }
        }

        public async Task<DataResult> SearchAsync(string index, string type, string searchText, int skip = 0, int take = 100)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                var items = await client.SearchAsync<dynamic>(s => s.Index(index).Type(type).From(skip).Size(take));

                return new DataResult { Total = items.Total, Items = items.Hits.Select(item => item.Source).ToArray() };
            }
            else
            {
                
                var items = await client.SearchAsync<dynamic>(s => s.
                    Index(index).
                    Type(type).
                    From(skip).
                    Size(take).
                    Query(q => q.QueryString(qs => qs.Query(searchText))));

                return new DataResult { Total = items.Total, Items = items.Hits.Select(item => item.Source).ToArray() };
            }
        }

        public class DataResult
        {
            public long Total { get; set; }

            public IEnumerable<dynamic> Items { get; set; }
        }
    }
}
