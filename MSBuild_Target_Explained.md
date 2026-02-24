# MSBuild Targets File Explained

## What is it?

A `.targets` file is an MSBuild XML file that defines **reusable build logic** - tasks, targets, and properties that can be imported into any project.

## Core Concepts

| Concept | Description | Example |
|---------|-------------|---------|
| **Target** | A named group of tasks that run together | `<Target Name="Compile">` |
| **Task** | A single build action | Copy files, run compiler, execute command |
| **Property** | A variable | `<OutputPath>bin\Release\</OutputPath>` |
| **Item** | A list of files/inputs | `<Compile Include="*.cs" />` |

## Simple Example

```xml
<!-- MyCustom.targets -->
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

  <!-- Define a property -->
  <PropertyGroup>
    <MyOutputDir>$(ProjectDir)out\</MyOutputDir>
  </PropertyGroup>

  <!-- Define a target -->
  <Target Name="CopyOutput" AfterTargets="Build">
    <Copy SourceFiles="@(OutputFiles)" DestinationFolder="$(MyOutputDir)" />
  </Target>

</Project>
```

## How Grpc.Tools.targets Works

```
┌─────────────────────────────────────────────────────────┐
│                   Grpc.Tools.targets                     │
├─────────────────────────────────────────────────────────┤
│  1. Defines <Protobuf> item type                        │
│  2. Locates protoc.exe compiler (in NuGet package)      │
│  3. Runs BEFORE CoreCompile target                      │
│  4. For each .proto file:                               │
│     - Runs: protoc --csharp_out=... --grpc_out=...     │
│     - Generates: Authorization.cs, AuthorizationGrpc.cs │
│  5. Adds generated .cs files to <Compile> items         │
└─────────────────────────────────────────────────────────┘
```

## Build Pipeline with Targets

```
Your .csproj
    │
    ├── <Import Microsoft.CSharp.targets>     ← Standard C# build
    │       │
    │       ├── Target: CoreCompile           ← Compiles .cs files
    │       ├── Target: Build                 ← Main build orchestration
    │       └── ...
    │
    └── <Import Grpc.Tools.targets>           ← Proto compilation
            │
            └── Target: _gRPC_Proto           ← Runs BEFORE CoreCompile
                    │
                    └── Generates .cs from .proto
```

## Common Target Hooks

| Attribute | When it runs |
|-----------|--------------|
| `BeforeTargets="Build"` | Before Build starts |
| `AfterTargets="Build"` | After Build completes |
| `DependsOnTargets="X"` | After target X completes |

## .props vs .targets

| File | When imported | Purpose |
|------|---------------|---------|
| `.props` | At the **beginning** | Set default properties, define items |
| `.targets` | At the **end** | Define targets that use those properties |

## View What's Happening

You can see the full build with all imports:

```bash
msbuild WpfApplication1.csproj /pp:fullbuild.xml
```

This "preprocesses" the project and shows all imported targets in one file.

## Related: Import Statement in This Project

```xml
<Import
  Project="$(NuGetPackageRoot)grpc.tools\2.67.0\buildTransitive\Grpc.Tools.targets"
  Condition="Exists('...')"
/>
```

| Part | Meaning |
|------|---------|
| `$(NuGetPackageRoot)` | NuGet cache path (`~/.nuget/packages/` or `%USERPROFILE%\.nuget\packages\`) |
| `grpc.tools\2.67.0\` | Package name and version |
| `buildTransitive\` | Targets that apply to projects referencing this package |
| `Condition="Exists(...)"` | Only import if file exists (safe fallback) |

This import is required for old-style `.csproj` files. SDK-style projects import NuGet targets automatically.
