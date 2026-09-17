# Hướng dẫn đóng góp

Cảm ơn bạn đã quan tâm đóng góp cho **WinCleaner**! Mọi đóng góp đều được chào đón, từ sửa lỗi typo đến tính năng mới.

## 📋 Mục lục
- [Code of Conduct](#code-of-conduct)
- [Cách báo cáo lỗi](#cách-báo-cáo-lỗi)
- [Đề xuất tính năng](#đề-xuất-tính-năng)
- [Quy trình Pull Request](#quy-trình-pull-request)
- [Coding Standards](#coding-standards)
- [Testing](#testing)
- [Documentation](#documentation)

## Code of Conduct
Dự án này tuân thủ [Code of Conduct](CODE_OF_CONDUCT.md). Bằng cách tham gia, bạn đồng ý tuân thủ các quy tắc này.

## Cách báo cáo lỗi

### Trước khi báo cáo
1. Kiểm tra [Issues](https://github.com/minhtuancn/WinCleaner/issues) xem lỗi đã được báo cáo chưa
2. Cập nhật lên phiên bản mới nhất
3. Chạy với Dry-run mode để xác nhận

### Template Bug Report
Sử dụng [Bug Report Template](.github/ISSUE_TEMPLATE/bug_report.yml) bao gồm:
- **Mô tả lỗi**: Rõ ràng, ngắn gọn
- **Bước tái hiện**: Step-by-step
- **Kết quả mong đợi vs thực tế**
- **Screenshots/Logs**: Console log error
- **Environment**: Windows version, .NET version, WinCleaner version
- **Profile đang dùng**: Safe/Deep/Custom/Nuclear

## Đề xuất tính năng
Sử dụng [Feature Request Template](.github/ISSUE_TEMPLATE/feature_request.yml):
- **Vấn đề**: Tính năng giải quyết vấn đề gì?
- **Giải pháp đề xuất**: Mô tả chi tiết
- **Alternatives**: Các giải pháp khác đã xem xét
- **Use cases**: Ai sẽ sử dụng, khi nào

## Quy trình Pull Request

### 1. Fork & Clone
```bash
git clone https://github.com/YOUR_USERNAME/WinCleaner.git
cd WinCleaner
git remote add upstream https://github.com/minhtuancn/WinCleaner.git
```

### 2. Tạo Branch
```bash
# Feature
git checkout -b feature/ten-tinh-nang

# Bug fix
git checkout -b fix/ten-loi

# Docs
git checkout -b docs/ten-cap-nhat
```

### 3. Phát triển
```bash
# Build & test
dotnet build -c Release
dotnet test

# Chạy app
cd WinCleaner/WinCleaner
dotnet run
```

### 4. Commit Convention
Sử dụng [Conventional Commits](https://www.conventionalcommits.org/):

```bash
# Format
<type>(<scope>): <mô tả>

# Types
feat:     Tính năng mới
fix:      Sửa lỗi
docs:     Chỉ cập nhật docs
style:    Format code (không thay đổi logic)
refactor: Refactor code
perf:     Cải thiện performance
test:     Thêm/sửa test
chore:    Build, deps, tooling
ci:       CI/CD changes

# Examples
feat(scanner): add support for Bun cache
fix(cleaner): handle read-only files correctly
docs(readme): update installation guide
refactor(models): extract converters to separate file
```

### 5. Push & PR
```bash
git push origin feature/ten-tinh-nang
# Tạo PR trên GitHub
```

### 6. PR Requirements
- [ ] Pass all CI checks
- [ ] Code follow coding standards
- [ ] Tests added/updated (nếu applicable)
- [ ] Docs updated (README, CHANGELOG, XML comments)
- [ ] No breaking changes (hoặc document breaking changes)
- [ ] Linked issue (Fixes #123)

## Coding Standards

### C# (.NET 8)
```csharp
// ✅ Good
public async Task<CleanResult> CleanAsync(
    List<CleanCategoryGroup> groups,
    IProgress<string> progress,
    CancellationToken cancellationToken = default)
{
    // Implementation
}

// ❌ Bad
public async Task CleanAsync(List<CleanCategoryGroup> groups) { }
```

### Naming Conventions
| Element | Convention | Example |
|---------|------------|---------|
| Namespace | PascalCase | `WinCleaner.Services` |
| Class/Interface | PascalCase | `ISystemScanner`, `SystemScanner` |
| Method | PascalCase | `GetDrivesAsync()` |
| Property | PascalCase | `TotalSize` |
| Parameter | camelCase | `cancellationToken` |
| Local variable | camelCase | `totalSize` |
| Constant | UPPER_SNAKE_CASE | `MAX_RETRY_COUNT` |
| Private field | _camelCase | `_scanner` |

### Async/Await
- Luôn dùng `Async` suffix cho async methods
- Dùng `CancellationToken` cho long-running operations
- Tránh `async void` (trừ event handlers)
- Dùng `ConfigureAwait(false)` cho library code

### Nullable Reference Types
```csharp
// Enable in .csproj: <Nullable>enable</Nullable>

// ✅ Good
public string? OptionalProperty { get; set; }
public string RequiredProperty { get; set; } = "";

// ✅ Good - null check
if (item.CleanAction is not null)
{
    await item.CleanAction(item, progress);
}
```

### Error Handling
```csharp
// ✅ Good - specific exceptions
try
{
    await DeletePathAsync(item, progress);
}
catch (UnauthorizedAccessException ex)
{
    Log(logProgress, LogLevel.Error, $"Access denied: {item.Path}", "Cleaner");
    return false;
}
catch (IOException ex) when (ex.HResult == 0x80070020) // FILE_IN_USE
{
    Log(logProgress, LogLevel.Warning, $"File in use: {item.Path}", "Cleaner");
    return false;
}
catch (Exception ex)
{
    Log(logProgress, LogLevel.Error, $"Unexpected error: {ex.Message}", "Cleaner");
    return false;
}
```

### Logging
```csharp
// Levels
LogDebug("Chi tiết debug")      // Chỉ dev
LogInfo("Thông tin chung")       // User sees
LogSuccess("Thành công")         // Green
LogWarning("Cảnh báo")           // Yellow
LogError("Lỗi")                  // Red
```

### XAML
- Sử dụng `StaticResource` cho styles/resources
- `DynamicResource` cho theme-aware resources
- `x:Bind` (compiled binding) thay vì `Binding` khi có thể
- VirtualizingStackPanel cho Lists lớn

## Testing

### Unit Tests
```bash
# Chạy tests
dotnet test --logger "console;verbosity=detailed"

# Coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Structure
```
Tests/
├── WinCleaner.Tests/
│   ├── Services/
│   │   ├── SystemScannerTests.cs
│   │   └── CleanerServiceTests.cs
│   ├── Models/
│   │   └── CleanModelsTests.cs
│   └── ViewModels/
│       └── MainViewModelTests.cs
```

### Test Naming
```csharp
[Fact]
public async Task CleanAsync_WithDryRunMode_ShouldNotDeleteFiles()

[Theory]
[InlineData(CleanProfile.Safe, 10)]
[InlineData(CleanProfile.Deep, 25)]
public void GetItemsForProfile_ShouldReturnCorrectCount(CleanProfile profile, int expectedCount)
```

## Documentation

### XML Comments
```csharp
/// <summary>
/// Quét hệ thống tìm các mục có thể dọn dẹp theo profile được chỉ định.
/// </summary>
/// <param name="profile">Profile dọn dẹp (Safe/Deep/Custom/Nuclear)</param>
/// <param name="progress">Progress reporter cho UI updates</param>
/// <param name="cancellationToken">Token để hủy tác vụ</param>
/// <returns>Danh sách các category group đã quét với size tính toán</returns>
/// <exception cref="OperationCanceledException">Khi tác vụ bị hủy</exception>
public async Task<List<CleanCategoryGroup>> ScanAsync(
    CleanProfile profile,
    IProgress<string>? progress = null,
    CancellationToken cancellationToken = default)
```

### README Updates
- Cập nhật tính năng mới
- Screenshots mới (nếu UI thay đổi)
- Version badge

### CHANGELOG
Tự động cập nhật qua Release notes hoặc manual:
```markdown
## [1.1.0] - 2026-01-15
### Added
- Support for Bun cache cleaning
- New "Export Log" feature

### Fixed
- Handle read-only files in DriverStore
- Memory leak in long-running scans

### Changed
- Updated to .NET 8.0.4
```

## 🏷️ Versioning
Sử dụng [Semantic Versioning](https://semver.org/):
- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

## 📞 Liên hệ
- **Maintainer**: Minh Tuấn (@minhtuancn)
- **Discussion**: [GitHub Discussions](https://github.com/minhtuancn/WinCleaner/discussions)
- **Security**: [SECURITY.md](SECURITY.md)

---

**Cảm ơn bạn đã đóng góp! 🎉**