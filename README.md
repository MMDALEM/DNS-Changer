# DNS Changer

A simple, modern **DNS changer for Windows**. Pick a DNS provider, choose your
network adapter, and switch with a single click — or reset back to automatic.

Built with **C# / WPF (.NET 8)**. No browser, no bloat — a small native app.

---

## ✨ Features

- **One-click apply** — set DNS on the selected adapter instantly.
- **One-click reset** — return any adapter to automatic (DHCP) DNS.
- **Add unlimited custom DNS** entries (name, primary, secondary, note).
- **Edit / delete** your own entries; built-in presets stay protected.
- **Search** across names and IP addresses.
- **Lots of well-known providers built in** (see below).
- **Per-adapter** control with live "current DNS" display.
- Clean dark UI, custom entries persist in `%AppData%\DnsChanger\providers.json`.

## 📦 Built-in providers

**International:** Cloudflare (+ Malware / Family), Google Public DNS, Quad9,
OpenDNS (+ Family Shield), AdGuard (+ Family), CleanBrowsing, Comodo Secure,
Level3, DNS.WATCH, Yandex, Verisign.

**Iran (anti-sanction / local):** Shecan, 403.online, Electro, Begzar,
RadarGame, Shelter, Pishgaman.

You can add any others you like from the app.

## 🚀 Getting started

### Download
Grab the prebuilt `DnsChanger.exe` from the **Actions → Build** artifacts (or a
release if published). It is a single self-contained file — no .NET install
needed.

> The app **requires administrator rights** (changing DNS does), so Windows will
> prompt for elevation when you launch it.

### Build from source
Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download) on Windows.

```bash
# Run it
dotnet run --project src/DnsChanger

# Or produce a single-file exe
dotnet publish src/DnsChanger/DnsChanger.csproj -c Release -r win-x64 ^
  --self-contained true -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

The exe will be at `publish/DnsChanger.exe`.

## 🧭 How to use

1. Launch the app (accept the admin prompt).
2. Choose your **network adapter** at the top right (e.g. Wi-Fi or Ethernet).
3. Pick a **DNS** from the list on the left.
4. Click **Apply DNS**. To undo, click **Reset to Automatic**.
5. To add your own, click **＋ Add** and fill in the address(es).

## 🛠 How it works

DNS changes are applied with Windows' built-in `netsh` command on the selected
adapter, followed by a DNS cache flush (`ipconfig /flushdns`). Nothing is sent
anywhere — it only configures your local network settings.

## 📂 Project structure

```
src/DnsChanger/
├─ Models/DnsProvider.cs       # the DNS entry model
├─ Services/DnsService.cs      # reads/sets adapter DNS via netsh
├─ Services/StorageService.cs  # presets + persistence of custom entries
├─ MainWindow.xaml(.cs)        # main UI
├─ ProviderDialog.xaml(.cs)    # add/edit dialog
├─ App.xaml(.cs)               # theme & styles
└─ app.manifest               # requests administrator elevation
```

## 📄 License

MIT — see [LICENSE](LICENSE).
