using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ExcelLikeGrid.UI.Filtering;
using ExcelLikeGrid.UI.Helpers;
using ExcelLikeGrid.UI.Models;

namespace ExcelLikeGrid.UI.Controls
{
    public class ExcelLikeDataGrid : DataGrid
    {
        private readonly Dictionary<string, ColumnFilterState> _filters = new();
        private Popup? _currentFilterPopup;

        static ExcelLikeDataGrid()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ExcelLikeDataGrid),
                new FrameworkPropertyMetadata(typeof(ExcelLikeDataGrid)));
        }

        public ExcelLikeDataGrid()
        {
            AddHandler(
                DataGridColumnHeader.MouseRightButtonUpEvent,
                new MouseButtonEventHandler(OnHeaderRightClick),
                true);
        }

        private void OnHeaderRightClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject source)
                return;

            var header = FindParent<DataGridColumnHeader>(source);
            if (header?.Column == null)
                return;

            e.Handled = true;
            ShowHeaderMenu(header);
        }

        private void ShowHeaderMenu(DataGridColumnHeader header)
        {
            var column = header.Column;
            var sortMemberPath = column.SortMemberPath;

            if (string.IsNullOrWhiteSpace(sortMemberPath))
                return;

            CloseCurrentPopup();

            var menu = new ContextMenu();

            var sortAsc = new MenuItem { Header = "Sort Ascending" };
            sortAsc.Click += (_, _) => ApplySort(sortMemberPath, ListSortDirection.Ascending);

            var sortDesc = new MenuItem { Header = "Sort Descending" };
            sortDesc.Click += (_, _) => ApplySort(sortMemberPath, ListSortDirection.Descending);

            var clearSort = new MenuItem { Header = "Clear Sort" };
            clearSort.Click += (_, _) => ClearSort();

            var openFilterPopup = new MenuItem { Header = "Filter..." };
            openFilterPopup.Click += (_, _) =>
            {
                menu.IsOpen = false;
                ShowFilterPopup(header, sortMemberPath);
            };

            var clearFilter = new MenuItem
            {
                Header = "Clear Filter",
                IsEnabled = _filters.ContainsKey(sortMemberPath)
            };
            clearFilter.Click += (_, _) => ClearFilter(sortMemberPath);

            menu.Items.Add(sortAsc);
            menu.Items.Add(sortDesc);
            menu.Items.Add(new Separator());
            menu.Items.Add(openFilterPopup);
            menu.Items.Add(clearFilter);
            menu.Items.Add(new Separator());
            menu.Items.Add(clearSort);

            menu.PlacementTarget = header;
            menu.IsOpen = true;
        }

        private void ShowFilterPopup(DataGridColumnHeader header, string propertyName)
        {
            CloseCurrentPopup();

            var options = BuildFilterOptions(propertyName);

            var outerBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10),
                Width = 240,
                Child = BuildFilterPopupContent(propertyName, options)
            };

            _currentFilterPopup = new Popup
            {
                PlacementTarget = header,
                Placement = PlacementMode.Bottom,
                StaysOpen = true,
                AllowsTransparency = true,
                Child = outerBorder,
                IsOpen = true
            };
        }

        private UIElement BuildFilterPopupContent(string propertyName, List<FilterOptionItem> options)
        {
            var panel = new StackPanel();

            var title = new TextBlock
            {
                Text = $"Filter: {propertyName}",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            panel.Children.Add(title);

            var searchLabel = new TextBlock
            {
                Text = "Search",
                Margin = new Thickness(0, 0, 0, 4)
            };
            panel.Children.Add(searchLabel);

            var searchBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 8)
            };
            panel.Children.Add(searchBox);

            var selectAllCheckBox = new CheckBox
            {
                Content = "(Select All)",
                Margin = new Thickness(0, 0, 0, 8),
                IsThreeState = true
            };
            panel.Children.Add(selectAllCheckBox);

            var scrollViewer = new ScrollViewer
            {
                Height = 180,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var optionsPanel = new StackPanel();
            var optionCheckBoxes = new List<CheckBox>();

            bool suppressSelectAllEvents = false;
            bool bulkUpdating = false;

            void UpdateSelectAllState()
            {
                var visibleBoxes = optionCheckBoxes
                    .Where(x => x.Visibility == Visibility.Visible)
                    .ToList();

                suppressSelectAllEvents = true;

                if (visibleBoxes.Count == 0)
                {
                    selectAllCheckBox.IsChecked = false;
                }
                else
                {
                    int checkedCount = visibleBoxes.Count(x => x.IsChecked == true);

                    if (checkedCount == 0)
                    {
                        selectAllCheckBox.IsChecked = false;
                    }
                    else if (checkedCount == visibleBoxes.Count)
                    {
                        selectAllCheckBox.IsChecked = true;
                    }
                    else
                    {
                        selectAllCheckBox.IsChecked = null;
                    }
                }

                suppressSelectAllEvents = false;
            }

            void ApplySearchFilter()
            {
                string searchText = searchBox.Text?.Trim() ?? "";

                foreach (var checkBox in optionCheckBoxes)
                {
                    if (checkBox.Tag is not FilterOptionItem option)
                        continue;

                    bool isVisible =
                        string.IsNullOrWhiteSpace(searchText) ||
                        option.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase);

                    checkBox.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                }

                UpdateSelectAllState();
            }

            foreach (var option in options)
            {
                var checkBox = new CheckBox
                {
                    Content = option.Value,
                    IsChecked = option.IsSelected,
                    Margin = new Thickness(0, 2, 0, 2),
                    Tag = option
                };

                checkBox.Checked += (_, _) =>
                {
                    option.IsSelected = true;
                    if (!bulkUpdating)
                        UpdateSelectAllState();
                };

                checkBox.Unchecked += (_, _) =>
                {
                    option.IsSelected = false;
                    if (!bulkUpdating)
                        UpdateSelectAllState();
                };

                optionCheckBoxes.Add(checkBox);
                optionsPanel.Children.Add(checkBox);
            }

            selectAllCheckBox.Click += (_, _) =>
            {
                if (suppressSelectAllEvents)
                    return;

                if (selectAllCheckBox.IsChecked is null)
                    return;

                bool shouldSelect = selectAllCheckBox.IsChecked == true;

                bulkUpdating = true;

                foreach (var checkBox in optionCheckBoxes.Where(x => x.Visibility == Visibility.Visible))
                {
                    checkBox.IsChecked = shouldSelect;

                    if (checkBox.Tag is FilterOptionItem option)
                        option.IsSelected = shouldSelect;
                }

                bulkUpdating = false;
                UpdateSelectAllState();
            };

            searchBox.TextChanged += (_, _) => ApplySearchFilter();

            scrollViewer.Content = optionsPanel;
            panel.Children.Add(scrollViewer);

            var buttonRow = new Grid
            {
                Margin = new Thickness(0, 10, 0, 0)
            };

            buttonRow.ColumnDefinitions.Add(new ColumnDefinition());
            buttonRow.ColumnDefinitions.Add(new ColumnDefinition());
            buttonRow.ColumnDefinitions.Add(new ColumnDefinition());

            var applyButton = new Button
            {
                Content = "Apply",
                Margin = new Thickness(0, 0, 6, 0),
                Padding = new Thickness(10, 4, 10, 4)
            };
            applyButton.Click += (_, _) =>
            {
                ApplyMultiValueFilter(propertyName, options);
                CloseCurrentPopup();
            };
            Grid.SetColumn(applyButton, 0);

            var clearButton = new Button
            {
                Content = "Clear",
                Margin = new Thickness(0, 0, 6, 0),
                Padding = new Thickness(10, 4, 10, 4)
            };
            clearButton.Click += (_, _) =>
            {
                ClearFilter(propertyName);
                CloseCurrentPopup();
            };
            Grid.SetColumn(clearButton, 1);

            var closeButton = new Button
            {
                Content = "Close",
                Padding = new Thickness(10, 4, 10, 4)
            };
            closeButton.Click += (_, _) => CloseCurrentPopup();
            Grid.SetColumn(closeButton, 2);

            buttonRow.Children.Add(applyButton);
            buttonRow.Children.Add(clearButton);
            buttonRow.Children.Add(closeButton);

            panel.Children.Add(buttonRow);

            UpdateSelectAllState();

            return panel;
        }

        private List<FilterOptionItem> BuildFilterOptions(string propertyName)
        {
            var distinctValues = GetDistinctValues(propertyName).ToList();

            _filters.TryGetValue(propertyName, out var currentFilter);
            var selectedValues = currentFilter?.SelectedValues ?? new HashSet<string>();

            return distinctValues
                .Select(v => new FilterOptionItem
                {
                    Value = v,
                    IsSelected = selectedValues.Count == 0 || selectedValues.Contains(v)
                })
                .ToList();
        }

        private void ApplyMultiValueFilter(string propertyName, List<FilterOptionItem> options)
        {
            var selected = options
                .Where(x => x.IsSelected)
                .Select(x => x.Value)
                .ToHashSet();

            if (selected.Count == 0 || selected.Count == options.Count)
            {
                _filters.Remove(propertyName);
            }
            else
            {
                _filters[propertyName] = new ColumnFilterState
                {
                    ColumnKey = propertyName,
                    SelectedValues = selected
                };
            }

            RefreshFilter();
        }

        private void ApplySort(string propertyName, ListSortDirection direction)
        {
            if (ItemsSource == null)
                return;

            var view = CollectionViewSource.GetDefaultView(ItemsSource);
            if (view == null)
                return;

            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(propertyName, direction));
            view.Refresh();

            foreach (var col in Columns)
                col.SortDirection = null;

            var targetColumn = Columns.FirstOrDefault(c => c.SortMemberPath == propertyName);
            if (targetColumn != null)
                targetColumn.SortDirection = direction;
        }

        private void ClearSort()
        {
            if (ItemsSource == null)
                return;

            var view = CollectionViewSource.GetDefaultView(ItemsSource);
            if (view == null)
                return;

            view.SortDescriptions.Clear();
            view.Refresh();

            foreach (var col in Columns)
                col.SortDirection = null;
        }

        private void ClearFilter(string propertyName)
        {
            if (_filters.Remove(propertyName))
            {
                RefreshFilter();
            }
        }

        private void RefreshFilter()
        {
            if (ItemsSource == null)
                return;

            var view = CollectionViewSource.GetDefaultView(ItemsSource);
            if (view == null)
                return;

            view.Filter = _filters.Count == 0 ? null : MatchesAllFilters;
            view.Refresh();

            UpdateFilterIndicators();
        }

        private void UpdateFilterIndicators()
        {
            foreach (var column in Columns)
            {
                bool hasFilter =
                    !string.IsNullOrWhiteSpace(column.SortMemberPath) &&
                    _filters.TryGetValue(column.SortMemberPath, out var state) &&
                    state.HasFilter;

                ColumnFilterProperties.SetHasActiveFilter(column, hasFilter);
            }
        }

        private bool MatchesAllFilters(object item)
        {
            foreach (var filter in _filters.Values)
            {
                var value = GetPropertyValue(item, filter.ColumnKey);

                if (filter.HasFilter && !filter.SelectedValues.Contains(value))
                    return false;
            }

            return true;
        }

        private IEnumerable<string> GetDistinctValues(string propertyName)
        {
            if (ItemsSource is not IEnumerable items)
                yield break;

            var seen = new HashSet<string>();

            foreach (var item in items)
            {
                var value = GetPropertyValue(item, propertyName);

                if (seen.Add(value))
                    yield return value;
            }
        }

        private string GetPropertyValue(object item, string propertyName)
        {
            PropertyInfo? prop = item.GetType().GetProperty(propertyName);
            var raw = prop?.GetValue(item);

            return raw?.ToString() ?? "(Blanks)";
        }

        private void CloseCurrentPopup()
        {
            if (_currentFilterPopup != null)
            {
                _currentFilterPopup.IsOpen = false;
                _currentFilterPopup = null;
            }
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject? current = child;

            while (current != null)
            {
                if (current is T match)
                    return match;

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}