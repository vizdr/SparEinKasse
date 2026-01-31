# SSKA Analyzer (SparEinKasse)

Personal finance analysis application for Windows. Processes German bank CSV exports (primarily Sparkasse format), categorizes transactions, and visualizes spending patterns through interactive charts.

## Features

- Import CSV bank statements (Sparkasse, other German banks)
- Automatic transaction categorization
- Interactive spending charts and visualizations
- Multi-account support
- Filter by date range, category, account
- Localized UI (German, English, Russian)

## Screenshots

<p align="center">
  <img src="Screenshot-SSKA_data _analyzer.png" width="900">
</p>

<p align="center">
  <img src="Screenshot-3-SSKA_data _analyzer.png" width="900">
</p>

## Requirements

- Windows 10/11
- .NET Framework 4.8.1
- Visual Studio 2019/2022 (for development)

## Installation

### From Installer

Download and run `SetupSSKA.Wix.msi` from the Releases.

### From Source

```bash
# Clone repository
git clone <repository-url>
cd SparEinKasse

# Build solution
msbuild WpfApp-SSKA.sln /p:Configuration=Release

# Run application
WpfApplication1\bin\Release\SSKAanalyzer.exe
```

## Solution Structure

```
WpfApp-SSKA.sln
│
├── WpfApplication1/              # Main WPF application
│   ├── BusinessLogic/            # Core business logic
│   ├── DAL/                      # Data access layer (CSV/XML)
│   ├── DTO/                      # Data transfer objects
│   ├── Presenters/               # MVP presenters
│   ├── IViews/                   # View interfaces
│   ├── Services/                 # Authorization services (WCF/gRPC)
│   ├── Protos/                   # gRPC protocol definitions
│   └── Local/                    # Localization resources
│
├── SimpleSecurity/               # License verification library
│
├── Categorization/
│   ├── CategoryFormatter/        # CSV categorization library (.NET 4.8)
│   ├── ConsTestCSV/              # Console test utility (.NET 8.0)
│   ├── Categorizer.exe           # Qt5 categorization tool
│   └── Categorization.csv        # Category definitions
│
├── WpfApplication1.Tests/        # Unit tests (xUnit)
│
└── SetupSSKA.Wix/                # MSI installer (WiX Toolset v5)
```

## Architecture

### MVP Pattern with Dependency Injection

```
┌─────────────┐     ┌─────────────┐     ┌─────────────────┐
│    Views    │────▶│  Presenters │────▶│  Business Logic │
│   (XAML)    │◀────│             │◀────│                 │
└─────────────┘     └─────────────┘     └─────────────────┘
                                               │
                                               ▼
                                        ┌─────────────┐
                                        │  DAL (XML)  │
                                        └─────────────┘
```

- **Views:** XAML windows implementing `IView*` interfaces
- **Presenters:** `ChartsPresenter`, `AccountsPresenter`, `SettingsPresenter`
- **Business Logic:** `BusinessLogicSSKA` - calculations, data processing
- **DAL:** XML-based transaction storage in `Documents\MySSKA\Arxiv\`

### Data Flow

```
CSV Import → CsvToXmlSSKA → Categorizer.exe → XML Storage
                                                   │
UI Charts ← ResponseModel ← BusinessLogicSSKA ←───┘
```

### Authorization Services

Dual-service architecture with fallback:
- **Primary:** WCF service (`ServARCode.svc`)
- **Fallback:** gRPC service (auto-generated from `authorization.proto`)

## Build Commands

```bash
# Build entire solution
msbuild WpfApp-SSKA.sln /p:Configuration=Release

# Build main application only
msbuild WpfApplication1/WpfApplication1.csproj /p:Configuration=Release

# Build CategoryFormatter library (required dependency)
msbuild Categorization/CategoryFormatter/CategoryFormatter.csproj /p:Configuration=Release

# Build console test utility
dotnet build Categorization/ConsTestCSV/ConsTestCSV/ConsTestCSV.csproj

