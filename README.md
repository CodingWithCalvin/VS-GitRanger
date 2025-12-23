# Git Ranger

A visually exciting Git management extension for Visual Studio 2022/2026, bringing GitLens-style functionality with theme-adaptive vibrant colors.

## Features

### Inline Blame Annotations
See who changed each line directly in the editor, with author name, commit date, and message displayed at the end of each line.

- Color-coded by author (each contributor gets a unique vibrant color)
- Optional age-based heat map (green = recent, red = old)
- Configurable opacity and display format
- Hover for full commit details

### Blame Gutter Margin
A visual indicator in the editor margin showing commit history at a glance.

- Age bars showing relative commit age
- Author color indicators
- Click to copy commit SHA
- Hover for commit details

### Planned Features
- **File History** - View all commits affecting the current file
- **Commit Details** - Deep dive into any commit
- **Interactive Git Graph** - Visual branch/merge history (SkiaSharp-powered)
- **Comparison Tools** - Compare with previous revisions

## Installation

### From Visual Studio Marketplace
*(Coming soon)*

### From Source
1. Clone the repository
2. Open `CodingWithCalvin.GitRanger.sln` in Visual Studio 2022
3. Build the solution
4. The VSIX will be created in the output directory

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

## Requirements

- Visual Studio 2022 (17.0) or later
- .NET Framework 4.8

## Technology Stack

- **Git Integration**: LibGit2Sharp
- **UI Framework**: WPF
- **Graph Rendering**: SkiaSharp (planned)
- **VS Integration**: Community.VisualStudio.Toolkit

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

**Calvin A. Allen**
- Website: [codingwithcalvin.net](https://codingwithcalvin.net)
- GitHub: [@CodingWithCalvin](https://github.com/CodingWithCalvin)

---

*Git Ranger - Taming your Git history, one line at a time.*
