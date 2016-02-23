using System.Windows;
using Client.ViewModels;

namespace Client.Views
{
    /// <summary>
    /// Interaction logic for HistoryView.xaml
    /// </summary>
    public partial class HistoryView : Window
    {
        public HistoryView()
        {
            InitializeComponent();
        }

        public HistoryView(HistoryListViewModel history)
        {
            InitializeComponent();
            HistoryGrid.DataContext = history;
        }
    }
}
