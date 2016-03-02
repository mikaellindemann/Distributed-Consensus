using System.Windows;
using System.Windows.Controls;
using Client.ViewModels;

namespace Client.Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView
    {
        public LoginView()
        {
            InitializeComponent();
            var vm = new LoginViewModel(); // this creates an instance of the ViewModel
            DataContext = vm; // this sets the newly created ViewModel as the DataContext for the View
            vm.CloseAction += Close;
        }

        private void Password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext != null)
            {
                ((LoginViewModel)DataContext).Password = ((PasswordBox)sender).Password;
            }
        }
    }
}
