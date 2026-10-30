# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and releases should follow [Semantic Versioning](https://semver.org/).

This repository uses independent package versions; each release entry lists the affected packages and their versions. Comparison links require identified release refs. No comparison baseline is recorded for the first dated entry; use the repository history linked below.

## Unreleased

### 2026-10-31 remove Runtime composite enum members

- Increment Runtime and all 13 referencing packages by one patch version so the internal dependency chain follows `Fast.Runtime` 3.5.36. IaaS, the two independent serialization packages and external dependency versions are unchanged.

| PackageId | Previous version | New version |
| --- | --- | --- |
| Fast.Cache | 3.5.38 | 3.5.39 |
| Fast.Consul | 3.5.14 | 3.5.15 |
| Fast.NET.Core | 3.5.45 | 3.5.46 |
| Fast.DependencyInjection | 3.5.34 | 3.5.35 |
| Fast.DynamicApplication | 3.5.40 | 3.5.41 |
| Fast.EventBus | 3.5.33 | 3.5.34 |
| Fast.JwtBearer | 3.5.49 | 3.5.50 |
| Fast.Logging | 3.5.37 | 3.5.38 |
| Fast.Mapster | 3.5.32 | 3.5.33 |
| Fast.OpenApi | 3.5.44 | 3.5.45 |
| Fast.Runtime | 3.5.35 | 3.5.36 |
| Fast.SqlSugar | 3.5.74 | 3.5.75 |
| Fast.Swagger | 3.5.39 | 3.5.40 |
| Fast.UnifyResult | 3.5.39 | 3.5.40 |

- Remove `AppEnvironmentEnum.Desktop`, `Mobile`, `MiniProgram` and `MobileThree`. Keep all 24 independent flags, their numeric values, the `long` underlying type and `[Flags]` unchanged.
- This is a breaking public API change despite retaining the requested 3.5.x patch cadence. Replace removed member references with explicit bitwise-OR masks in consumers and regenerate client enums. Former numeric masks retain their meaning when upgrading from 3.5.35; migrations from older versions must still follow the earlier numeric mapping.
- Review enum-name parsing, string serialization, display names and reflection: removed names are no longer declared, and combined values can format as constituent names. Compiled numeric constants may continue to work because they can be inlined; they do not preserve removed declaration names. No application, database migration or package publication is performed by this update.

### 2026-10-30 continuous Runtime platform flags

- Increment Runtime and all 13 packages that reference it by one patch version so their NuGet dependencies resolve to `Fast.Runtime` 3.5.35. IaaS and the two independent serialization packages are outside this dependency chain; external dependency versions remain unchanged.

| PackageId | Previous version | New version |
| --- | --- | --- |
| Fast.Cache | 3.5.37 | 3.5.38 |
| Fast.Consul | 3.5.13 | 3.5.14 |
| Fast.NET.Core | 3.5.44 | 3.5.45 |
| Fast.DependencyInjection | 3.5.33 | 3.5.34 |
| Fast.DynamicApplication | 3.5.39 | 3.5.40 |
| Fast.EventBus | 3.5.32 | 3.5.33 |
| Fast.JwtBearer | 3.5.48 | 3.5.49 |
| Fast.Logging | 3.5.36 | 3.5.37 |
| Fast.Mapster | 3.5.31 | 3.5.32 |
| Fast.OpenApi | 3.5.43 | 3.5.44 |
| Fast.Runtime | 3.5.34 | 3.5.35 |
| Fast.SqlSugar | 3.5.73 | 3.5.74 |
| Fast.Swagger | 3.5.38 | 3.5.39 |
| Fast.UnifyResult | 3.5.38 | 3.5.39 |

- Make all 24 independent `AppEnvironmentEnum` flags contiguous from bit 0 through bit 23. Android starts at `1L << 4`, followed by HarmonyOS at bit 5 and IOS at bit 6, through JDMiniProgram at bit 21, Api at bit 22 and Other at bit 23. Web, Windows, Mac and Linux retain their values.
- This is a breaking numeric-contract change despite the patch version. Compared with 3.5.34, Android through JDMiniProgram shift right one bit; Api changes from 4294967296 to 4194304 and Other from 8589934592 to 8388608. Mobile is now 112, MiniProgram is 4186496, MobileThree is 4186608 and Desktop remains 14.
- Migrate persisted values, numeric protocols and combined masks by member meaning using the source version's mapping; do not shift entire masks because lower flags and Api/Other have different offsets. Recompile consumers and regenerate client enums; replacing the Runtime DLL alone is insufficient. See the bilingual Runtime guide for the complete 3.5.34-to-3.5.35 mapping. This update does not perform a database migration or package publication.

