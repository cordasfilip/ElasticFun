using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticFun.Models
{
    public class Index
    {
        public string Name { get; set; }

        public string IndexName { get; set; }

        public IEnumerable<Index> Types { get; set; }
    }
}
