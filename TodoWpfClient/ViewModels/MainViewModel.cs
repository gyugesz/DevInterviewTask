using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using TodoWpfClient.Models;
using TodoWpfClient.Services;

namespace TodoWpfClient.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly TodoApiClient _api;

        public ObservableCollection<TodoItem> Items { get; } = new();
        public ListCollectionView VisibleItems { get; }

        private string _baseUrl = "http://localhost:5055";
        public string BaseUrl
        {
            get => _baseUrl;
            set { _baseUrl = value; OnPropertyChanged(); }
        }

        private string _newTitle = string.Empty;
        public string NewTitle
        {
            get => _newTitle;
            set { _newTitle = value; OnPropertyChanged(); ((RelayCommand)AddCommand).RaiseCanExecuteChanged(); }
        }

        private string _query = string.Empty;
        public string Query
        {
            get => _query;
            set { _query = value; OnPropertyChanged(); VisibleItems.Refresh(); }
        }

        private string _sortKey = "Cím (A→Z)";
        public string SortKey
        {
            get => _sortKey;
            set { _sortKey = value; OnPropertyChanged(); ApplySort(); }
        }

        private string _status = "Készen áll.";
        public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand CompleteCommand { get; }
        public ICommand SaveBaseUrlCommand { get; }

        public MainViewModel()
        {
            // Load saved base URL if present
            try
            {
                var saved = Properties.Settings.Default.BaseUrl;
                if (!string.IsNullOrWhiteSpace(saved)) _baseUrl = saved;
            }
            catch { }

            _api = new TodoApiClient(BaseUrl);

            VisibleItems = (ListCollectionView)CollectionViewSource.GetDefaultView(Items);
            VisibleItems.Filter = item =>
            {
                if (item is not TodoItem t) return false;
                if (t.IsCompleted) return false; // only incomplete
                if (!string.IsNullOrWhiteSpace(Query))
                    return (t.Title ?? string.Empty).IndexOf(Query, StringComparison.OrdinalIgnoreCase) >= 0;
                return true;
            };

            RefreshCommand = new RelayCommand(async _ => await RefreshAsync());
            AddCommand = new RelayCommand(async _ => await AddAsync(), _ => !string.IsNullOrWhiteSpace(NewTitle));
            CompleteCommand = new RelayCommand(async t => await CompleteAsync(t as TodoItem));
            SaveBaseUrlCommand = new RelayCommand(_ => SaveBaseUrl());

            _ = RefreshAsync();
        }

        private void SaveBaseUrl()
        {
            try
            {
                Properties.Settings.Default.BaseUrl = BaseUrl;
                Properties.Settings.Default.Save();
                _api.UpdateBaseUrl(BaseUrl);
                Status = "API URL mentve.";
            }
            catch
            {
                Status = "Nem sikerült elmenteni az API URL-t.";
            }
        }
        private async Task RefreshAsync()
        {
            try
            {
                Status = "Betöltés...";
                Items.Clear();
                _api.UpdateBaseUrl(BaseUrl);
                var list = await _api.GetTodosAsync();
                foreach (var t in list.OrderBy(t => t.Title))
                    Items.Add(t);
                ApplySort();
                VisibleItems.Refresh();
                Status = $"Betöltve: {Items.Count} db (csak az elvégzetlenek láthatók).";
            }
            catch (Exception ex)
            {
                Status = "Hiba: " + ex.Message;
                MessageBox.Show(ex.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string _newDescription = string.Empty;
        public string NewDescription
        {
            get => _newDescription;
            set { _newDescription = value; OnPropertyChanged(); }
        }

        private int _newPriority = 1;    // 1=Alacsony, 2=Közepes, 3=Magas
        public int NewPriority
        {
            get => _newPriority;
            set { _newPriority = value; OnPropertyChanged(); }
        }

        private async Task AddAsync()
        {
            try
            {
                var title = NewTitle?.Trim();
                if (string.IsNullOrWhiteSpace(title)) return;

                Status = "Hozzáadás...";
                var created = await _api.CreateTodoAsync(title, NewDescription?.Trim(),
                                                         NewPriority <= 0 ? 1 : NewPriority);
                // Optimista beszúrás
                if (string.IsNullOrWhiteSpace(created.Title)) created.Title = title;
                if (string.IsNullOrWhiteSpace(created.Description)) created.Description = NewDescription;
                if (created.Priority <= 0) created.Priority = NewPriority;

                Items.Insert(0, created);

                // mezők nullázása
                NewTitle = string.Empty;
                NewDescription = string.Empty;
                NewPriority = 1;

                ApplySort();
                VisibleItems.Refresh();
                Status = "Hozzáadva.";
            }
            catch (Exception ex)
            {
                Status = "Hiba: " + ex.Message;
                MessageBox.Show(ex.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CompleteAsync(TodoItem? item)
        {
            if (item is null) return;
            try
            {
                Status = "Készre jelölés...";
                var ok = await _api.CompleteTodoAsync(item);
                if (ok)
                {
                    item.IsCompleted = true;
                    VisibleItems.Refresh(); // disappears due to filter
                    Status = "Készre jelölve.";
                }
                else
                {
                    Status = "Nem sikerült készre jelölni (API válasz hibás).";
                }
            }
            catch (Exception ex)
            {
                Status = "Hiba: " + ex.Message;
                MessageBox.Show(ex.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplySort()
        {
            VisibleItems.SortDescriptions.Clear();
            switch (SortKey)
            {
                case "Cím (A→Z)":
                    VisibleItems.SortDescriptions.Add(new SortDescription(nameof(TodoItem.Title), ListSortDirection.Ascending));
                    break;
                case "Cím (Z→A)":
                    VisibleItems.SortDescriptions.Add(new SortDescription(nameof(TodoItem.Title), ListSortDirection.Descending));
                    break;
                case "Létrehozás (új elöl)":
                    VisibleItems.SortDescriptions.Add(new SortDescription(nameof(TodoItem.CreatedAt), ListSortDirection.Descending));
                    break;
                case "Létrehozás (régi elöl)":
                    VisibleItems.SortDescriptions.Add(new SortDescription(nameof(TodoItem.CreatedAt), ListSortDirection.Ascending));
                    break;
                default:
                    VisibleItems.SortDescriptions.Add(new SortDescription(nameof(TodoItem.Title), ListSortDirection.Ascending));
                    break;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
