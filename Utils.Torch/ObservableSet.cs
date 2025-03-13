using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Utils.Torch
{
    internal sealed class ObservableSet<T, U>
    {
        HashSet<U> _self = new();
        ObservableCollection<T> _source;
        readonly Func<T, U> _selector;

        public ObservableSet(Func<T, U> selector)
        {
            _selector = selector;
        }

        public void SetSource(ObservableCollection<T> source)
        {
            _self = new HashSet<U>(source.Select(_selector));

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
                        _self.Add(_selector(item));
                    }

                    break;
                }
                case NotifyCollectionChangedAction.Remove when e.OldItems?.Count > 0:
                {
                    foreach (T item in e.OldItems)
                    {
                        _self.Remove(_selector(item));
                    }

                    break;
                }
                case NotifyCollectionChangedAction.Reset:
                {
                    _self = new HashSet<U>(_source.Select(_selector));
                    break;
                }
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool Contains(U item)
        {
            return _self.Contains(item);
        }
    }
}