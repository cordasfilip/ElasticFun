using ElasticFun.DataAccess;
using ElasticFun.Models;
using ElasticFun.Views;
using Newtonsoft.Json.Linq;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ElasticFun.DataAccess;

namespace ElasticFun.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly ElasticRepo db;

        private dynamic selectedItem;
        public Index SelectedItem
        {
            get { return selectedItem; }
            set { SetProperty(ref selectedItem, value); }
        }

        private ObservableCollection<Index> indexList;
        public ObservableCollection<Index> IndexList
        {
            get { return indexList; }
            set { SetProperty(ref indexList, value); }
        }

        private string progress;
        public string Progress
        {
            get { return progress; }
            set { SetProperty(ref progress, value); }
        }

        private Visibility isLoading;
        public Visibility IsLoading
        {
            get { return isLoading; }
            set { SetProperty(ref isLoading, value); }
        }

        private string searchText;
        public string SearchText
        {
            get { return searchText; }
            set { SetProperty(ref searchText, value); }
        }

        private long total;
        public long Total
        {
            get { return total; }
            set { SetProperty(ref total, value); }
        }

        private ObservableCollection<dynamic> data;
        public ObservableCollection<dynamic> Data
        {
            get { return data; }
            set
            {
                SetProperty(ref data, value);
                base.OnPropertyChanged("Items");
            }
        }

        private ObservableCollection<Q> queries;
        public ObservableCollection<Q> Queries
        {
            get { return queries; }
            set
            {
                SetProperty(ref queries, value);
            }
        }

        private JObject selectedRow;
        public JObject SelectedRow
        {
            get { return selectedRow; }
            set
            {
                SetProperty(ref selectedRow, value);
                OnPropertyChanged("SelectedRowMap");
                OnPropertyChanged("SelectedHighLight");
            }
        }

        public IEnumerable<Tuple<string, string>> SelectedRowMap
        {
            get
            {
                if (SelectedRow == null)
                {
                    return null;
                }
                return SelectedRow.Properties().Where(p=>
                p.Value.Type != JTokenType.Object && p.Value.Type != JTokenType.Array && p.Name != "highlight")
                .Select( p => Tuple.Create(p.Name,p.Value.ToObject<string>())).ToArray();
            }
        }

        public string SelectedHighLight
        {
            get
            {
                if (SelectedRow == null)
                {
                    return null;
                }

                var data = SelectedRow.Value<string>("highlight");
                return data;
            }
            set { }
        }


        private Q query;
        public Q Query
        {
            get { return query; }
            set
            {
                SetProperty(ref query, value);
                OnSearch();
            }
        }

        public IEnumerable<ElasticRepo.ChartData> ChartData { get; set; }

        public DelegateCommand Init { get; set; }

        public DelegateCommand AddData { get; set; }

        public DelegateCommand<Index> SelectionChanged { get; set; }

        public DelegateCommand<Index> Search { get; set; }

        public DelegateCommand MonitorQuery { get; set; }

        public DelegateCommand ShowChart { get; set; }

        public DelegateCommand Deselect { get; set; }

        public MainWindowViewModel()
        {
            db = new ElasticRepo();

            Init = DelegateCommand.FromAsyncHandler(OnInit);

            MonitorQuery =new DelegateCommand(OnMonitorQuery);

            AddData = DelegateCommand.FromAsyncHandler(OnAddData);

            Search = DelegateCommand<Index>.FromAsyncHandler(OnSearch);

            SelectionChanged = DelegateCommand<Index>.FromAsyncHandler(OnSearch);

            ShowChart =new DelegateCommand(OnShowChart);

            Deselect = new DelegateCommand(() => 
            {
                Query = null;
                SelectedItem = null;
            });
        }

        private void OnShowChart()
        {
            var window = new ChartWindow(ChartData);
            window.ShowDialog();
        }

        private async Task OnSearch(Index index = null)
        {
            StartLoad();
            index = index ?? new Index { Name="", IndexName = "_all" };
            var indexName = string.IsNullOrEmpty(index.IndexName) ? index.Name : index.IndexName;
            var typeName = string.IsNullOrEmpty(index.IndexName) ? "" : index.Name;

            string query = null;
            if (Query != null)
            {
                query = File.ReadAllText(Query.Path);
            }

            var data = await db.SearchAsync(indexName, typeName,SearchText,query);
            Total = data.Total;

            Data = new ObservableCollection<dynamic>(data.Items);

            ChartData = data.Points;
            EndLoad();
        }
        public async Task OnAddData()
        {
            StartLoad();
            await db.AddDataAsync("company", new Progress<string>((p) => Progress = p));

            EndLoad();
        }

        public async Task OnInit()
        {
            StartLoad();
            IndexList = new ObservableCollection<Index>(await db.GetAllIndex());
            await OnSearch();
            EndLoad();
        }

        public void OnMonitorQuery()
        {
            var dir = "Query";

            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            var items = System.IO.Directory.GetFiles(dir).Select(p => new FileInfo(p)).Select(fi=>new Q{ Name = Path.GetFileNameWithoutExtension(fi.Name), Path= fi.FullName });

            Queries = new ObservableCollection<Q>(items);

            FileSystemWatcher watcher = new FileSystemWatcher(dir);
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
         | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            // Only watch text files.
            watcher.Filter = "*.json";


            watcher.Created += (object sender, FileSystemEventArgs e) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    var path = new FileInfo(e.FullPath).FullName;
                    Queries.Add(new Q { Name = Path.GetFileNameWithoutExtension(e.Name), Path = path });
                });
            };

            watcher.Changed += (object sender, FileSystemEventArgs e) =>
            {
                var path = new FileInfo(e.FullPath).FullName;
                if (Query != null && Query.Path == path)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        OnSearch();
                    });
                }
            };

            watcher.Deleted += (object sender, FileSystemEventArgs e) =>
            {
                App.Current.Dispatcher.Invoke(() => 
                {
                    var path = new FileInfo(e.FullPath).FullName;
                    Queries.Remove(Queries.FirstOrDefault(q=>q.Path == path));
                });
            };

            watcher.EnableRaisingEvents = true;
        }

        private void StartLoad()
        {
            IsLoading = Visibility.Visible;
        }

        private void EndLoad()
        {
            IsLoading = Visibility.Collapsed;
        }

        public class Q
        {
            public string Name { get; set; }
            public string Path { get; set; }
        }
    }
}
