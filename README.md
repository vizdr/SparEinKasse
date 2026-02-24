# SSKA Analyzer (SparEinKasse)

Personal finance analysis application for Windows. Processes German bank CSV exports (primarily Sparkasse format), categorizes transactions, and visualizes spending patterns through interactive charts.

## Features

- Import CSV bank statements (Sparkasse, other German banks)
- Automatic transaction categorization via keyword rules
- **Update Categorization** — re-apply updated category rules to existing data without re-importing
- Interactive spending charts and visualizations (bar, line, area)
- Category-Expense 3D chart with additional time-span comparison
- Multi-account support
- Filter by date range, category, account, booking text, and amount
- Localized UI (German, English, Russian)

## Screenshots

*Application dashboard with spending charts and category breakdown*

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

# Build CategoryFormatter library first (required dependency)
cmd.exe /c "E:\Microsoft_Visual_Studio_2022_Community\MSBuild\Current\Bin\MSBuild.exe" Categorization/CategoryFormatter/CategoryFormatter.csproj /p:Configuration=Release /p:Platform=x64

# Build main application
cmd.exe /c "E:\Microsoft_Visual_Studio_2022_Community\MSBuild\Current\Bin\MSBuild.exe" WpfApplication1/WpfApplication1.csproj /p:Configuration=Release /p:Platform=x64

# Run application
WpfApplication1\bin\x64\Release\SSKAanalyzer.exe
```

## Solution Structure

```
WpfApp-SSKA.sln
│
├── WpfApplication1/              # Main WPF application
│   ├── BusinessLogic/            # Core business logic
│   ├── DAL/                      # Data access layer (CSV/XML)
│   ├── DTO/                      # Data transfer objects and request model
│   ├── Presenters/               # MVP presenters
│   ├── IViews/                   # View interfaces
│   ├── Services/                 # Authorization services (WCF/gRPC)
│   ├── Protos/                   # gRPC protocol definitions
│   └── Local/                    # Localization resources (EN/DE/RU)
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
- **Business Logic:** `BusinessLogicSSKA` — calculations, parallel data queries
- **DAL:** XML-based transaction storage in `Documents\MySSKA\Arxiv\`

### Data Flow

```
CSV Import → CsvToXmlSSKA → Categorizer.exe → XML Storage
                                                   │
UI Charts ← ResponseModel ← BusinessLogicSSKA ←───┘

Categorization.csv update → "Update Categorization" button
  → CsvToXmlSSKA.UpdateCategorization() → XML Storage (in-place)
```

### Event-Driven Request Model

`DataRequest` is a singleton that raises events consumed by `BusinessLogicSSKA`:

| Event | Trigger |
|-------|---------|
| `DataRequested` | Date range or filter change |
| `FilterValuesRequested` | Filter panel opened |
| `DataBankUpdateRequested` | "Update DataStorage" button |
| `CategorizationUpdateRequested` | "Update Categorization" button |
| `ViewDataRequested` | Chart point click (detail popup) |

### Authorization Services

Dual-service architecture with fallback:
- **Primary:** WCF service (`ServARCode.svc`)
- **Fallback:** gRPC service (auto-generated from `authorization.proto`)

## Build Commands

**Note:** MSBuild must be invoked via `cmd.exe /c` with the full path. Build `CategoryFormatter` before `WpfApplication1`.

```bash
# Build CategoryFormatter library (required first)
cmd.exe /c "E:\Microsoft_Visual_Studio_2022_Community\MSBuild\Current\Bin\MSBuild.exe" Categorization/CategoryFormatter/CategoryFormatter.csproj /p:Configuration=Release /p:Platform=x64

# Build entire solution
cmd.exe /c "E:\Microsoft_Visual_Studio_2022_Community\MSBuild\Current\Bin\MSBuild.exe" WpfApp-SSKA.sln /p:Configuration=Release /p:Platform=x64

# Build console test utility (.NET 8.0)
dotnet build Categorization/ConsTestCSV/ConsTestCSV/ConsTestCSV.csproj

# Run xUnit tests
dotnet test WpfApplication1.Tests/WpfApplication1.Tests.csproj

# Build MSI installer (requires WiX Toolset v5)
dotnet build SetupSSKA.Wix/SetupSSKA.Wix.wixproj -c Release
```

## Output Artifacts

| Artifact | Path |
|----------|------|
| Application | `WpfApplication1/bin/x64/Release/SSKAanalyzer.exe` |
| Installer | `SetupSSKA.Wix/bin/x64/Release/SetupSSKA.Wix.msi` |

## User Data Location

```
Documents/
└── MySSKA/
    ├── Arxiv/                    # XML transaction database + categorized CSVs
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
3. **Add/Edit rules** — each rule maps transaction patterns to categories
4. **Save changes** — updates are written to `Categorization.csv`
5. Click **"Update Categorization"** in SSKA Analyzer to re-apply the new rules to all existing transactions immediately (no restart or re-import needed)

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

Rules are matched in order. First match wins. Leave fields empty to skip matching on that field. Keywords are case-insensitive; multi-word keywords are matched as substrings, single-word keywords as whole tokens.

### Tips

- Add specific rules before general ones
- Back up your `Categorization.csv` before major edits
- The "Update Categorization" button is disabled while an update is in progress; a red "Updating..." label appears while the operation runs

## Configuration

Application settings in `SSKAanalyzer.exe.config`:
- `GrpcServerAddress` — gRPC service endpoint
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
1. `SimpleSecurity` — no dependencies
2. `CategoryFormatter` — no dependencies
3. `WpfApplication1` — depends on SimpleSecurity, CategoryFormatter

### Adding New .cs Files

`WpfApplication1.csproj` is old-style (non-SDK format). New `.cs` files must be manually added as `<Compile Include="..."/>` entries in the project file.

### Adding New Categories

Use the **Categorizer** GUI tool (see [Using Categorizer](#using-categorizer)) or manually edit:
- Installed: `Documents\MySSKA\Categorization\Categorization.csv`
- Development: `Categorization/Categorization.csv`

Then click **"Update Categorization"** in the running application.

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
