# Extension: WCF to gRPC Fallback for Authorization Service

This document describes the implementation of gRPC fallback support for the authorization/activation service in SSKA Analyzer.

## Overview

The authorization logic has been updated to use gRPC as a fallback when the WCF service is unavailable. This provides resilience when one service endpoint is down.

## New Files Created

| File | Description |
|------|-------------|
| `WpfApplication1/Services/IAuthorizationService.cs` | Interface and DTOs for authorization operations |
| `WpfApplication1/Services/WcfAuthorizationService.cs` | WCF service wrapper implementing `IAuthorizationService` |
| `WpfApplication1/Services/GrpcAuthorizationService.cs` | gRPC service implementation using `SSKA.Grpc` client |
| `WpfApplication1/Services/FallbackAuthorizationService.cs` | Fallback service that tries WCF first, then gRPC |
| `WpfApplication1/Protos/authorization.proto` | Protocol buffer definition (auto-generates client code on build) |

## Modified Files

| File | Changes |
|------|---------|
| `WpfApplication1/WindowActivation.xaml.cs` | Updated to use `FallbackAuthorizationService` instead of direct WCF calls |
| `WpfApplication1/WpfApplication1.csproj` | Added gRPC NuGet packages and new source files |
| `WpfApplication1/Properties/Settings.settings` | Added `GrpcServerAddress` setting |
| `WpfApplication1/Properties/Settings.Designer.cs` | Added `GrpcServerAddress` property |
| `WpfApplication1/app.config` | Added `GrpcServerAddress` application setting |

## NuGet Packages Added

- `Google.Protobuf` (3.28.3)
- `Grpc.Net.Client` (2.67.0)
- `Grpc.Net.Client.Web` (2.67.0)
- `Grpc.Core.Api` (2.67.0)
- `Grpc.Tools` (2.67.0) - generates client code from proto at build time

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    WindowActivation.xaml.cs                  │
│                              │                               │
│                              ▼                               │
│                 FallbackAuthorizationService                 │
│                      │              │                        │
│                      ▼              ▼                        │
│         WcfAuthorizationService   GrpcAuthorizationService   │
│                      │              │                        │
│                      ▼              ▼                        │
│              ServARCodeClient    SSKA.Grpc.Client            │
│                      │              │                        │
│                      ▼              ▼                        │
│         WCF Service (HTTPS)    gRPC Service (HTTPS)          │
│      vizdr.somee.com/WcfServARR   vizdr.somee.com            │
└─────────────────────────────────────────────────────────────┘
```

## Fallback Behavior

1. `FallbackAuthorizationService` first attempts to use the WCF service (`ServARCodeClient`)
2. If WCF fails with a connection error (timeout, communication error, unavailable), it falls back to gRPC
3. If WCF returns a business error (not a connection error), that error is returned without fallback
4. The UI status messages indicate which service was used: `(WCF)` or `(gRPC)`

## Configuration

The gRPC server address is configurable via application settings:

```xml
<setting name="GrpcServerAddress" serializeAs="String">
    <value>https://vizdr.somee.com</value>
</setting>
```

Default value: `https://vizdr.somee.com`

## Service Interface

```csharp
public interface IAuthorizationService : IDisposable
{
    AuthorizationResult TryToRegisterAuthRequest(AuthorizationRequestData request);
    AuthorizationResult GetAuthCode(int accountId, string authRequestCode);
    bool IsAvailable();
}
```

## gRPC Proto Definition

The gRPC service is defined in `authorization.proto` with the following operations:

- `TryToRegisterAuthRequest` - Register an authorization request
- `GetAuthCode` - Get the authorization code for a paid account
- `CheckHealth` - Health check endpoint

## gRPC Client Code Generation

The gRPC client code is auto-generated from `WpfApplication1/Protos/authorization.proto` at build time using `Grpc.Tools`.

Generated files are placed in `obj\$(Configuration)\Protos\`:
- `Authorization.cs` - Protobuf message classes
- `AuthorizationGrpc.cs` - gRPC client stub

The proto definition is shared with the SSKA Admin gRPC server to ensure compatibility.
