using System.Collections.Generic;
using Client.ViewModels;

namespace Client.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow(Dictionary<string, ICollection<string>> rolesOnWorkflows)
        {
            var vm = new WorkflowListViewModel(rolesOnWorkflows);
            DataContext = vm;
            InitializeComponent();
            if (vm.CloseAction == null)
                vm.CloseAction = Close;
        }
    }
}
