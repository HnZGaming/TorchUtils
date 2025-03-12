using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Utils.Torch
{
    internal sealed class ObservableSet<T>
    {
        HashSet<T> _self = new();
        ObservableCollection<T> _source;

        public void SetSource(ObservableCollection<T> source)
        {
            _self = new HashSet<T>(source);

            if (_source != null)
            {
                _source.CollectionChanged -= OnSourceChanged;
            }

            _source = source;
            _source.CollectionChanged += OnSourceChanged;
        }

        void OnSourceChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add when e.NewItems?.Count > 0:
                {
                    foreach (T item in e.NewItems)
                    {
                        _self.Add(item);
                    }

                    break;
                }
                case NotifyCollectionChangedAction.Remove when e.OldItems?.Count > 0:
                {
                    foreach (T item in e.OldItems)
                    {
                        _self.Remove(item);
                    }

                    break;
                }
                case NotifyCollectionChangedAction.Reset:
                {
                    _self = new HashSet<T>(_source);
                    break;
                }
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool Contains(T item)
        {
            return _self.Contains(item);
        }
    }
}