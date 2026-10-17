# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and releases should follow [Semantic Versioning](https://semver.org/).

This repository uses independent package versions; each release entry lists the affected packages and their versions. Comparison links require identified release refs. No comparison baseline is recorded for the first dated entry; use the repository history linked below.

## 2026-10-17

- `Fast.SqlSugar` 3.5.69 checks the current entity property name and fill conditions before evaluating audit, department and tenant assignment callbacks. Unrelated properties and populated insert fields no longer invoke those handlers unnecessarily; update user callbacks run only for the matching updater fields.
- Preserve null-only `CreatedTime` filling on object insertion, retaining caller-supplied creation timestamps. Existing empty-value rules for department, tenant, creator and row-version fields remain in effect; public declarations and dependency versions are unchanged.

### Request cancellation and Runtime dependency alignment

| PackageId | Previous version | New version |
| --- | --- | --- |
| Fast.Runtime | 3.5.31 | 3.5.32 |
| Fast.EventBus | 3.5.29 | 3.5.30 |
| Fast.Logging | 3.5.33 | 3.5.34 |
| Fast.UnifyResult | 3.5.35 | 3.5.36 |
| Fast.Cache | 3.5.34 | 3.5.35 |
| Fast.Consul | 3.5.10 | 3.5.11 |
| Fast.NET.Core | 3.5.40 | 3.5.41 |
| Fast.DependencyInjection | 3.5.30 | 3.5.31 |
| Fast.DynamicApplication | 3.5.36 | 3.5.37 |
| Fast.JwtBearer | 3.5.44 | 3.5.45 |
| Fast.Mapster | 3.5.28 | 3.5.29 |
| Fast.OpenApi | 3.5.40 | 3.5.41 |
| Fast.SqlSugar | 3.5.69 | 3.5.70 |
| Fast.Swagger | 3.5.35 | 3.5.36 |

- `Fast.Runtime` adds an optional cancellation token to `RemoteIpv4InfoAsync(HttpContext, string, CancellationToken?)`. A null token uses `HttpContext.RequestAborted`; an explicitly supplied token replaces that default rather than linking both tokens. Cancellation is checked before request-local cache access and propagated to lock waits and remote queries. A null HttpContext still returns null.
- The new three-parameter declaration replaces the previous two-parameter method. Ordinary calls can omit the new argument after recompilation, but previously compiled consumers must be rebuilt; method-group bindings and reflection calls must be reviewed. This compatibility change remains on the requested 3.5.x patch line.
- `Fast.EventBus` observes cancellation before the initial attempt, before each retry, and before retry/fallback handling after a failure, including immediate retries without a delay. Cancellation suppresses subsequent retries and fallback callbacks. Retry exhaustion and exception-type rejection share the existing failure path.
- `Fast.UnifyResult` leaves OperationCanceledException unhandled when RequestAborted is cancelled, allowing the upper pipeline to handle request cancellation instead of producing a normal business-error or 500 response. Other exception handling is unchanged.
- Runtime, EventBus and Logging use the standard null-argument helpers without changing their parameter checks. All packages referencing Runtime are rebuilt against Fast.Runtime 3.5.32; DynamicApplication also takes Fast.UnifyResult 3.5.36, and Swagger takes Fast.DynamicApplication 3.5.37. Third-party dependency versions are unchanged.
- `Fast.IaaS` 3.5.29, `Fast.Serialization.Newtonsoft.Json` 3.5.27 and `Fast.Serialization.System.Text.Json` 3.5.21 remain unchanged because this batch changes neither their source nor their package dependencies.

## 2026-10-16

- `Fast.SqlSugar` 3.5.68 removes `DepartmentName` from `PagedOutput`, so paging DTOs no longer inherit a department name by default. DTOs that still expose this field must declare it explicitly; replace overrides of the removed base property, update projections and generic member access, and recompile dependent assemblies. Default serialization no longer includes the inherited field unless the concrete DTO declares it. `IDepartmentEntity` and department assignment hooks are unchanged.
- `Fast.SqlSugar` 3.5.67 continues the 3.5.x patch release line and carries forward the department-entity changes previously recorded under 4.0.0: `IDepartmentEntity` declares `DepartmentId` and `DepartmentName`, while the general and record entity base types no longer supply them. Consumers requiring those entity fields must declare their properties and mappings explicitly; these public API changes still require migration despite the chosen patch version.
- `PagedOutput.DepartmentName` remains available in 3.5.67. Department assignment hooks, entity mappings and database columns retain the preceding implementation. Other package versions are unchanged.

## 2026-10-15

