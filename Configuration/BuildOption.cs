// Copyright 2018 Dan Pike
// Use of this source code is governed by a MIT license that can be
// found in the LICENSE file.

using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using YamlDotNet.Serialization;

namespace V8Builder.Configuration
{
    public class BuildOption : IComparable, INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Default { get; set; }
        public bool Selected { get; set; }
        public string Description { get; set; }
        public string NewValue
        {
            get => newValue_;
            set
            {
                if (newValue_ != value)
                {
                    newValue_ = value;
                    InvokePropertyChanged(nameof(NewValue), nameof(DisplayValue));
                }
            }
        }

        [YamlIgnore]
        public string DisplayValue
        {
            get => NewValue ?? Default;
            set
            {
                if (NewValue != value)
                {
                    NewValue = value;
                    InvokePropertyChanged(nameof(DisplayValue));
                }
            }
        }

        [YamlIgnore]
        public Visibility Visibility
        {
            get => visibility_;
            set
            {
                visibility_ = value;
                InvokePropertyChanged(nameof(Visibility));
            }
        }

        [YamlIgnore]
        public bool ReadOnly { get; set; } = true;

        public int CompareTo(object other)
        {
            return string.Compare(Name, (other as BuildOption)?.Name, StringComparison.OrdinalIgnoreCase);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(params string[] propertyNames)
        {
            if (App.GuiDispatcher != null
                && App.GuiThread.ManagedThreadId != Thread.CurrentThread.ManagedThreadId
                )
            {
                App.GuiDispatcher.Invoke(() => InvokePropertyChanged(propertyNames));
            }
            else
            {
                foreach (var propertyName in propertyNames)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }

        private string newValue_ = string.Empty;
        private Visibility visibility_ = Visibility.Visible;
    }
}