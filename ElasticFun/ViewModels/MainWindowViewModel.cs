using ElasticFun.DataAccess;
using ElasticFun.Models;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

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
            set { SetProperty(ref data, value); }
        }

        public DelegateCommand Init { get; set; }

        public DelegateCommand AddData { get; set; }

        public DelegateCommand<Index> SelectionChanged { get; set; }

        public DelegateCommand<Index> Search { get; set; }

        public MainWindowViewModel()
        {
            db = new ElasticRepo();

            Init = DelegateCommand.FromAsyncHandler(OnInit);

            AddData = DelegateCommand.FromAsyncHandler(OnAddData);

            Search = DelegateCommand<Index>.FromAsyncHandler(OnSearch);

            SelectionChanged = DelegateCommand<Index>.FromAsyncHandler(OnSearch);
        }

        private async Task OnSearch(Index index = null)
        {
            StartLoad();
            index = index ?? new Index { Name="", IndexName = "_all" };
            var indexName = string.IsNullOrEmpty(index.IndexName) ? index.Name : index.IndexName;
            var typeName = string.IsNullOrEmpty(index.IndexName) ? "" : index.Name;
            var data = await db.SearchAsync(indexName, typeName,SearchText);
            Total = data.Total;
            Data = new ObservableCollection<dynamic>(data.Items);
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

        private void StartLoad()
        {
            IsLoading = Visibility.Visible;
        }

        private void EndLoad()
        {
            IsLoading = Visibility.Collapsed;
        }
    }
}
