using Microsoft.Practices.Unity;
using Prism.Unity;
using ElasticFun.Views;
using System.Windows;

namespace ElasticFun
{
    class Bootstrapper : UnityBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void InitializeShell()
        {
            Application.Current.MainWindow.Show();
        }
    }
}