- `Fast.SqlSugar` 4.0.0 introduces `IDepartmentEntity` with `DepartmentId` and `DepartmentName`, and removes these properties from `BaseEntity`, `IBaseEntity`, `BaseRecordEntity`, and `IBaseRecordEntity`. This is a breaking change, including for entities inheriting from `BaseTEntity`. Entities that still require department fields must declare the properties and their `SugarColumn` mappings explicitly, and department-specific consumers must use the new interface or concrete entity contract. Existing database columns are not removed by this source change; review schema changes separately.
- Department assignment in `SugarEntityFilter` now references `IDepartmentEntity` member names. Assignment still matches property names, fills null/zero IDs and null/empty names on object insertion when a handler is available, and does not require the entity to implement `IDepartmentEntity`. No department query filter is added. `BaseRecordEntity` device and location columns now use `CreateTableFieldSort` values 971 through 976; `PagedOutput.DepartmentName` remains available.
- `Fast.JwtBearer` 3.5.44 and `Fast.Serialization.Newtonsoft.Json` 3.5.27 update their ASP.NET Core dependencies. Microsoft.AspNetCore.Authentication.JwtBearer and Microsoft.AspNetCore.Mvc.NewtonsoftJson now use 8.0.32 / 9.0.21 / 10.0.13 for net8.0 / net9.0 / net10.0 respectively. Other package versions are unchanged.

## 2026-10-08

- `Fast.JwtBearer` 3.5.43 returns 401 when `AuthorizeHandle` returns false or throws, and 403 when `PermissionHandle` returns false or throws. Cancellation propagates without calling failure callbacks. Endpoint and MVC rejection responses preserve optional callback bodies and Challenge/Forbid semantics independently of UnifyResult. The refresh location and authentication cache remain unchanged.
- Retain existing Exchange/AutoRefreshToken APIs, refresh cache configuration and JWT ClockSkew. No new rotation store or reuse-window configuration is required. Identity rejection removes pending new-token headers; permission denial does not discard a successful refresh.
- `Fast.NET.Core` 3.5.40 and `Fast.UnifyResult` 3.5.35 remove token-header-based 401-to-403 conversion. Unified status responses respect started responses and are written once.
- Update centrally managed dependencies: Mapster and Mapster.DependencyInjection 10.0.12 → 10.0.13, SqlSugarCore 5.1.4.220 → 5.1.4.221, and Swashbuckle.AspNetCore 10.2.3 → 10.3.0.
- Update Microsoft.AspNetCore.Authentication.JwtBearer and Microsoft.AspNetCore.Mvc.NewtonsoftJson to 8.0.31 / 9.0.20 / 10.0.12 for net8.0 / net9.0 / net10.0 respectively.

## 2026-09-22

### Documentation and Tooling

- Correct localized Fast.Docs links and retain minimal README examples.
- Align public-contract comments and agent guidance; keep licenses and `.editorconfig` unchanged.

### Package Versions

| PackageId                             | Version  |
| ------------------------------------- | -------- |
| `Fast.Cache`                          | `3.5.34` |
| `Fast.Consul`                         | `3.5.10` |
| `Fast.NET.Core`                       | `3.5.39` |
| `Fast.DependencyInjection`            | `3.5.30` |
| `Fast.DynamicApplication`             | `3.5.36` |
| `Fast.EventBus`                       | `3.5.29` |
| `Fast.IaaS`                           | `3.5.29` |
| `Fast.JwtBearer`                      | `3.5.42` |
| `Fast.Logging`                        | `3.5.33` |
| `Fast.Mapster`                        | `3.5.28` |
| `Fast.OpenApi`                        | `3.5.40` |
| `Fast.Runtime`                        | `3.5.31` |
| `Fast.Serialization.Newtonsoft.Json`  | `3.5.26` |
| `Fast.Serialization.System.Text.Json` | `3.5.21` |
| `Fast.SqlSugar`                       | `3.5.66` |
| `Fast.Swagger`                        | `3.5.35` |
| `Fast.UnifyResult`                    | `3.5.34` |

### Added

- `HttpRequestMethodEnum.Unknown` represents calls made without an active HTTP request context; `GetRequestMethod()` returns it instead of throwing when the context is unavailable.
- `Fast.Runtime` 3.5.29 adds `MAppContext.ConsoleWrite` with a `ConsoleWriter` callback for segmented text and color output, automatic foreground/background restoration, serialized synchronous callbacks, and nested writes. `Fast.IaaS` includes an internal writer while retaining its standalone `netstandard2.1` target.
- Central NuGet dependency management through `Directory.Packages.props`.
- GitHub CI for restore, dependency auditing, build, packaging, and artifact upload.
- A single Windows entry point for build, pack, optional all-package publishing, and optional single-package publishing.
- Bilingual getting-started, module, and release guides plus security and community policies.

### Changed

