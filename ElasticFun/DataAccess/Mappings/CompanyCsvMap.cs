using CsvHelper.Configuration;
using ElasticFun.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticFun.DataAccess.Mappings
{
    public class CompanyCsvMap : CsvClassMap<Company>
    {
        public CompanyCsvMap()
        {
            AutoMap();
            Map(m => m.IPOyear).ConvertUsing(reader => 
                {
                    int iPOyear;
                    Int32.TryParse(reader.GetField<string>("IPOyear"), out iPOyear);
                    return new DateTime(iPOyear==0?1:iPOyear, 1, 1);
                });
        }
    }
}
