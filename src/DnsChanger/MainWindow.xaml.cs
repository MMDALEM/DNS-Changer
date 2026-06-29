using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DnsChanger.Models;
using DnsChanger.Services;

namespace DnsChanger;

public partial class MainWindow : Window
{
    private readonly List<DnsProvider> _allProviders;
    private readonly ObservableCollection<DnsProvider> _view = new();

    public MainWindow()
    {
        InitializeComponent();

        _allProviders = StorageService.Load();
        ProviderList.ItemsSource = _view;
        RefreshView();

        LoadAdapters();
        UpdateDetail(null);
        UpdateActionButtons();
    }

    // ---------------- Adapters ----------------

    private void LoadAdapters()
    {
        var selectedName = (AdapterBox.SelectedItem as NetworkAdapter)?.Name;
        var adapters = DnsService.GetActiveAdapters();
        AdapterBox.ItemsSource = adapters;

        if (adapters.Count == 0)
        {
            SetStatus("No active network adapters found.", StatusKind.Warning);
            return;
        }

        var restore = adapters.FirstOrDefault(a => a.Name == selectedName);
        AdapterBox.SelectedItem = restore ?? adapters[0];
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadAdapters();
        SetStatus("Adapters refreshed.", StatusKind.Info);
    }

    private void AdapterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AdapterBox.SelectedItem is NetworkAdapter a)
            SetStatus($"Current DNS on \"{a.Name}\": {a.CurrentDns}", StatusKind.Info);
    }

    // ---------------- Provider list ----------------

    private void RefreshView()
    {
        var query = SearchBox?.Text?.Trim() ?? string.Empty;
        var selected = ProviderList.SelectedItem as DnsProvider;

        _view.Clear();
        foreach (var p in _allProviders)
        {
            if (query.Length == 0 ||
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Primary.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Secondary.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                _view.Add(p);
            }
        }

        if (selected != null && _view.Contains(selected))
            ProviderList.SelectedItem = selected;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshView();

    private void ProviderList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateDetail(ProviderList.SelectedItem as DnsProvider);
        UpdateActionButtons();
    }

    private void UpdateDetail(DnsProvider? p)
    {
        SelName.Text = p?.Name ?? "—";
        SelNote.Text = p?.Note ?? string.Empty;
        SelPrimary.Text = string.IsNullOrWhiteSpace(p?.Primary) ? "—" : p!.Primary;
        SelSecondary.Text = string.IsNullOrWhiteSpace(p?.Secondary) ? "—" : p!.Secondary;
        ApplyBtn.IsEnabled = p != null && AdapterBox.SelectedItem != null;
    }

    private void UpdateActionButtons()
    {
        bool isCustom = ProviderList.SelectedItem is DnsProvider { IsCustom: true };
        EditBtn.IsEnabled = isCustom;
        DeleteBtn.IsEnabled = isCustom;
    }

    // ---------------- Add / Edit / Delete ----------------

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ProviderDialog { Owner = this };
        if (dlg.ShowDialog() == true && dlg.Result != null)
        {
            dlg.Result.IsCustom = true;
            _allProviders.Add(dlg.Result);
            StorageService.Save(_allProviders);
            RefreshView();
            ProviderList.SelectedItem = dlg.Result;
            SetStatus($"Added \"{dlg.Result.Name}\".", StatusKind.Success);
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (ProviderList.SelectedItem is not DnsProvider { IsCustom: true } current)
            return;

        var dlg = new ProviderDialog(current) { Owner = this };
        if (dlg.ShowDialog() == true && dlg.Result != null)
        {
            current.Name = dlg.Result.Name;
            current.Primary = dlg.Result.Primary;
            current.Secondary = dlg.Result.Secondary;
            current.Note = dlg.Result.Note;
            StorageService.Save(_allProviders);
            RefreshView();
            ProviderList.SelectedItem = current;
            UpdateDetail(current);
            SetStatus($"Updated \"{current.Name}\".", StatusKind.Success);
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (ProviderList.SelectedItem is not DnsProvider { IsCustom: true } current)
            return;

        var confirm = MessageBox.Show(
            $"Delete \"{current.Name}\"?", "DNS Changer",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm == MessageBoxResult.Yes)
        {
            _allProviders.Remove(current);
            StorageService.Save(_allProviders);
            RefreshView();
            UpdateDetail(ProviderList.SelectedItem as DnsProvider);
            UpdateActionButtons();
            SetStatus($"Deleted \"{current.Name}\".", StatusKind.Info);
        }
    }

    // ---------------- Apply / Clear ----------------

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (AdapterBox.SelectedItem is not NetworkAdapter adapter)
        {
            SetStatus("Select a network adapter first.", StatusKind.Warning);
            return;
        }
        if (ProviderList.SelectedItem is not DnsProvider p)
        {
            SetStatus("Select a DNS provider first.", StatusKind.Warning);
            return;
        }

        SetStatus($"Applying {p.Name}…", StatusKind.Info);
        var result = DnsService.SetDns(adapter.Name, p.Primary, p.Secondary);
        SetStatus(result.Message, result.Ok ? StatusKind.Success : StatusKind.Error);
        LoadAdapters();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (AdapterBox.SelectedItem is not NetworkAdapter adapter)
        {
            SetStatus("Select a network adapter first.", StatusKind.Warning);
            return;
        }

        SetStatus("Resetting DNS…", StatusKind.Info);
        var result = DnsService.ClearDns(adapter.Name);
        SetStatus(result.Message, result.Ok ? StatusKind.Success : StatusKind.Error);
        LoadAdapters();
    }

    // ---------------- Status ----------------

    private enum StatusKind { Info, Success, Warning, Error }

    private void SetStatus(string message, StatusKind kind)
    {
        StatusText.Text = message;
        StatusDot.Fill = kind switch
        {
            StatusKind.Success => (Brush)FindResource("Success"),
            StatusKind.Error => (Brush)FindResource("Danger"),
            StatusKind.Warning => Brushes.Goldenrod,
            _ => (Brush)FindResource("Accent"),
        };
    }
}
