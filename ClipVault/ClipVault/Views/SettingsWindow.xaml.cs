using System.IO;
using System.Windows;
using ClipVault.ViewModels;
using Microsoft.Win32;

namespace ClipVault.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm;

    public SettingsWindow()
    {
        InitializeComponent();
        _vm = new SettingsViewModel(App.Settings);
        DataContext = _vm;
    }

    private void BrowseDb_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "选择数据库文件夹",
            Filter = "SQLite 数据库|*.db|所有文件|*.*",
            FileName = Path.GetFileName(_vm.DatabasePath),
            InitialDirectory = Path.GetDirectoryName(_vm.DatabasePath)
        };
        if (dlg.ShowDialog() == true)
            _vm.DatabasePath = dlg.FileName;
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Title = "导出数据",
            Filter = "JSON 文件|*.json",
            FileName = $"clipvault_export_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            App.Database.ExportToJson(dlg.FileName);
            MessageBox.Show("导出成功！", "ClipVault", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败：{ex.Message}", "ClipVault", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "导入数据",
            Filter = "JSON 文件|*.json"
        };
        if (dlg.ShowDialog() != true) return;

        var result = MessageBox.Show(
            "导入方式：\n\n是 — 合并（保留现有数据，追加导入内容）\n否 — 覆盖（清空现有数据后导入）\n取消 — 放弃",
            "选择导入方式",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Cancel) return;

        try
        {
            App.Database.ImportFromJson(dlg.FileName, merge: result == MessageBoxResult.Yes);
            App.MainVM?.Refresh();
            MessageBox.Show("导入成功！", "ClipVault", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导入失败：{ex.Message}", "ClipVault", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "确定要清空所有记录吗？\n\n点击\u300c是\u300d同时清除收藏记录，点击\u300c否\u300d仅清除普通记录。",
            "清空记录",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Cancel) return;

        App.Database.ClearAll(includeFavorites: result == MessageBoxResult.Yes);
        App.MainVM?.Refresh();
        MessageBox.Show("已清空。", "ClipVault", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var oldPath = App.Settings.Settings.DatabasePath;
        var newPath = _vm.DatabasePath;

        if (!string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
        {
            var result = MessageBox.Show(
                "数据库路径已更改。\n\n是 — 将现有数据迁移到新位置\n否 — 在新位置创建空数据库（旧数据仍保留在原路径）\n取消 — 放弃",
                "数据库路径变更",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel) return;

            try
            {
                if (result == MessageBoxResult.Yes)
                {
                    App.Database.MigrateDatabase(newPath);
                    MessageBox.Show($"迁移成功。旧文件仍保留在：\n{oldPath}", "ClipVault",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    App.Database.Initialize(newPath);
                    MessageBox.Show($"已在新位置创建空数据库。\n旧数据仍保留在：\n{oldPath}", "ClipVault",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败：{ex.Message}", "ClipVault", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        // Re-register hotkey if changed
        var hotkeyChanged = _vm.GlobalHotkey != App.Settings.Settings.GlobalHotkey
                            || _vm.GlobalHotkeyEnabled != App.Settings.Settings.GlobalHotkeyEnabled;

        _vm.SaveToSettings();

        if (hotkeyChanged)
        {
            App.Hotkey.Unregister();
            if (App.Settings.Settings.GlobalHotkeyEnabled)
            {
                bool ok = App.Hotkey.Register(App.Settings.Settings.GlobalHotkey);
                if (!ok)
                    MessageBox.Show("快捷键注册失败，可能与其他程序冲突，已恢复默认。", "ClipVault",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
