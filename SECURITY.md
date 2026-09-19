# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 2.0.x   | :white_check_mark: |
| 1.x.x   | :x:                |

## Reporting a Vulnerability

We take security vulnerabilities seriously. If you discover a security vulnerability in WinCleaner, please report it responsibly.

### How to Report

**Please do NOT report security vulnerabilities through public GitHub issues.**

Instead, please report them via email to: **security@minhtuancn.github.io** (or create a private security advisory on GitHub)

### What to Include

Please include the following information in your report:

- Type of issue (e.g., buffer overflow, SQL injection, cross-site scripting, etc.)
- Full paths of source file(s) related to the vulnerability
- The location of the affected source code (tag/branch/commit or direct URL)
- Any special configuration required to reproduce the issue
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact of the issue, including how an attacker might exploit it

### Response Timeline

- **Acknowledgment**: Within 48 hours
- **Initial Assessment**: Within 5 business days
- **Fix Timeline**: Depends on severity
  - Critical: Within 7 days
  - High: Within 14 days
  - Medium: Within 30 days
  - Low: Within 90 days

## Security Features

WinCleaner implements several security measures:

- **Path Safety Validation**: All file operations are validated against a whitelist of safe paths
- **App Running Guard**: Prevents cleaning files used by running applications
- **Secure Deletion**: Supports DoD 5220.22-M and other shredding algorithms
- **System Restore Points**: Creates restore points before cleaning operations
- **Least Privilege**: Runs with minimal required permissions
- **Code Signing**: All releases are code-signed (planned for v2.0.0)

## Responsible Disclosure

We follow responsible disclosure practices. Once a vulnerability is reported:

1. We acknowledge receipt within 48 hours
2. We investigate and validate the issue
3. We develop and test a fix
4. We coordinate disclosure timing with the reporter
5. We release the fix and publish an advisory

## Security Updates

Security updates are released as patch versions (e.g., 2.0.1, 2.0.2) and announced via:

- GitHub Security Advisories
- Release notes
- In-app update notifications (when implemented)

## Contact

For security-related questions or concerns, contact: **security@minhtuancn.github.io**