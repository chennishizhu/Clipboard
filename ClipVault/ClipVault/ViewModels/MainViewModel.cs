using System.Collections.ObjectModel;
using System.Windows.Input;
using ClipVault.Models;
using ClipVault.Services;

namespace ClipVault.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    private readonly ClipboardService _clipboard;

    public MainViewModel(DatabaseService db, ClipboardService clipboard)
    {
        _db = db;
        _clipboard = clipboard;

        SearchCommand = new RelayCommand(Refresh);
        ClearSearchCommand = new RelayCommand(() => { SearchText = string.Empty; Refresh(); });
        CopyItemCommand = new RelayCommand<ClipItemViewModel>(CopyItem);
        ToggleFavoriteCommand = new RelayCommand<ClipItemViewModel>(ToggleFavorite);
        TogglePinnedCommand = new RelayCommand<ClipItemViewModel>(TogglePinned);
        DeleteItemCommand = new RelayCommand<ClipItemViewModel>(DeleteItem);
        ToggleExpandCommand = new RelayCommand<ClipItemViewModel>(ToggleExpand);

        Refresh();
    }

    // ── Filter state ──────────────────────────────────────────────

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) Refresh(); }
    }

    private string _timeFilter = "all";
    public string TimeFilter
    {
        get => _timeFilter;
        set { if (SetField(ref _timeFilter, value)) Refresh(); }
    }

    private string _typeFilter = "all";
    public string TypeFilter
    {
        get => _typeFilter;
        set { if (SetField(ref _typeFilter, value)) Refresh(); }
    }

    // ── Data ──────────────────────────────────────────────────────

    public ObservableCollection<ClipItemViewModel> Items { get; } = [];

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        private set => SetField(ref _totalCount, value);
    }

    private bool _isFiltered;
    public bool IsFiltered
    {
        get => _isFiltered;
        private set => SetField(ref _isFiltered, value);
    }

    public string StatusText => IsFiltered
        ? $"显示 {Items.Count} 条 / 共 {TotalCount} 条"
        : $"共 {TotalCount} 条";

    public bool HasNoItems => Items.Count == 0;
    public string EmptyMessage => string.IsNullOrWhiteSpace(SearchText) && TimeFilter == "all" && TypeFilter == "all"
        ? "暂无记录"
        : "未找到匹配内容";

    // ── Commands ──────────────────────────────────────────────────

    public ICommand SearchCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand CopyItemCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand TogglePinnedCommand { get; }
    public ICommand DeleteItemCommand { get; }
    public ICommand ToggleExpandCommand { get; }

    // ── Actions ───────────────────────────────────────────────────

    public void Refresh()
    {
        var kw = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText;
        var time = TimeFilter == "all" ? null : TimeFilter;
        var type = TypeFilter == "all" ? null : TypeFilter;

        var rows = _db.Query(kw, time, type);
        TotalCount = _db.GetTotalCount();
        IsFiltered = kw != null || time != null || type != null;

        Items.Clear();
        foreach (var r in rows)
            Items.Add(new ClipItemViewModel(r));

        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(HasNoItems));
        OnPropertyChanged(nameof(EmptyMessage));
    }

    public void AddNewItem(string content, string type)
    {
        var item = new ClipItem
        {
            Content = content,
            Type = type,
            CreatedAt = DateTime.Now
        };
        _db.Insert(item);
        Refresh();
    }

    private void CopyItem(ClipItemViewModel? vm)
    {
        if (vm == null) return;
        _clipboard.CopyWithoutRecording(vm.Content);
    }

    private void ToggleFavorite(ClipItemViewModel? vm)
    {
        if (vm == null) return;
        vm.IsFavorite = !vm.IsFavorite;
        _db.UpdateFavorite(vm.Id, vm.IsFavorite);
    }

    private void TogglePinned(ClipItemViewModel? vm)
    {
        if (vm == null) return;
        vm.IsPinned = !vm.IsPinned;
        _db.UpdatePinned(vm.Id, vm.IsPinned);
        Refresh();
    }

    private void DeleteItem(ClipItemViewModel? vm)
    {
        if (vm == null) return;
        _db.Delete(vm.Id);
        Items.Remove(vm);
        TotalCount = _db.GetTotalCount();
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(HasNoItems));
        OnPropertyChanged(nameof(EmptyMessage));
    }

    private void ToggleExpand(ClipItemViewModel? vm)
    {
        if (vm == null) return;
        // Collapse all others first
        foreach (var item in Items)
            if (item != vm) item.IsExpanded = false;
        vm.IsExpanded = !vm.IsExpanded;
    }
}
