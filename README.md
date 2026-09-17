# WinCleaner

[![Build Status](https://img.shields.io/github/actions/workflow/status/minhtuancn/WinCleaner/ci.yml?branch=main&logo=github-actions)](https://github.com/minhtuancn/WinCleaner/actions)
[![Release](https://img.shields.io/github/v/release/minhtuancn/WinCleaner?include_prereleases&logo=github)](https://github.com/minhtuancn/WinCleaner/releases)
[![License](https://img.shields.io/github/license/minhtuancn/WinCleaner)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-blueviolet?logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D4?logo=windows)](https://www.microsoft.com/windows)
[![Language](https://img.shields.io/badge/Language-Vietnamese-red?logo=google-translate)](https://github.com/minhtuancn/WinCleaner)

**Professional System Cleaner for Windows** - Dọn dẹp hệ thống chuyên nghiệp, đa ổ đĩa, đa người dùng, đa phiên bản Windows.

![WinCleaner Screenshot](docs/images/screenshot-main.png)

## 🎯 Tính năng chính

### 🧹 4 Profile dọn dẹp thông minh
| Profile | Mô tả | Mức độ an toàn |
|---------|-------|----------------|
| **Safe (An toàn)** | System temp, Windows Update, Browser cache, Dev tools, User temp, Recycle bin | ✅ 100% an toàn |
| **Deep (Sâu)** | + DriverStore, Discord old versions, Playwright, Minecraft temp, Updater caches | ⚠️ Cẩn thận |
| **Custom (Tùy chỉnh)** | + Game data (Roblox, Minecraft), User Programs, Other user profiles | ⚠️ Chọn lọc |
| **Nuclear (Cực đại)** | + CompactOS, Hibernation, System Restore, Windows.old | 🔴 Chuyên gia |

### 🖥️ Hỗ trợ đa nền tảng
- **Windows 10** (Build 10240+) / **Windows 11** (Build 22000+)
- **x64** / **ARM64** native
- **Multi-drive**: Auto-detect tất cả ổ đĩa (C:, D:, E:, ...)
- **Multi-user**: Quét và dọn cho từng tài khoản người dùng
- **Per-user paths**: `%LOCALAPPDATA%`, `%TEMP%`, `%APPDATA%` riêng biệt

### 🎨 Giao diện chuyên nghiệp
- **Modern WPF UI** với Fluent Design
- **Real-time console log** màu sắc (Debug/Info/Warning/Error/Success)
- **Progress tracking** chi tiết từng mục
- **Risk level badges** (Safe/Low/Medium/High/Critical)
- **Dry-run mode** - Mô phỏng trước khi xóa thực tế
- **Dark/Light theme** tự động theo hệ thống

### 🔧 Hỗ trợ 50+ loại rác
- **Hệ thống**: Windows Temp, Prefetch, SoftwareDistribution, Logs, DriverStore
- **Trình duyệt**: Chrome, Edge, Firefox, Brave, Opera, Vivaldi
- **Dev Tools**: npm, Yarn, uv, pip, NuGet, Go, Flutter, .NET, Gradle, Maven, Cargo, Playwright, Cypress
- **Apps**: Discord, Electron, VS Code, Cursor, Codex, Copilot, Roblox, Minecraft, Zalo, Steam
- **Game**: Roblox, RobloxPCGDK, Lunar Client, CurseForge, Steam
- **System**: Recycle Bin, CompactOS, Hibernation, System Restore, Windows.old

## 📥 Cài đặt

### Yêu cầu
- Windows 10 version 1809+ / Windows 11
- .NET 8.0 Runtime (tự động cài nếu dùng self-contained)

### Tải về
```bash
# Cách 1: Tải bản release mới nhất
gh release download -R minhtuancn/WinCleaner

# Cách 2: Clone và build
git clone https://github.com/minhtuancn/WinCleaner.git
cd WinCleaner/WinCleaner
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Cài đặt nhanh (PowerShell Admin)
```powershell
# Tải và chạy bản portable
irm https://github.com/minhtuancn/WinCleaner/releases/latest/download/WinCleaner.exe -OutFile WinCleaner.exe
./WinCleaner.exe
```

## 🚀 Sử dụng

### Chạy ứng dụng
```bash
# Debug mode
cd WinCleaner/WinCleaner
dotnet run

# Release build
dotnet publish -c Release -r win-x64 --self-contained true
.\bin\Release\net8.0-windows\win-x64\publish\WinCleaner.exe
```

### Quy trình dọn dẹp
1. **Chọn Profile** → Safe (khuyến nghị) / Deep / Custom / Nuclear
2. **Bật Dry-run** → Xem trước những gì sẽ bị xóa (không xóa thực tế)
3. **Nhấn Quét** → Phân tích toàn bộ hệ thống
4. **Review kết quả** → Bỏ chọn mục không muốn xóa
5. **Nhấn Dọn dẹp** → Xác nhận và thực thi
6. **Xem Console Log** → Theo dõi tiến trình real-time

### Keyboard Shortcuts
| Phím | Chức năng |
|------|-----------|
| `F5` | Quét lại |
| `Ctrl+A` | Chọn tất cả |
| `Ctrl+D` | Bỏ chọn tất cả |
| `Ctrl+S` | Chỉ chọn mục an toàn |
| `Ctrl+E` | Xuất log ra file |
| `Esc` | Hủy tác vụ đang chạy |

## 🏗️ Kiến trúc

```
WinCleaner/
├── WinCleaner/                 # Main WPF Application
│   ├── Models/                 # Data models & Converters
│   │   └── CleanModels.cs      # CleanItem, Category, Profile, Drive, User, LogEntry
│   ├── ViewModels/             # MVVM ViewModels
│   │   ├── BaseViewModel.cs    # Base với logging, progress
│   │   └── MainViewModel.cs    # Main logic: scan, clean, profiles
│   ├── Views/                  # XAML Views
│   │   ├── MainWindow.xaml     # Main UI
│   │   └── MainWindow.xaml.cs  # Code-behind
│   ├── Services/               # Business Logic
│   │   ├── ISystemScanner.cs   # Scanner interface
│   │   ├── SystemScanner.cs    # Multi-drive/user/OS scanner
│   │   ├── ICleanerService.cs  # Cleaner interface
│   │   ├── CleanerService.cs   # Execute cleaning actions
│   │   ├── ISettingsService.cs # Settings persistence
│   │   └── SettingsService.cs  # JSON settings storage
│   ├── Resources/              # Styles & Themes
│   │   └── Styles.xaml         # Fluent Design styles
│   ├── Helpers/                # Extension methods
│   ├── App.xaml/.cs            # DI Container, Host builder
│   └── app.manifest            # DPI awareness, OS compatibility
├── docs/                       # Documentation
│   ├── images/                 # Screenshots
│   ├── analysis-report.md      # Detailed disk analysis
│   └── architecture.md         # Technical architecture
├── .github/
│   ├── workflows/              # CI/CD pipelines
│   └── ISSUE_TEMPLATE/         # Issue templates
├── LICENSE                     # MIT License
├── CONTRIBUTING.md             # Contribution guide
├── CHANGELOG.md                # Version history
├── SECURITY.md                 # Security policy
├── CODE_OF_CONDUCT.md          # Community guidelines
└── WinCleaner.sln              # Solution file
```

## 🛠️ Phát triển

### Requirements
- Visual Studio 2022 / VS Code + C# Dev Kit
- .NET 8.0 SDK
- Windows 10/11 SDK

### Build
```bash
# Restore & Build
dotnet restore
dotnet build -c Release

# Run tests
dotnet test

# Package
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Code Style
- **C# 12** / .NET 8
- **Nullable reference types** enabled
- **MVVM** với CommunityToolkit.Mvvm
- **DI** với Microsoft.Extensions.Hosting
- **Logging** với Microsoft.Extensions.Logging

## 📊 Phân tích ổ đĩa mẫu

Kết quả quét trên máy phát triển (Windows 11 Pro 26200):

| Đối tượng | Kích thước | Phân loại |
|-----------|------------|-----------|
| `uv` cache | **10.04 GB** | 🔴 Critical - Python packages |
| `Yarn` cache | **5.06 GB** | 🔴 Critical - Node.js packages |
| `Programs` (user) | **5.95 GB** | 🟡 Review needed |
| `DriverStore` | **5.12 GB** | 🟡 Old drivers |
| `Roblox` + PCGDK | **5.39 GB** | 🟡 Game data |
| `Chrome` cache | **2.58 GB** | 🟢 Safe |
| `npm` cache | **1.42 GB** | 🟢 Safe |
| `Playwright` | **1.15 GB** | 🟢 Safe |
| `Firefox` cache | **1.28 GB** | 🟢 Safe |
| Windows Update | **255 MB** | 🟢 Safe |
| System Logs | **118 MB** | 🟢 Safe |

**Dự kiến giải phóng:** 18-30 GB tùy profile

## 🤝 Đóng góp

Mọi đóng góp đều được chào đón! Xem [CONTRIBUTING.md](CONTRIBUTING.md) để biết chi tiết.

### Cách đóng góp
1. Fork repository
2. Tạo feature branch: `git checkout -b feature/amazing-feature`
3. Commit changes: `git commit -m 'Add amazing feature'`
4. Push branch: `git push origin feature/amazing-feature`
5. Tạo Pull Request

### Báo cáo lỗi
Sử dụng [Issue Templates](.github/ISSUE_TEMPLATE/) cho:
- 🐛 Bug Report
- ✨ Feature Request
- 📚 Documentation
- ❓ Question

## 📄 Giấy phép

Distributed under the MIT License. See [LICENSE](LICENSE) for more information.

## 👨‍💻 Tác giả

**Minh Tuấn** - *Full Stack Developer*
- GitHub: [@minhtuancn](https://github.com/minhtuancn)
- Repository: [WinCleaner](https://github.com/minhtuancn/WinCleaner)

## 🙏 Acknowledgments

- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - MVVM framework
- [Microsoft.Extensions.Hosting](https://github.com/dotnet/runtime) - DI & Hosting
- [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon) - System tray
- [Fluent Design System](https://developer.microsoft.com/fluentui/) - UI inspiration

---

⭐ **Star repo này nếu bạn thấy hữu ích!** ⭐