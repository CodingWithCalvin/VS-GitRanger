<p align="center">
  <img src="src/CodingWithCalvin.GitRanger/Resources/Icons/icon.png" alt="Git Ranger Logo" width="128" />
</p>

# 🤠 Git Ranger

> *Taming your Git history, one line at a time!* 🐎

[![Visual Studio 2022](https://img.shields.io/badge/Visual%20Studio-2022%20%7C%202026-purple?logo=visualstudio&logoColor=white)](https://visualstudio.microsoft.com/)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-blue?logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

A **visually stunning** Git management extension for Visual Studio 2022/2026, bringing GitLens-style functionality with theme-adaptive vibrant colors. 🎨✨

---

## 🚀 Features

### 🔍 Inline Blame Annotations

See who changed each line **directly in the editor** — author name, commit date, and message displayed right at the end of each line!

- 🎨 **Color-coded by author** — each contributor gets a unique vibrant color
- 🔥 **Heat map mode** — green = recent, red = old (optional)
- 👁️ **Configurable opacity** and display format
- 💬 **Hover for full commit details**

### 📊 Blame Gutter Margin

A visual indicator in the editor margin showing commit history **at a glance**.

- 📈 Age bars showing relative commit age
- 🎯 Author color indicators
- 📋 Click to copy commit SHA
- 🔎 Hover for commit details

### 🔮 What's Next?

Check out our [issues list](https://github.com/CodingWithCalvin/VS-GitRanger/issues) to see what features are planned and vote on what you'd like to see next!

---

## 📦 Installation

### From Visual Studio Marketplace

🚧 *Coming soon!* 🚧

### From Source

```bash
# 1. Clone the repository
git clone https://github.com/CodingWithCalvin/VS-GitRanger.git

# 2. Open in Visual Studio 2022
# 3. Build the solution (F5 to debug)
# 4. VSIX will be created in the output directory
```

---

## ⚙️ Configuration

Configure Git Ranger via **Tools → Options → Git Ranger**

### 🏷️ Blame Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Enable Inline Blame | Show blame at end of lines | ✅ `true` |
| Enable Blame Gutter | Show blame in margin | ✅ `true` |
| Show Author Name | Display author in inline blame | ✅ `true` |
| Show Commit Date | Display date in inline blame | ✅ `true` |
| Show Commit Message | Display message in inline blame | ✅ `true` |
| Date Format | `relative` or custom format string | `relative` |

### 🎨 Color Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Color Mode | `Author`, `Age`, or `None` | `Author` |
| Max Age (days) | Maximum age for heat map | `365` |

### 🖥️ Display Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Inline Blame Opacity | Transparency (0.0 - 1.0) | `0.7` |
| Compact Mode | Condensed display format | ❌ `false` |
| Gutter Width | Width in pixels | `40` |
| Show Age Bars | Visual age indicators | ✅ `true` |

---

## 📋 Requirements

- 💻 Visual Studio 2022 (17.0) or later
- 🔧 .NET Framework 4.8

---

## 🛠️ Technology Stack

| Component | Technology |
|-----------|------------|
| 🔗 Git Integration | LibGit2Sharp |
| 🎨 UI Framework | WPF |
| 📊 Graph Rendering | SkiaSharp *(planned)* |
| 🔌 VS Integration | Community.VisualStudio.Toolkit |

---

## 🤝 Contributing

Contributions are welcome! Feel free to submit issues and pull requests. 💪

1. 🍴 Fork the repository
2. 🌿 Create a feature branch (`git checkout -b feature/amazing-feature`)
3. 💾 Commit your changes (`git commit -m 'feat: add amazing feature'`)
4. 📤 Push to the branch (`git push origin feature/amazing-feature`)
5. 🎉 Open a Pull Request

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

## 👨‍💻 Author

**Calvin A. Allen**

[![Website](https://img.shields.io/badge/Website-codingwithcalvin.net-blue?style=flat&logo=google-chrome&logoColor=white)](https://codingwithcalvin.net)
[![GitHub](https://img.shields.io/badge/GitHub-CodingWithCalvin-181717?style=flat&logo=github)](https://github.com/CodingWithCalvin)

---

<div align="center">

### ⭐ If you find Git Ranger useful, please consider giving it a star! ⭐

*Made with ❤️ for the Visual Studio community from Coding With Calvin*

</div>
