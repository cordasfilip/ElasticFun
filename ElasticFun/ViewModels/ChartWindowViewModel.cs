using Nest;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElasticFun.DataAccess;

namespace ElasticFun.ViewModels
{
    public class ChartWindowViewModel: BindableBase
    {
        public List<ChartModel> Charts { get; private set; }

        public ChartWindowViewModel(IEnumerable<ElasticRepo.ChartData> points)
        {
            Charts = new List<ChartModel>();
            points = points ?? Enumerable.Empty<ElasticRepo.ChartData>();


            foreach (var series in points)
            {
                var plot = new PlotModel { Title = series.Key };
                plot.Axes.Add(new CategoryAxis { Position = AxisPosition.Left, ItemsSource = series.Value.Items, LabelField = "Key" , IsZoomEnabled = false, IsPanEnabled = false});
                plot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, MinimumPadding = 0, AbsoluteMinimum = 0, IsZoomEnabled = false, IsPanEnabled = false });

                if (series.Value.Items.FirstOrDefault() is KeyedBucket<object>)
                {
                    var plot2 = new PlotModel { Title = series.Key };
                    plot2.Axes.Add(new CategoryAxis { Position = AxisPosition.Left, ItemsSource = series.Value.Items, LabelField = "Key", IsZoomEnabled = false, IsPanEnabled = false });
                    plot2.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, MinimumPadding = 0, AbsoluteMinimum = 0, IsZoomEnabled = false, IsPanEnabled = false });
                    var data = series.Value.Items.Cast<KeyedBucket<object>>().Select(kb => new { Value = (kb.Aggregations.FirstOrDefault().Value as Nest.ValueAggregate).Value }).ToArray();
                    var bar2 = new BarSeries
                    {
                        Title = series.Key,
                        ItemsSource = data,
                        ValueField = "Value",
                        FillColor= OxyColor.FromRgb(0x00,0x46,0x7B)
                    };
                    plot2.Series.Add(bar2);
                    Charts.Add(new ChartModel { Model = plot2 });
                }
                var bar = new BarSeries
                {
                    Title = series.Key,
                    ItemsSource = series.Value.Items,
                    ValueField = "DocCount",
                    FillColor = OxyColor.FromRgb(0x00, 0x46, 0x7B)
                };
                plot.Series.Add(bar);

                Charts.Add(new ChartModel { Model = plot });
            }
        }

        public class ChartModel
        {
            public PlotModel Model { get; set; }
        }
    }
}
