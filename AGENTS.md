# Repository guidelines

## Project purpose

This repository contains `Megaraz.ResultPattern.AspNetCore`, the public NuGet
extension package that maps `Megaraz.ResultPattern` results to HTTP response
descriptors and provides optional ASP.NET Core MVC and minimal-API adapters.
It should make HTTP integration predictable without taking ownership of an
application's routing, authentication, localization, or public API design.

## Package boundaries

- This package depends on `Megaraz.ResultPattern`; the core package must never
  depend on this package.
- Keep HTTP mapping policy separate from framework adapters. Prefer the pure
  `HttpResultMapper` as the central behavior and keep MVC/minimal-API
  extensions thin.
- Do not create endpoints, impose application-specific error schemas, or move
  business and authorization policy into this library.
- Keep HTTP-specific types and dependencies here. Persistence and logging
  integrations belong in infrastructure packages.
- Avoid dependencies beyond the core package, the ASP.NET Core shared
  framework, and tooling packages unless they provide substantial value.

## Public contracts and safety

- Treat public response fields, JSON names, error codes, status-code mappings,
  headers, and documented defaults as compatibility-sensitive API.
- Preserve the distinction between the framework-neutral `ErrorType` and
  HTTP-specific `HttpErrorType`. HTTP and transport errors remain specialized
  external errors rather than expanding the core package with HTTP concerns.
- Keep custom behavior explicit through `HttpResultMappingPolicy` and
  `HttpResponseMappingOptions`; defaults should remain safe and predictable.
- Do not expose untrusted upstream response text or exception details through
  user-facing messages by default. Applications must explicitly opt in to
  proxying vetted messages.
- Preserve cancellation rather than converting it into a failure result.
  Keep inbound response reading bounded, and make ownership or consumption of
  response content clear.
- Keep success, created, no-content, validation, and non-validation response
  behavior consistent across the pure mapper, MVC adapters, and minimal-API
  adapters.

## Technical constraints

- The package and tests currently target `net8.0`, `net9.0`, and `net10.0`.
  Do not change target frameworks without explicitly reporting the
  compatibility and packaging impact.
- Keep nullable reference types and implicit usings enabled. Treat compiler
  and analyzer warnings seriously.
- Preserve semantic versioning. Report any source, binary, behavioral, or
  wire-contract compatibility risk before completing a public API change.
- Keep the minimum compatible `Megaraz.ResultPattern` package dependency
  deliberate; do not replace the package reference with a local project
  reference in the shipping library.
- Add XML documentation for public APIs and update the README when mappings,
  response contracts, defaults, security behavior, or examples change.
- Keep NuGet metadata, release notes, Source Link settings, and the packaged
  README accurate when preparing a release.

## Testing and packaging

Add or update tests for new public behavior and regressions. Cover status
codes, bodies, headers, policy overrides, cancellation, malformed or oversized
responses, and adapter parity when relevant. Tests should assert observable
contracts rather than implementation details.

Before completing implementation work, run:

1. `dotnet restore Megaraz.ResultPattern.AspNetCore.slnx`
2. `dotnet build Megaraz.ResultPattern.AspNetCore.slnx --configuration Release --no-restore`
3. `dotnet test Megaraz.ResultPattern.AspNetCore.slnx --configuration Release --no-build`
4. `dotnet pack Megaraz.ResultPattern.AspNetCore/Megaraz.ResultPattern.AspNetCore.csproj --configuration Release --no-build --no-restore`

Report failures, warnings, skipped validation, and public API, wire-contract,
dependency, or package changes.

## Working style

- Start non-trivial tasks by reading the relevant implementation, tests,
  README, and project metadata, then present a short implementation plan.
- Make focused changes and preserve the repository's existing style.
- Do not reformat unrelated files or overwrite unrelated work in a dirty
  worktree.
- Prefer simple policy objects and composition over inheritance or
  application-specific hooks.
- Summarize changed files, design decisions, verification performed, and any
  compatibility or security considerations.
