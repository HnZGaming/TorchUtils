using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Torch;

namespace Utils.Torch
{
    public class ViewModelCollection<T> : ObservableCollection<T> where T : ViewModel
    {
        public ViewModelCollection()
        {
            CollectionChangedEventManager.AddHandler(this, OnMyCollectionChanged);
        }

        void OnMyCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var element in this)
            {
                PropertyChangedEventManager.RemoveHandler(element, OnRemotePortPropertyChanged, "");
                PropertyChangedEventManager.AddHandler(element, OnRemotePortPropertyChanged, "");
            }
        }

        void OnRemotePortPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}