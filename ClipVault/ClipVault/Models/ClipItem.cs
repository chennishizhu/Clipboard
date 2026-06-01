namespace ClipVault.Models;

public class ClipItem
{
    public long Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "text"; // "text" | "link"
    public DateTime CreatedAt { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsPinned { get; set; }

    public string RelativeTime
    {
        get
        {
            var diff = DateTime.Now - CreatedAt;
            if (diff.TotalMinutes < 1) return "刚刚";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}分钟前";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}小时前";
            if (diff.TotalDays < 2) return "昨天";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}天前";
            return CreatedAt.ToString("MM-dd");
        }
    }

    public string Preview => Content.Length > 120 ? Content[..120] : Content;
}
