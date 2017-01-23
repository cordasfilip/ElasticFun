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

        public DelegateCommand Init { get; set; }

        public DelegateCommand AddData { get; set; }

        public MainWindowViewModel()
        {
            db = new ElasticRepo();

            Init = DelegateCommand.FromAsyncHandler(OnInit);

            AddData = DelegateCommand.FromAsyncHandler(OnAddData);
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