# Run tests
dotnet test WpfApplication1.Tests/WpfApplication1.Tests.csproj

# Build MSI installer (requires WiX Toolset v5)
dotnet build SetupSSKA.Wix/SetupSSKA.Wix.wixproj -c Release
```

## Output Artifacts

| Artifact | Path |
|----------|------|
| Application | `WpfApplication1/bin/Release/SSKAanalyzer.exe` |
| Installer | `SetupSSKA.Wix/bin/Release/SetupSSKA.Wix.msi` |

## User Data Location

```
Documents/
└── MySSKA/
    ├── Arxiv/                    # XML transaction database
    └── Categorization/           # Category editor and definitions
        ├── Categorization.csv    # User-editable category rules
        ├── Categorizer.exe       # Qt5 GUI editor
        └── (Qt5 DLLs)            # Required Qt runtime
```

## Using Categorizer

The **Categorizer** is a Qt5-based GUI tool for editing transaction category rules.

### Location

After installation: `Documents\MySSKA\Categorization\Categorizer.exe`

### How It Works

1. **Open Categorizer.exe** from `Documents\MySSKA\Categorization\`
2. The tool automatically loads `Categorization.csv` from the same folder
3. **Add/Edit rules** - each rule maps transaction patterns to categories
4. **Save changes** - updates are written to `Categorization.csv`
5. **Restart SSKA Analyzer** to apply new category rules

### CSV Format

The `Categorization.csv` file uses semicolon-delimited format:

```csv
Category;Beneficiary;Reason4Payment;BookingText;
FoodMarket;ALDI;;;
FoodMarket;LIDL;;;
FoodMarket;REWE;;;
Energy;Energie;;;
CashDispenser;SPARKASSE;;;
```

| Column | Description |
|--------|-------------|
| **Category** | Category name shown in charts |
| **Beneficiary** | Match against beneficiary/payee field |
| **Reason4Payment** | Match against payment purpose field |
| **BookingText** | Match against booking text field |

Rules are matched in order. First match wins. Leave fields empty to skip matching.

### Tips

- Patterns are case-insensitive
- Partial matches work (e.g., "ALDI" matches "ALDI SUED 1234")
- Add specific rules before general ones
- Back up your `Categorization.csv` before major edits

## Configuration

Application settings in `SSKAanalyzer.exe.config`:
- `GrpcServerAddress` - gRPC service endpoint
- WCF service bindings

## Key Dependencies

| Package | Purpose |
|---------|---------|
| DotNetProjects.WpfToolkit.DataVisualization | Chart controls |
| Microsoft.Extensions.DependencyInjection | DI container |
| Microsoft.Extensions.Hosting | Host builder |
| Grpc.Net.Client | gRPC client |
| Grpc.Tools | Proto code generation |
| Google.Protobuf | Protocol buffers |

## Development

### Prerequisites

- Visual Studio 2019/2022
- .NET Framework 4.8.1 SDK
- .NET 8.0 SDK (for test utilities)
- WiX Toolset v5 (for installer)

### Project Dependencies

Build order:
1. `SimpleSecurity` - no dependencies
2. `CategoryFormatter` - no dependencies
3. `WpfApplication1` - depends on SimpleSecurity, CategoryFormatter

### Adding New Categories

Use the **Categorizer** GUI tool (see [Using Categorizer](#using-categorizer)) or manually edit:
- Installed: `Documents\MySSKA\Categorization\Categorization.csv`
- Development: `Categorization/Categorization.csv`

## Documentation

| Document | Description |
|----------|-------------|
| [Extension_Wcf_to_Grpc.md](Extension_Wcf_to_Grpc.md) | WCF/gRPC fallback implementation |
| [Grpc_From_Copy_to_Autogeneration.md](Grpc_From_Copy_to_Autogeneration.md) | gRPC code generation setup |
| [Wix-Setup-Guide.md](Wix-Setup-Guide.md) | Installer build guide |

## License

Copyright Vladimir Zdravkov

## Author

V. Zdravkov
