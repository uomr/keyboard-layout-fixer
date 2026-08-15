# Contributing to KeyboardFixer

Thank you for your interest in contributing! 🎉

## How to Contribute

### 🐛 Reporting Bugs
1. Check [existing issues](../../issues) first.
2. Open a new issue with:
   - Windows version
   - Application where the bug occurred
   - Steps to reproduce
   - Expected vs. actual result

### 💡 Suggesting Features
Open an issue with the label `enhancement` and describe:
- The use case
- Why it would benefit others

### 🔧 Submitting Code

1. **Fork** the repository
2. **Clone** your fork:
   ```bash
   git clone https://github.com/YOUR_USERNAME/keyboard-layout-fixer.git
   ```
3. Create a **feature branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```
4. Make your changes in `src/KeyboardFixer.cs`
5. **Test** by running:
   ```powershell
   .\build.ps1
   .\bin\KeyboardFixer.exe
   ```
6. **Commit** with a clear message:
   ```bash
   git commit -m "feat: add support for Persian keyboard layout"
   ```
7. **Push** and open a Pull Request

## Code Style

- Keep everything in `KeyboardFixer.cs` (single-file philosophy)
- Add comments in Arabic and/or English for any mapping changes
- Test with: Notepad, Chrome, Word, and at least one legacy Win32 app

## Keyboard Map Changes

If you're adding a new language or fixing a mapping, update `docs/keyboard-map.md` as well.

---

Thank you! شكراً لمساهمتك 🙏
