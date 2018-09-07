// Copyright 2018 Dan Pike
// Use of this source code is governed by a MIT license that can be
// found in the LICENSE file.

using MahApps.Metro.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using V8Builder.Configuration;

namespace V8Builder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Title = app_.Title;
            dataContext_ = DataContext as MainViewModel;
        }

        private void AvailableOptions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MoveAvailable();
        }

        private void Sort(ObservableCollection<BuildOption> options)
        {
            var sorted = options.OrderBy(x => x).ToList();
            for (int ii = 0; ii < sorted.Count(); ii++)
            {
                options.Move(options.IndexOf(sorted[ii]), ii);
            }
        }

        public void MoveAvailable()
        {
            try
            {
                ignoreSelectionChange_ = true;
                var selectedOptions = AvailableOptionsList.SelectedItems.Cast<Configuration.BuildOption>().ToList();
                foreach (var selectedOption in selectedOptions)
                {
                    selectedOption.Selected = true;
                    dataContext_.AvailableOptions.Remove(selectedOption);
                    dataContext_.SelectedOptions.Add(selectedOption);
                    SelectedOptionsList.SelectedItem = selectedOption;
                }
                Sort(dataContext_.SelectedOptions);
                dataContext_.InvokePropertyChanged(nameof(dataContext_.Config), nameof(dataContext_.CommandLine)
                    , nameof(dataContext_.DescribingOption));
            }
            finally
            {
                ignoreSelectionChange_ = false;
            }
        }

        public void MoveSelected()
        {
            try
            {
                ignoreSelectionChange_ = true;
                var selectedOptions = SelectedOptionsList.SelectedItems.Cast<Configuration.BuildOption>().ToList();
                foreach (var selectedOption in selectedOptions)
                {
                    selectedOption.Selected = false;
                    selectedOption.NewValue = null;
                    dataContext_.SelectedOptions.Remove(selectedOption);
                    dataContext_.AvailableOptions.Add(selectedOption);
                    AvailableOptionsList.SelectedItem = selectedOption;
                }
                Sort(dataContext_.AvailableOptions);
                dataContext_.InvokePropertyChanged(nameof(dataContext_.Config), nameof(dataContext_.CommandLine)
                    , nameof(dataContext_.DescribingOption));
            }
            finally
            {
                ignoreSelectionChange_ = false;
            }
        }

        private void AvailableOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ignoreSelectionChange_)
            {
                return;
            }
            try
            {
                ignoreSelectionChange_ = true;
                SelectedOptionsList.SelectedItem = null;
                dataContext_.DescribingOption = AvailableOptionsList.SelectedItem as Configuration.BuildOption;
                dataContext_.DescribingOption.ReadOnly = true;
            }
            finally
            {
                ignoreSelectionChange_ = false;
            }
        }

        private void SelectedOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ignoreSelectionChange_)
            {
                return;
            }
            try
            {
                ignoreSelectionChange_ = true;
                AvailableOptionsList.SelectedItem = null;
                dataContext_.DescribingOption = SelectedOptionsList.SelectedItem as Configuration.BuildOption;
            }
            finally
            {
                ignoreSelectionChange_ = false;
            }
        }

        private void SelectedOptions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MoveSelected();
        }

        private void DescribingOption_NewValueChanged(object sender, TextChangedEventArgs e)
        {
            dataContext_?.InvokePropertyChanged(nameof(dataContext_.CommandLine));
        }

        private void BuildFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            dataContext_?.InvokePropertyChanged(nameof(dataContext_.CommandLine));
        }

        private void BuildConfigurationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dataContext_?.InvokePropertyChanged(nameof(dataContext_.CommandLine));
        }

        private void ApplyFilter(ListBox listBox, string filterText)
        {
            filterText = filterText?.ToLower() ?? string.Empty;

            foreach (var option in listBox.Items.Cast<BuildOption>())
            {
                var visibility = string.IsNullOrEmpty(filterText) || option.Name.ToLower().Contains(filterText)
                    ? Visibility.Visible : Visibility.Collapsed;
                if (option.Visibility != visibility)
                {
                    option.Visibility = visibility;
                }
            }
        }

        private void FilterAvailable_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter(AvailableOptionsList, dataContext_?.AvailableFilter);
        }

        private void FilterSelected_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter(SelectedOptionsList, dataContext_?.SelectedFilter);
        }

        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Tabs.SelectedIndex == 1 && BuildConfigurationsList.SelectedItem != null)
            {
                BuildConfigurationsList.ScrollIntoView(BuildConfigurationsList.SelectedItem);
            }
        }

        private App app_ = Application.Current as App;
        private MainViewModel dataContext_;
        private bool ignoreSelectionChange_;
    }
}