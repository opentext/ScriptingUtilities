using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LookupListUpdater
{
    [Serializable]
    public class ModelBase : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public static bool Dirty = false;

        protected void SetField<T>(ref T field, T value, [CallerMemberName]string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            Dirty = true;
            RaisePropertyChanged(propertyName);
        }

        protected void SendPropertyChanged ([CallerMemberName]string propertyName = null)
        {
            RaisePropertyChanged(propertyName);
        }

        public void RaisePropertyChanged(string name)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