### 2026-10-30 SqlSugar connection and public API migration

- Keep `Fast.SqlSugar` at the requested, unpublished 3.5.73 version. This update also carries breaking public API changes; its patch version does not imply source or binary compatibility.
- Remove `DbConnectionInfo`. `ConnectionSettingsOptions` now implements `IPostConfigure` directly, while `SlaveConnectionInfo` is an independent class without `PostConfigure` or `CustomConnectionStr`. Several previously virtual configuration members are no longer virtual; update inheritance, overrides and ORM mappings in consumers.
- Make `SqlSugarDatabaseUtil` internal and remove `ISqlSugarRepository<TEntity>.Entities`; build repository queries through `Queryable<TEntity>()`. Use `SqlSugarContext.GetConnectionConfig(ConnectionSettingsOptions)` for the public connection-config entry point.
- Restrict `SugarDbType` to MySql=1, SqlServer=2, Sqlite=3, Oracle=4, PostgreSQL=5 and MongoDb=6. Their previous values were 0, 1, 2, 3, 4 and 32 respectively. Other members are removed, and both conversion extensions reject unsupported values. Migrate persisted numeric values and generated clients by member meaning, and rebuild consumers; the separate `SqlSugar.DbType` configuration enum is unchanged.
- Add configurable pool limits to automatically generated MySql/MySqlConnector, SqlServer and PostgreSQL connection strings for primary and replica connections, with a fallback of 100. A nonblank `CustomConnectionStr` bypasses generated primary connection fields and suppresses configured replicas. Slave `CommandTimeOut` is not applied as a separate replica timeout.
- Document the current defaults and AOP fallbacks, and synchronize bilingual guides and generated API references. No database initialization, migration or data operation is required for this documentation update.

### 2026-10-30 dependency patch updates

| PackageId | Previous version | New version | Updated dependency |
| --- | --- | --- | --- |
| Fast.JwtBearer | 3.5.47 | 3.5.48 | Microsoft.AspNetCore.Authentication.JwtBearer |
| Fast.Serialization.Newtonsoft.Json | 3.5.27 | 3.5.28 | Microsoft.AspNetCore.Mvc.NewtonsoftJson |
| Fast.SqlSugar | 3.5.72 | 3.5.73 | System.Security.Cryptography.Pkcs |

- Keep the current central dependency versions: both ASP.NET Core packages specify 8.0.33 / 9.0.22 / 10.0.14 for net8.0 / net9.0 / net10.0. Pkcs retains 8.0.2 for net8.0 and updates to 9.0.22 / 10.0.14 for net9.0 / net10.0. Declared versions and the restored graph must be distinguished; restore and package validation results are recorded in the corresponding Fast.Docs module guides.
- Increment only the three directly affected packages on the requested patch release line. This dependency update does not modify module implementations or public declarations. Fast.Runtime remains at 3.5.34, and Fast.NET.Core retains the separate 3.5.44 static-file update below. No packages have been published as part of this update.

### 2026-10-30 configurable static-file MIME mappings

- Increment `Fast.NET.Core` from 3.5.43 to 3.5.44 on the requested patch release line.
- Add `UseStaticFiles(IApplicationBuilder, IConfiguration, string section = "StaticFileSettings")` to enable static-file middleware with MIME mappings from configuration. Keys are file extensions including the leading dot; values are MIME types. Configured entries supplement or override the default mappings, while other default mappings remain available.
- Clarify the XML documentation and synchronize the bilingual Fast.Docs guide and generated API reference. This source version has not been published; the existing `Fast.Runtime` 3.5.34 dependency is unchanged.

### 2026-10-24 package versions and dependencies

| PackageId | Previous version | New version |
| --- | --- | --- |
| Fast.Cache | 3.5.36 | 3.5.37 |
| Fast.Consul | 3.5.12 | 3.5.13 |
| Fast.NET.Core | 3.5.42 | 3.5.43 |
| Fast.DependencyInjection | 3.5.32 | 3.5.33 |
| Fast.DynamicApplication | 3.5.38 | 3.5.39 |
| Fast.EventBus | 3.5.31 | 3.5.32 |
| Fast.JwtBearer | 3.5.46 | 3.5.47 |
| Fast.Logging | 3.5.35 | 3.5.36 |
| Fast.Mapster | 3.5.30 | 3.5.31 |
| Fast.OpenApi | 3.5.42 | 3.5.43 |
| Fast.Runtime | 3.5.33 | 3.5.34 |
| Fast.SqlSugar | 3.5.71 | 3.5.72 |
| Fast.Swagger | 3.5.37 | 3.5.38 |
| Fast.UnifyResult | 3.5.37 | 3.5.38 |