- `Fast.Runtime` 3.5.30 removes the misleading `HttpContext.LanIpv4()` and `HttpContext.LanIpv6()` extension methods. Use `LocalIpv4()`/`LocalIpv6()` for the server endpoint or `RemoteIpv4()`/`RemoteIpv6()` for the remote endpoint after trusted forwarded-header processing.
- Primary SDK packages now target .NET 8, .NET 9, and .NET 10. .NET 6 and .NET 7 assets are no longer produced.
- Repository builds use C# 14 and the .NET 10 SDK.
- `RetryUtil.InvokeAsync` now awaits retry callbacks, invokes the fallback for suppressed terminal failures, and accepts a cancellation token.
- Updated `Fast.NET.Core` from 3.5.35 to 3.5.36. The `builder.Initialize()` banner now displays application identity, application/framework informational and assembly versions, .NET runtime, environment, host, OS, OS/process architectures, and local startup time. Environment values use distinct colors; startup time is now `White`. The banner also adjusts spacing and adds a lawful-use reminder and learning messages while retaining `MAppContext.ConsoleWrite` color restoration and plain redirected output.
- Updated `Mapster` and `Mapster.DependencyInjection` to 10.0.12, `MiniExcel` to 1.45.0, `SQLitePCLRaw.bundle_e_sqlite3` to 3.0.5, `SqlSugarCore` to 5.1.4.220, and `Swashbuckle.AspNetCore` to 10.2.3.
- Updated ASP.NET Core package references to 8.0.30, 9.0.19, and 10.0.11 for their corresponding target frameworks.
- Migrated `Fast.Swagger` custom filters and security registration to the Microsoft.OpenApi 2.x API required by Swashbuckle 10.
- Established `FAST-AES-256-GCM-V1` as the initial cross-language password-based AES payload for `Fast.IaaS`.
- Simplified XML documentation by removing type references already conveyed by signatures, inheriting existing implementation contracts, and retaining links to related APIs and constraints.
- Marked internal leaf implementations and JSON converters as sealed where inheritance is not supported.
- Incremented 15 SDK package versions; the two serialization packages retain their existing versions. Package versions recorded by that earlier source baseline: `Fast.Cache` 3.5.33, `Fast.Consul` 3.5.9, `Fast.NET.Core` 3.5.38, `Fast.DependencyInjection` 3.5.29, `Fast.DynamicApplication` 3.5.35, `Fast.EventBus` 3.5.28, `Fast.IaaS` 3.5.28, `Fast.JwtBearer` 3.5.41, `Fast.Logging` 3.5.32, `Fast.Mapster` 3.5.27, `Fast.OpenApi` 3.5.39, `Fast.Runtime` 3.5.30, `Fast.Serialization.Newtonsoft.Json` 3.5.25, `Fast.Serialization.System.Text.Json` 3.5.20, `Fast.SqlSugar` 3.5.65, `Fast.Swagger` 3.5.34, and `Fast.UnifyResult` 3.5.33.
- Packages referencing `Fast.Runtime` now require 3.5.30 or later. Upgrade installed Fast.NET modules to the versions listed above together to keep transitive dependencies aligned.
- Incremented `I18nTranslateTool` to 1.0.1 and moved its `MiniExcel` version into central package management.

### Fixed

