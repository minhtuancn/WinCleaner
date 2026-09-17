# Security Policy

## Supported Versions

Chúng tôi cung cấp bản vá bảo mật cho các phiên bản sau:

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | ✅ Yes             |
| < 1.0   | ❌ No              |

## Reporting a Vulnerability

Nếu bạn phát hiện lỗ hổng bảo mật, **KHÔNG** tạo issue công khai. Hãy báo cáo riêng tư:

### 📧 Cách báo cáo
- **Email**: security@minhtuancn.github.io (hoặc GitHub Security Advisory)
- **GitHub**: [Private Security Advisory](https://github.com/minhtuancn/WinCleaner/security/advisories/new)

### 📋 Thông tin cần cung cấp
1. **Mô tả lỗ hổng**: Chi tiết kỹ thuật
2. **Bước tái hiện**: Proof of concept
3. **Tác động**: Data exposure, RCE, privilege escalation, etc.
4. **Phiên bản ảnh hưởng**: WinCleaner version, Windows version
5. **Khuyến nghị**: Temporary workaround (nếu có)

### ⏱️ Thời gian phản hồi
- **48h**: Confirm nhận báo cáo
- **7 ngày**: Initial assessment
- **30 ngày**: Fix & release (critical)
- **90 ngày**: Fix & release (non-critical)

## Security Considerations

### 🔐 Application Design
- **No network access**: WinCleaner hoàn toàn offline, không tải dữ liệu từ internet
- **No auto-update**: Không có background updater có thể bị khai thác
- **Least privilege**: Chỉ yêu cầu Admin khi dọn system folders (DriverStore, Windows)
- **No telemetry**: Không thu thập dữ liệu người dùng

### 🛡️ File Operations
- **Dry-run default**: Mặc định bật Dry-run để preview trước khi xóa
- **Confirmation dialog**: Yêu cầu xác nhận trước khi xóa thực tế
- **Recycle Bin integration**: Sử dụng Windows Shell API cho Recycle Bin (có thể restore)
- **No hardcoded paths**: Tất cả paths resolved tại runtime qua Environment variables

### 🔒 Manifest & Permissions
```xml
<!-- app.manifest -->
<requestedExecutionLevel level="asInvoker" uiAccess="false" />
<!-- Chỉ yêu cầu Admin khi user chọn profile Deep/Custom/Nuclear -->
```

### 📁 Data Handling
- **Settings**: Stored in `%APPDATA%\WinCleaner\settings.json` (user-only access)
- **Logs**: In-memory only, optional export to user-chosen location
- **No credentials**: Không lưu password, token, API keys

### ⚠️ Known Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Accidental system file deletion | Low | High | Risk levels, confirmations, dry-run |
| DriverStore cleanup breaks drivers | Medium | High | Profile Deep+ only, DISM safe API |
| CompactOS/Hibernation disable | Low | Medium | Nuclear profile only, explicit warning |
| Other user data access | Low | High | Admin required, explicit per-user opt-in |

### 🔄 Dependency Security
- **NuGet packages**: Signed, verified sources (nuget.org)
- **Pinned versions**: Exact versions in `.csproj`
- **Audit**: `dotnet list package --vulnerable` in CI
- **Minimal deps**: Chỉ 7 NuGet packages

```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="8.0.0" />
<PackageReference Include="Hardcodet.NotifyIcon.Wpf" Version="1.1.0" />
<PackageReference Include="System.Management" Version="8.0.0" />
```

### 🧪 Security Testing
- Static Analysis: `dotnet build -p:EnableNETAnalyzers=true`
- SAST: GitHub CodeQL (configured in CI)
- Dependency scanning: Dependabot alerts enabled
- Manual review: All file deletion paths reviewed

## Disclosure Policy

### Coordinated Disclosure
1. Reporter báo cáo privately
2. Maintainer xác nhận & assess
3. Develop fix in private fork
4. Release patched version
5. Public disclosure sau 30-90 ngày (hoặc khi user đã update)

### Credit
Reporter sẽ được ghi nhận trong:
- Release notes
- Security Advisories
- Hall of Fame (nếu công khai)

## Secure Usage Guidelines

### For Users
1. **Luôn chạy Dry-run trước** để xem sẽ xóa gì
2. **Backup quan trọng** trước khi dùng Deep/Custom/Nuclear
3. **Chạy as Admin** chỉ khi cần (DriverStore, Windows folders)
4. **Review selection** trước khi nhấn Clean
5. **Export log** để存档

### For Admins/Enterprise
- Deploy via Intune/SCCM với profile Safe only
- Group Policy để disable Nuclear profile
- Monitor Event Viewer cho WinCleaner events
- Test trên non-production trước

## Contact

**Security Team**: Minh Tuấn
- GitHub Security Advisory (preferred)
- Email: security@minhtuancn.github.io

**PGP Key**: (Available on GitHub profile)

---

*Cập nhật lần cuối: 2026-09-17*
*Version: 1.0*