- Increment Runtime and its dependent packages once within 3.5.x so their package dependency chain follows Runtime 3.5.34. IaaS and both standalone serialization packages are unchanged.
- `Fast.SqlSugar` 3.5.72 explicitly references `System.Security.Cryptography.Pkcs` 8.0.2 / 9.0.21 / 10.0.13 for net8.0 / net9.0 / net10.0 respectively. Remove the explicit `SQLitePCLRaw.bundle_e_sqlite3` reference and central 3.0.5 override; SQLite dependencies are resolved transitively through the unchanged SqlSugarCore 5.1.4.221 dependency. This update does not verify the resulting restored dependency graph or runtime behavior.
- `Fast.Swagger` 3.5.38 updates Swashbuckle.AspNetCore from 10.3.0 to 10.3.1; module implementation and public declarations are unchanged.
- These are unpublished source versions. Tests were skipped at the user's request; no restore, build, pack or consumer runtime validation was performed for this version update.

### Breaking changes

- `Fast.Runtime` 3.5.34: add `AppEnvironmentEnum.HarmonyOS = 64`. Keep Web, Windows, Mac, Linux and Android unchanged; shift each existing independent flag from IOS onward left by one bit, including Api and Other. IOS is now 128, QuickApp is 256, Api is 4294967296, and Other is 8589934592.
- Include HarmonyOS in Mobile (224) and MobileThree (8373216); MiniProgram is now 8372992 and Desktop remains 14. Existing persisted numeric values, combined masks and numeric request values must be migrated by their old member meanings. Recompile consumers and regenerate client enums together; replacing the DLL alone is insufficient because enum constants can be embedded in compiled code. Do not reinterpret old IOS=64 as HarmonyOS or blindly double complete masks, since lower flags remain unchanged.
- This breaking change retains the requested 3.5.x patch versioning. Adding the platform identifier does not implement HarmonyOS package upload, installation or hot-update support in consuming applications.

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

### Request-local IPv4 cache before cancellation checks

| PackageId | Previous version | New version |
| --- | --- | --- |
| Fast.Runtime | 3.5.32 | 3.5.33 |
| Fast.EventBus | 3.5.30 | 3.5.31 |
| Fast.Logging | 3.5.34 | 3.5.35 |
| Fast.UnifyResult | 3.5.36 | 3.5.37 |
| Fast.Cache | 3.5.35 | 3.5.36 |
| Fast.Consul | 3.5.11 | 3.5.12 |
| Fast.NET.Core | 3.5.41 | 3.5.42 |
| Fast.DependencyInjection | 3.5.31 | 3.5.32 |
| Fast.DynamicApplication | 3.5.37 | 3.5.38 |
| Fast.JwtBearer | 3.5.45 | 3.5.46 |
| Fast.Mapster | 3.5.29 | 3.5.30 |
| Fast.OpenApi | 3.5.41 | 3.5.42 |
| Fast.SqlSugar | 3.5.70 | 3.5.71 |
| Fast.Swagger | 3.5.36 | 3.5.37 |

- `Fast.Runtime` 3.5.33 returns an existing `HttpContext.Items` IPv4 lookup result before resolving or checking the cancellation token. A request-local cache hit therefore returns even if the default or explicit token is cancelled. On a request-local miss, cancellation is still checked before the shared memory cache, lock waits and remote query. A null context still returns null; token defaults, the three-parameter public declaration and the 24-hour memory-cache lifetime are unchanged from 3.5.32.
- Increment the Runtime dependency chain on the 3.5.x patch line. All referencing packages target Fast.Runtime 3.5.33; DynamicApplication also takes Fast.UnifyResult 3.5.37, and Swagger takes Fast.DynamicApplication 3.5.38. The other modules' own behavior and third-party dependency versions are unchanged. Fast.IaaS and both standalone serialization package versions remain unchanged.
- Synchronize bilingual documentation and API comments. This batch was reviewed from source only; no Fast.NET restore, build, pack or tests were run at the user's request. Earlier validation results apply to the preceding batch and do not validate these new versions.

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
