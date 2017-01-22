using ElasticFun.Models;
using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace ElasticFun.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
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

        public MainWindowViewModel()
        {
            IndexList = new ObservableCollection<Index>(new Index[] 
            {
                new Index
                {
                    Name = "A",
                    Types = new ObservableCollection<Index>(new Index[] 
                    {
                        new Index{ Name = "B" },
                         new Index{ Name = "B" },
                          new Index{ Name = "B" }
                    })
                }
            });
           
        }
    }
}
