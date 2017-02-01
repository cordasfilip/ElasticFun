using CsvHelper;
using ElasticFun.DataAccess.Mappings;
using ElasticFun.Models;
using Elasticsearch.Net;
using HtmlAgilityPack;
using Microsoft.Practices.ObjectBuilder2;
using Nest;
using Newtonsoft.Json.Linq;
using OxyPlot.Series;
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
            var uri = "http://localhost:9200";
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
                        IPOyear = new DateTime(iPOyear,1,1),
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
            csv.Configuration.RegisterClassMap<CompanyCsvMap>();
            while (csv.Read())
            {
                yield return csv.GetRecord<Company>();
            }
        }

        public async Task<DataResult> SearchAsync(string index, string type, string searchText,string query, int skip = 0, int take = 100)
        {
            try
            {
                if (string.IsNullOrEmpty(query) && string.IsNullOrEmpty(searchText))
                {
                    var items = await client.SearchAsync<dynamic>(s => s.Index(index).Type(type).From(skip).Size(take));

                    return new DataResult { Total = items.Total, Items = items.Hits.Select(item => item.Source).ToArray() };
                }
                else
                {

                    ISearchResponse<JObject> items;

                    if (string.IsNullOrEmpty(query))
                    {
                        items = await client.SearchAsync<JObject>(s => s.
                                   Index(index).
                                   Type(type).
                                   From(skip).
                                   Size(take).
                                   Query(q => q.QueryString(qs => qs.Query(searchText))).
                                   Highlight(hl=>hl.Fields(f=>f.AllField().PreTags("<Run Foreground=\"{DynamicResource HighlightBrush}\">").PostTags("</Run>"))));
                    }
                    else
                    {
                        ElasticsearchResponse<SearchResponse<JObject>> data;
                        if (index == "_all")
                        {
                            data = await client.LowLevel.SearchAsync<SearchResponse<JObject>>(query);
                        }
                        else
                        {
                            data = String.IsNullOrEmpty(type) ?
                           await client.LowLevel.SearchAsync<SearchResponse<JObject>>(index, query) :
                           await client.LowLevel.SearchAsync<SearchResponse<JObject>>(index, type, query);
                        }

                        var response = data.Body;
                        items = response;
                    }

                    return new DataResult
                    {
                        Total = items.Total,
                        Items = items.Hits.Select(
                        item =>
                        {
                            if (item.Highlights.Count > 0)
                            {
                                var data = from h in item.Highlights
                                           from fh in h.Value.Highlights
                                           select fh;
                                StringBuilder sb = new StringBuilder();
                                sb.AppendLine(@"<Section xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" >");
                                foreach (var p in data)
                                {
                                    sb.AppendLine("<Paragraph>");
                                    sb.AppendLine(p);
                                    sb.AppendLine("</Paragraph>");
                                }
                                sb.AppendLine("</Section>");
                                item.Source.Add("highlight", sb.ToString());
                            }

                            return item.Source;
                        }
                        ).ToArray(),
                        Points = items
                       .Aggregations
                       .Where(a => a.Value is BucketAggregate)
                       .Select(a => new ChartData { Key = a.Key, Value = a.Value as BucketAggregate })
                    };
                }
            }
            catch (Exception e)
            {
                return new DataResult
                {
                    Total = 0,
                    Items = Enumerable.Empty<JObject>()
                };
            }
        }

        public class DataResult
        {
            public long Total { get; set; }

            public IEnumerable<dynamic> Items { get; set; }

            public IEnumerable<ChartData> Points { get; set; }
        }

        public class ChartData
        {
            public string Key { get; set; }

            public BucketAggregate Value { get; set; }
        }
    }
}
