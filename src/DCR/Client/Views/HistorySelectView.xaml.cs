using System.Windows;
using Client.ViewModels;

namespace Client.Views
{
    /// <summary>
    /// Interaction logic for HistorySelectView.xaml
    /// </summary>
    public partial class HistorySelectView : Window
    {
        public HistorySelectView()
        {
            InitializeComponent();
        }

        public HistorySelectView(HistorySelectViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
