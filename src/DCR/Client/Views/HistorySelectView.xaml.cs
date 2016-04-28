using System;
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

            Closing += (sender, args) => ((IDisposable)DataContext).Dispose();
        }

        public HistorySelectView(HistorySelectViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            Closing += (sender, args) => vm.Dispose();
        }
    }
}
