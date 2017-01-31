using ElasticFun.ViewModels;
using MahApps.Metro.Controls;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ElasticFun.DataAccess;

namespace ElasticFun.Views
{
    /// <summary>
    /// Interaction logic for ChartWindow.xaml
    /// </summary>
    public partial class ChartWindow : MetroWindow
    {

        public ChartWindow(IEnumerable<ElasticRepo.ChartData> points)
        {
            DataContext = new ChartWindowViewModel(points);

            InitializeComponent();
        }
    }
}
