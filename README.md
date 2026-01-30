<p align="center">
  <img src="https://raw.githubusercontent.com/CodingWithCalvin/VS-GitRanger/main/resources/icon.png" alt="Git Ranger Logo" width="128" height="128">
</p>

<h1 align="center">Git Ranger</h1>

<p align="center">
  <strong>A visually stunning Git management extension for Visual Studio with theme-adaptive vibrant colors</strong>
</p>

<p align="center">
  <a href="https://github.com/CodingWithCalvin/VS-GitRanger/blob/main/LICENSE">
    <img src="https://img.shields.io/github/license/CodingWithCalvin/VS-GitRanger?style=for-the-badge" alt="License">
  </a>
  <a href="https://github.com/CodingWithCalvin/VS-GitRanger/actions/workflows/build.yml">
    <img src="https://img.shields.io/github/actions/workflow/status/CodingWithCalvin/VS-GitRanger/build.yml?style=for-the-badge" alt="Build Status">
  </a>
</p>

<p align="center">
  <a href="https://marketplace.visualstudio.com/items?itemName=CodingWithCalvin.VS-GitRanger">
    <img src="https://img.shields.io/visual-studio-marketplace/v/CodingWithCalvin.VS-GitRanger?style=for-the-badge" alt="Marketplace Version">
  </a>
  <a href="https://marketplace.visualstudio.com/items?itemName=CodingWithCalvin.VS-GitRanger">
    <img src="https://img.shields.io/visual-studio-marketplace/i/CodingWithCalvin.VS-GitRanger?style=for-the-badge" alt="Marketplace Installations">
  </a>
  <a href="https://marketplace.visualstudio.com/items?itemName=CodingWithCalvin.VS-GitRanger">
    <img src="https://img.shields.io/visual-studio-marketplace/d/CodingWithCalvin.VS-GitRanger?style=for-the-badge" alt="Marketplace Downloads">
  </a>
  <a href="https://marketplace.visualstudio.com/items?itemName=CodingWithCalvin.VS-GitRanger">
    <img src="https://img.shields.io/visual-studio-marketplace/r/CodingWithCalvin.VS-GitRanger?style=for-the-badge" alt="Marketplace Rating">
  </a>
</p>

---

## Features

### Inline Blame Annotations

See who changed each line **directly in the editor** - author name, commit date, and message displayed right at the end of each line!

- **Color-coded by author** - each contributor gets a unique vibrant color
- **Heat map mode** - green = recent, red = old (optional)
- **Configurable opacity** and display format
- **Hover for full commit details**

![Inline Blame](https://raw.githubusercontent.com/CodingWithCalvin/VS-GitRanger/main/resources/blame-inline.png)

### Blame Gutter Margin

A visual indicator in the editor margin showing commit history **at a glance**.

- Age bars showing relative commit age
- Author color indicators
- Click to copy commit SHA
- Hover for commit details

![Blame Gutter](https://raw.githubusercontent.com/CodingWithCalvin/VS-GitRanger/main/resources/blame-gutter.png)

### Status Bar Blame

See blame info for the **current line** right in the Visual Studio status bar - updates instantly as you navigate!

- **Real-time updates** - blame follows your cursor
- **Customizable format** - choose what to display with `{author}`, `{date}`, `{message}`, `{sha}` placeholders
- **Relative or absolute dates** - "2 days ago" or "1/21/2026"
- **Auto-truncate** - configurable max length keeps your status bar tidy

![Status Bar Blame](https://raw.githubusercontent.com/CodingWithCalvin/VS-GitRanger/main/resources/blame-status-bar.png)

### What's Next?

Check out our [issues list](https://github.com/CodingWithCalvin/VS-GitRanger/issues) to see what features are planned and vote on what you'd like to see next!

## Installation

### Visual Studio Marketplace

1. Open Visual Studio 2022 or 2026
2. Go to **Extensions > Manage Extensions**
3. Search for "Git Ranger"
4. Click **Download** and restart Visual Studio

### Manual Installation

Download the latest `.vsix` from the [Releases](https://github.com/CodingWithCalvin/VS-GitRanger/releases) page and double-click to install.

## Configuration

Configure Git Ranger via **Tools > Options > Git Ranger**

### Blame Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Enable Inline Blame | Show blame at end of lines | `true` |
| Enable Blame Gutter | Show blame in margin | `true` |
| Show Author Name | Display author in inline blame | `true` |
| Show Commit Date | Display date in inline blame | `true` |
| Show Commit Message | Display message in inline blame | `true` |
| Date Format | `relative` or custom format string | `relative` |

### Color Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Color Mode | `Author`, `Age`, or `None` | `Author` |
| Max Age (days) | Maximum age for heat map | `365` |

### Display Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Inline Blame Opacity | Transparency (0.0 - 1.0) | `0.7` |
| Compact Mode | Condensed display format | `false` |
| Gutter Width | Width in pixels | `40` |
| Show Age Bars | Visual age indicators | `true` |

### Status Bar Settings

| Setting | Description | Default |
|---------|-------------|---------|
| Enable Status Bar Blame | Show blame in status bar | `true` |
| Format | Template with `{author}`, `{date}`, `{message}`, `{sha}` | `{author}, {date} • {message}` |
| Use Relative Dates | Show "2 days ago" vs absolute date | `true` |
| Max Length | Truncate long messages (0 = unlimited) | `100` |

### Diagnostics

| Setting | Description | Default |
|---------|-------------|---------|
| Log Level | Output pane verbosity: `None`, `Error`, `Info`, `Verbose` | `Error` |

*Logs are written to the "Git Ranger" output pane in Visual Studio.*

## Requirements

- Visual Studio 2022 (17.0) or later
- .NET Framework 4.8

## Technology Stack

| Component | Technology |
|-----------|------------|
| Git Integration | LibGit2Sharp |
| UI Framework | WPF |
| Graph Rendering | SkiaSharp *(planned)* |
| VS Integration | Community.VisualStudio.Toolkit |

## Contributing

Contributions are welcome! Whether it's bug reports, feature requests, or pull requests - all feedback helps make this extension better.

### Development Setup

1. Clone the repository
2. Open the solution in Visual Studio 2022 or 2026
3. Ensure you have the "Visual Studio extension development" workload installed
4. Press F5 to launch the experimental instance

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## Contributors

<!-- readme: contributors -start -->
<!-- readme: contributors -end -->

---

<p align="center">
  Made with ❤️ by <a href="https://github.com/CodingWithCalvin">Coding With Calvin</a>
</p>
