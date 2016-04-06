using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Client.Annotations;

namespace Client.ViewModels
{
    /// <summary>
    /// An abstract class which provides all the viewmodels with the neccesary functionality to bind to a xaml view.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
