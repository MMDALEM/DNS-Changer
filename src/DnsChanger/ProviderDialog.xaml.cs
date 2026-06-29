using System.Net;
using System.Windows;
using DnsChanger.Models;

namespace DnsChanger;

public partial class ProviderDialog : Window
{
    public DnsProvider? Result { get; private set; }

    public ProviderDialog()
    {
        InitializeComponent();
        HeaderText.Text = "Add DNS";
        NameBox.Focus();
    }

    public ProviderDialog(DnsProvider existing) : this()
    {
        HeaderText.Text = "Edit DNS";
        NameBox.Text = existing.Name;
        PrimaryBox.Text = existing.Primary;
        SecondaryBox.Text = existing.Secondary;
        NoteBox.Text = existing.Note;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        var primary = PrimaryBox.Text.Trim();
        var secondary = SecondaryBox.Text.Trim();
        var note = NoteBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("Please enter a name.");
            return;
        }
        if (!IsValidIp(primary))
        {
            ShowError("Primary DNS must be a valid IPv4/IPv6 address.");
            return;
        }
        if (!string.IsNullOrWhiteSpace(secondary) && !IsValidIp(secondary))
        {
            ShowError("Secondary DNS is not a valid address.");
            return;
        }

        Result = new DnsProvider
        {
            Name = name,
            Primary = primary,
            Secondary = secondary,
            Note = note,
            IsCustom = true,
        };
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private static bool IsValidIp(string value) =>
        IPAddress.TryParse(value, out _);

    private void ShowError(string message) => ErrorText.Text = message;
}
