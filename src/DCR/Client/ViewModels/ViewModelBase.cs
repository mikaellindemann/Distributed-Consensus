using System;
using System.ComponentModel;

namespace Client.ViewModels
{
    /// <summary>
    /// An abstract class which provides all the viewmodels with the neccesary functionality to bind to a xaml view.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Sends a notification to listeners/binders that the property with name of the parameter.
        /// If an empty string is provided in the parameter, all bindings to the datacontext will be updated.
        /// </summary>
        /// <param name="info">The name of the property which has been updated.</param>
        public void NotifyPropertyChanged(String info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }
}