- Only certify Fast-owned authorization requirements; preserve third-party policies/handlers, deny missing Fast permission implementations, and retain failure state for non-MVC resources.
- Add IRefreshTokenReplayStore and a Redis SET NX implementation; generic distributed get/set no longer pretends to provide cross-instance atomic consumption. The memory implementation is process-local only.
- Preserve decimal JSON precision; require explicit ownership for background scoped services; configure the Redis options consumed by the cache directly.
- Align named DI with registered interface services and reject abstract/ambiguous automatic handler registrations; fail fast on nested writes to a full dispatch queue.
- Swap Redis clients only after successful construction; retain borrowed retired clients until cache disposal. Nonpaged SQL limits allow exactly N rows and probe N + 1.
- Propagate observed caller cancellation from RetryUtil before fallback handling, including finalThrow=false.
- `Fast.JwtBearer` 3.5.41 now keeps revoked access tokens blacklisted through the configured JWT `ClockSkew` window. Cache writes use the remaining relative lifetime and skip tokens whose `exp + ClockSkew` has already elapsed, preventing Redis from rejecting an absolute expiration that is no longer in the future.
- `Fast.Runtime` 3.5.30, `Fast.NET.Core` 3.5.38, `Fast.SqlSugar` 3.5.65, and `Fast.UnifyResult` 3.5.33 now handle a missing HTTP context outside the request lifecycle without null-reference failures. Remote addresses rely exclusively on `Connection.RemoteIpAddress` after trusted `UseForwardedHeaders` processing instead of reading forwarding headers directly, and record entities tolerate unavailable user-agent and IP metadata.
- `UploadNuget.bat` now uses Simplified Chinese CLI diagnostics within the script, classifies redirected UTF-8 NuGet output before converting it to CMD CP936, and displays skipped results in dark gray. With PowerShell unavailable, output remains plain text and a zero exit code is conservatively reported as a warning because detailed diagnostics cannot be classified.
- `Fast.IaaS` 3.5.27 now includes the correct unit when `TimeSpanExtension.ToDescription()` receives exactly one minute, one hour, or one day, producing `01分00秒`, `01时00分00秒`, or `01天00时00分00秒` respectively.
- `Fast.OpenApi` 3.5.38 now generates `Promise<void>` and calls `axiosUtil.request<void>` for actions returning non-generic `Task` or `ValueTask`. `Download` and `Export` actions retain their file response and expose an `autoDownloadFile` parameter that defaults to `true`: Web clients use `AxiosResponse<Blob>`, while mobile clients allow `AxiosResponse<Blob | ArrayBuffer | string>`. Missing response schemas for other value-returning actions continue to use `unknown`.
- `UploadNuget.bat` now checks NuGet diagnostics as well as exit codes, reports existing packages as skipped, separates warnings and partial symbol results from confirmed success, and groups all results with one package per line. Success is green, skips and warnings are yellow, and failures are red; redirected output remains plain text. Exit codes are `0` for success/skips only, `2` for warnings, and `1` for failures.
- Replaced bare ANSI output in startup banners and direct diagnostics starting with `Fast.NET.Core` 3.5.35, `Fast.Runtime` 3.5.29, `Fast.OpenApi` 3.5.37, `Fast.SqlSugar` 3.5.64, and `Fast.IaaS` 3.5.26 with `ConsoleWriter`. Redirected output remains plain text, and colors are restored even when output fails. The existing `Fast.Logging` formatter and `ConsoleLoggerProvider` pipeline are unchanged.
- Fast.IaaS tree building now supports partial node sets whose parent nodes are not included and uses parent lookups to avoid repeated collection scans during recursive construction.
- `Fast.Logging` now restores foreground and background state after independently colored log segments.
- `Fast.OpenApi` Web and mobile upload clients now expose an optional Axios `onUploadProgress` callback and forward it to the corresponding adapter.
- `Fast.OpenApi` TypeScript clients now emit separate type imports in rule-compliant order, explicit `Promise<T>` return types, and `unknown` for unspecified schemas to satisfy the current Fast ESLint Config rules.
- `Fast.DependencyInjection` now registers marked concrete services that do not expose a business interface.
- `Fast.JwtBearer` now accepts the standard `access_token` query parameter regardless of a SignalR hub's route prefix, preserves custom token extraction, and resolves the HTTP context during endpoint and hub-method authorization.
- WebSocket detection now relies on ASP.NET Core's WebSocket feature instead of treating every request to `/ws` as a WebSocket request.
- `Fast.OpenApi` now resolves duration, dictionary, collection, nested, and composed OpenAPI schemas to concrete TypeScript types and unwraps nullable schemas without emitting explicit `| null` unions.
- `Fast.OpenApi` now maps `Int64` and OpenAPI `int64` values, including nullable, collection, and dictionary forms, to TypeScript `string` to match the default JSON serialization behavior.
- Dynamic application discovery no longer inserts duplicate MVC application parts.
- Runtime dependency discovery now works under test and plugin hosts whose entry assembly has no adjacent `.deps.json` file.
- `Fast.Runtime` disposable cleanup now drains the concurrent collection without forcing a full garbage collection or discarding objects added during cleanup.
- `Fast.Runtime` remote IPv4 metadata lookups now use a bounded set of lock stripes, avoiding keyed-lock removal races and unbounded lock retention.
- `Fast.IaaS`, `Fast.DynamicApplication`, and `Fast.UnifyResult` async method inspection now recognizes `ValueTask`, `ValueTask<T>`, and compiler-generated async state machines.
- `Fast.IaaS`, `Fast.DynamicApplication`, `Fast.Swagger`, and `Fast.Logging` identifier casing and rolling-file numbering are now stable across server cultures.
- `Fast.Cache` named Redis contexts now expose the effective inherited key prefix used by the client.
- `Fast.EventBus` retries now stop immediately when host or event cancellation is requested.
- `Fast.Logging` now reports queue saturation and write failures, and safely handles file-name rule changes during rolling.
- Build and pack are separate operations; normal builds no longer emit NuGet packages implicitly.
- NuGet repository metadata now uses the canonical Gitee URL and the standard `git` repository type.

For changes before this changelog was introduced, see the [repository history](https://gitee.com/FastDotnet/Fast.NET/commits/master).
