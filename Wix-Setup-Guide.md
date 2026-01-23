# WiX Toolset v5 with HeatWave - Setup Guide

## Prerequisites

1. **Install HeatWave extension** in Visual Studio:
   - Extensions → Manage Extensions → Search "HeatWave" → Install
   - Restart Visual Studio

2. **.NET SDK** must be installed (comes with Visual Studio)

---

## Creating a New WiX Installer Project

### Option 1: Via Visual Studio

1. Right-click Solution → **Add → New Project**
2. Search for "**WiX**" or "**MSI**"
3. Select "**MSI Package (WiX v5)**"
4. Name your project (e.g., `SetupMyApp.Wix`)
5. Click Create

### Option 2: Via Command Line

```bash
dotnet new wix -n SetupMyApp.Wix
```

---

## Project Structure

```
SetupMyApp.Wix/
├── SetupMyApp.Wix.wixproj    # Project file (SDK-style)
├── Package.wxs               # Main installer definition
└── bin/Release/
    └── SetupMyApp.Wix.msi    # Output MSI file
```

---

## Key Files Explained

### 1. Project File (.wixproj)

```xml
<Project Sdk="WixToolset.Sdk/5.0.0">
  <PropertyGroup>
    <OutputType>Package</OutputType>
    <InstallerPlatform>x64</InstallerPlatform>
  </PropertyGroup>
</Project>
```

### 2. Package Definition (Package.wxs)

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Package
    Name="My Application"
    Manufacturer="Your Company"
    Version="1.0.0"
    UpgradeCode="PUT-GUID-HERE"
    Scope="perMachine">

    <!-- Upgrade handling -->
    <MajorUpgrade
      DowngradeErrorMessage="A newer version is already installed." />

    <!-- Directory structure -->
    <StandardDirectory Id="ProgramFiles64Folder">
      <Directory Id="INSTALLFOLDER" Name="MyApp" />
    </StandardDirectory>

    <!-- Files to install -->
    <ComponentGroup Id="MainFiles" Directory="INSTALLFOLDER">
      <Component Id="MainExe">
        <File Source="..\MyApp\bin\Release\MyApp.exe" KeyPath="yes" />
      </Component>
      <Component Id="ConfigFile">
        <File Source="..\MyApp\bin\Release\MyApp.exe.config" />
      </Component>
    </ComponentGroup>

    <!-- Feature (what user can select to install) -->
    <Feature Id="Main" Title="My Application" Level="1">
      <ComponentGroupRef Id="MainFiles" />
    </Feature>

  </Package>
</Wix>
```

---

## Common Tasks

### Adding Files

```xml
<Component Id="MyDll">
  <File Source="..\bin\Release\MyLibrary.dll" />
</Component>
```

### Adding Shortcuts (Start Menu)

```xml
<StandardDirectory Id="ProgramMenuFolder">
  <Directory Id="AppMenuFolder" Name="My Application" />
</StandardDirectory>

<ComponentGroup Id="Shortcuts" Directory="AppMenuFolder">
  <Component Id="AppShortcut">
    <Shortcut Id="StartMenuShortcut"
              Name="My Application"
              Target="[INSTALLFOLDER]MyApp.exe"
              WorkingDirectory="INSTALLFOLDER" />
    <RemoveFolder Id="RemoveMenuFolder" On="uninstall" />
    <RegistryValue Root="HKCU" Key="Software\MyCompany\MyApp"
                   Name="installed" Type="integer" Value="1" KeyPath="yes" />
  </Component>
</ComponentGroup>
```

### Adding Desktop Shortcut

```xml
<StandardDirectory Id="DesktopFolder" />

<ComponentGroup Id="DesktopShortcut" Directory="DesktopFolder">
  <Component Id="DesktopShortcutComp">
    <Shortcut Id="DesktopShortcut"
              Name="My Application"
              Target="[INSTALLFOLDER]MyApp.exe" />
    <RegistryValue Root="HKCU" Key="Software\MyCompany\MyApp"
                   Name="desktopShortcut" Type="integer" Value="1" KeyPath="yes" />
  </Component>
</ComponentGroup>
```

### Setting Application Icon in Add/Remove Programs

```xml
<Icon Id="AppIcon.ico" SourceFile="..\MyApp\app.ico" />
<Property Id="ARPPRODUCTICON" Value="AppIcon.ico" />
```

---

## Build Order Setup

Since WiX packages files from build output, set project dependencies:

1. Right-click **Solution** → **Project Dependencies**
2. Select your WiX project
3. Check the main application project(s)

---

## Building

### From Visual Studio

- Build → Build Solution (F6)
- Or right-click WiX project → Build

### From Command Line

```bash
# Build main app first
msbuild MyApp\MyApp.csproj /p:Configuration=Release

# Build installer
dotnet build SetupMyApp.Wix\SetupMyApp.Wix.wixproj -c Release
```

**Output:** `SetupMyApp.Wix\bin\Release\SetupMyApp.Wix.msi`

---

## Installation Scopes

| Scope | Location | Requires Admin |
|-------|----------|----------------|
| `perMachine` | Program Files | Yes |
| `perUser` | AppData\Local | No |

---

## Useful Properties

```xml
<!-- Help/Support links in Add/Remove Programs -->
<Property Id="ARPHELPLINK" Value="https://myapp.com/support" />
<Property Id="ARPURLINFOABOUT" Value="https://myapp.com" />

<!-- Don't show Modify button -->
<Property Id="ARPNOMODIFY" Value="1" />
```

---

## Debugging Tips

1. **View MSI contents:** Use tools like Orca (from Windows SDK) or LessMSI

2. **Verbose install log:**
   ```cmd
   msiexec /i SetupMyApp.Wix.msi /l*v install.log
   ```

3. **Test uninstall:**
   ```cmd
   msiexec /x SetupMyApp.Wix.msi
   ```

---

## Resources

- WiX v5 Documentation: https://wixtoolset.org/docs/
- WiX GitHub: https://github.com/wixtoolset/wix
