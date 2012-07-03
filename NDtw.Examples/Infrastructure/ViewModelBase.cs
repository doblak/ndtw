using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace NDtw.Examples.Infrastructure
{
    public class ViewModelBase : INotifyPropertyChanged 
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged<T>(Expression<Func<T>> expression)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(StrongTypingHelper.GetProperty(expression).Name));
        }
    }
}
