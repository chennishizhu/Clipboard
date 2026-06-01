using ClipVault.Models;

namespace ClipVault.ViewModels;

public class ClipItemViewModel(ClipItem item) : ViewModelBase
{
    public ClipItem Model { get; } = item;

    public long Id => Model.Id;
    public string Content => Model.Content;
    public string Type => Model.Type;
    public string TypeLabel => Model.Type == "link" ? "链接" : "文本";
    public string RelativeTime => Model.RelativeTime;
    public string Preview => Model.Preview;

    public bool IsFavorite
    {
        get => Model.IsFavorite;
        set { Model.IsFavorite = value; OnPropertyChanged(); }
    }

    public bool IsPinned
    {
        get => Model.IsPinned;
        set { Model.IsPinned = value; OnPropertyChanged(); }
    }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetField(ref _isExpanded, value);
    }
}
