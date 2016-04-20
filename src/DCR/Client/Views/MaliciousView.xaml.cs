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
using Client.ViewModels;

namespace Client.Views
{
    /// <summary>
    /// Interaction logic for MaliciousView.xaml
    /// </summary>
    public partial class MaliciousView : Window
    {
        public MaliciousView()
        {
            InitializeComponent();
        }

        public MaliciousView(MaliciousViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
