using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticFun.Models
{
    public class Company
    {
        //Symbol","Name","LastSale","MarketCap","ADR TSO","IPOyear","Sector","Industry","Summary Quote"
        public string Symbol { get; set; }

        public string Name { get; set; }

        public double? LastSale { get; set; }

        public double? MarketCap { get; set; }

        public int IPOyear { get; set; }

        public string Sector { get; set; }

        public string Industry { get; set; }

        public string SummaryQuote { get; set; }

        public string Text { get; set; }
    }
}
