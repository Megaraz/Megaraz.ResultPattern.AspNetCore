# Code Review Standard

This document defines the review standard for the following public .NET NuGet
packages:

- `Megaraz.ResultPattern`
- `Megaraz.ResultPattern.AspNetCore`
- `Megaraz.ResultPattern.Infrastructure`

It is a reusable review specification, not a record of one particular review.
Use it for pull requests, release-readiness reviews, public API changes,
dependency upgrades, security reviews, and periodic repository health checks.

The goal is production-quality open-source libraries that are safe to adopt,
easy to understand, stable to upgrade, and credible as published portfolio
projects.

## Review objectives

A review must evaluate more than whether the code compiles. It must determine
whether the change:

- Solves the stated problem correctly.
- Belongs in the package being changed.
- Preserves documented behavior and package boundaries.
- Provides a small, intentional, and maintainable public API.
- Is secure by default and does not disclose sensitive information.
- Remains flexible without introducing unnecessary abstraction.
- Preserves source, binary, behavioral, wire-format, and package compatibility.
- Works across every supported target framework.
- Includes sufficient tests, documentation, package metadata, and release
  information.
- Can be safely consumed by applications outside these repositories.

Prefer concrete findings backed by evidence over speculative concerns or style
preferences.

## One package family, three repositories

Treat the three repositories as parts of one product family rather than three
unrelated projects. A consumer should experience them as coherent layers of the
same library.

Unless a package-specific responsibility requires a difference, keep the
following aligned:

- Project layout, filenames, namespaces, and code organization.
- C# language features, formatting, naming, nullability, analyzer policy, and
  warning policy.
- Public API terminology and equivalent method or option shapes.
- Error, result, validation, cancellation, and exception conventions.
- README structure, tone, terminology, installation guidance, examples, and
  versioning statements.
- XML documentation quality and wording for equivalent concepts.
- Target frameworks and the policy for adding or dropping them.
- Test frameworks, test naming, project layout, and supported-TFM coverage.
- NuGet metadata, Source Link, symbols, package contents, and release notes.
- CI, package validation, security controls, and release workflow structure.
- Repository health files and contributor experience.

When equivalent behavior exists in more than one package, reviewers must compare
the sibling implementation and documentation. Prefer the same convention unless
the difference is deliberate, useful to consumers, and documented.

A change to a shared concept is not fully reviewed until its effect on sibling
packages has been considered. Examples include:

- A core API or invariant used by either extension package.
- Error codes, messages, result behavior, or validation semantics.
- Target frameworks or minimum package versions.
- Package metadata, documentation conventions, and release processes.
- Naming, serialization, or policy-object patterns consumers encounter across
  the family.

Do not create superficial consistency by moving package-specific concerns into
core. Consistency must preserve the dependency boundaries below. If a justified
difference remains, document why the packages differ and how consumers should
use each one.

## Package family and dependency direction

The intended dependency direction is:

```text
Megaraz.ResultPattern
├── Megaraz.ResultPattern.AspNetCore
└── Megaraz.ResultPattern.Infrastructure
```

### `Megaraz.ResultPattern`

The core package is framework-agnostic.

It must not depend on:

- ASP.NET Core or HTTP abstractions.
- Persistence providers or infrastructure integrations.
- Logging frameworks.
- Application-specific contracts, policies, or error catalogs.
- Either extension package in this family.

It owns the core result, error, validation, error-code, and error-context
contracts.

### `Megaraz.ResultPattern.AspNetCore`

This package may depend on `Megaraz.ResultPattern` and the ASP.NET Core shared
framework.

It owns:

- HTTP-specific error classifications.
- Framework-neutral result-to-HTTP mapping.
- HTTP response-to-result mapping.
- ASP.NET Core MVC and minimal-API adapters.
- HTTP response contracts explicitly documented as package defaults.

It must not own:

- Application routing or endpoint design.
- Authentication or authorization policy.
- Application-specific error schemas.
- Localization policy.
- Persistence or infrastructure integrations.
- Business rules.

Keep pure mapping behavior separate from thin MVC and minimal-API adapters.

### `Megaraz.ResultPattern.Infrastructure`

This package may depend on `Megaraz.ResultPattern`.

It owns reusable, provider-neutral infrastructure error concepts and narrowly
defined infrastructure integrations.

It must not depend on:

- `Megaraz.ResultPattern.AspNetCore`.
- ASP.NET Core, HTTP, endpoint, authentication, or presentation policy.
- The original monolith or application-owned interfaces.
- A persistence provider unless a separate, explicit provider integration
  requires and documents that dependency.

Provider-specific behavior must remain isolated from general abstractions.

## Reviewer operating rules

Before reviewing:

1. Read the issue, pull request description, or requested outcome.
2. Inspect the complete diff and the surrounding implementation.
3. Read affected tests, project files, README sections, XML documentation, CI
   workflows, and release metadata.
4. Check the working tree so unrelated or pre-existing changes are not
   attributed to the change under review.
5. Compare affected contracts with the current published package behavior.
6. Inspect sibling packages when boundaries or shared conventions are relevant.
7. Identify affected target frameworks and consumer scenarios.
8. Run the required validation when the environment permits it.

Do not:

- Approve a change merely because tests pass.
- Request abstractions without a concrete extension or maintenance need.
- Treat personal style preferences as defects.
- Move application policy into a reusable package for convenience.
- Hide compatibility risks behind refactoring terminology.
- Assume exception messages, upstream payloads, paths, SQL, headers, or log
  content are safe to expose.
- Silently ignore warnings, skipped tests, failed commands, or unavailable
  validation.
- Modify code during a review unless implementation was explicitly requested.

## Evidence and finding quality

Every actionable finding must contain:

- **Severity:** `P0`, `P1`, `P2`, or `P3`.
- **Title:** A concise statement of the defect or risk.
- **Location:** File and the smallest useful line range.
- **Evidence:** What the code demonstrably does.
- **Impact:** The consumer-visible, security, compatibility, or maintenance
  consequence.
- **Recommendation:** Focused remediation without unnecessary design demands.
- **Acceptance criteria:** An observable condition proving resolution.

A finding must identify a real defect, regression, undocumented compatibility
risk, missing release requirement, or credible failure mode.

Do not report hypothetical problems that require unlikely assumptions and have
no meaningful consumer impact. When no actionable findings exist, say so
explicitly and list residual risks or validation that could not be performed.

## Severity model

### P0 — Release blocker

Use when the change must not be published or merged in its current state.

Examples:

- Credential, personal-data, connection-string, SQL, filesystem, stack-trace,
  or other sensitive-data exposure.
- Remote code execution, injection, path traversal, unsafe deserialization, or
  an equivalent critical vulnerability.
- Package cannot restore, build, test, pack, or load for a supported target.
- Shipping package is missing its primary assembly or contains secrets.
- Fundamental public contract is unusable or corrupts consumer data.
- Release workflow can publish unreviewed or unverified artifacts.
- License or ownership prevents legal distribution.

### P1 — Must fix before release

Use for serious correctness, compatibility, security, or boundary problems.

Examples:

- Breaking change without the required versioning treatment.
- Public API permits invalid states or violates documented invariants.
- Cancellation is swallowed or converted into an unrelated failure.
- Unbounded reads or unsafe concurrency can cause material production failure.
- User-facing output exposes untrusted external or exception detail by default.
- Missing tests for important public behavior.
- Reverse or forbidden package dependency.
- Incorrect status, response body, header, serialization, or error-code
  behavior.
- Target-framework or dependency change excludes consumers without a
  documented decision.

### P2 — Should fix

Use for meaningful maintainability, extensibility, reliability, performance,
documentation, or developer-experience concerns.

Examples:

- A public API is unnecessarily difficult to extend or compose.
- Resource ownership or disposal behavior is unclear.
- Edge cases or malformed input are handled inconsistently.
- A dependency is avoidable or leaks transitively without reason.
- Documentation omits important defaults, exceptions, limits, or ownership.
- Package metadata or release notes are inaccurate.
- CI lacks a relevant compatibility, package-content, or security check.
- Equivalent sibling-package behavior is needlessly inconsistent.

### P3 — Improvement

Use for low-risk quality improvements that do not block a release.

Examples:

- Minor API naming or discoverability issue before API stabilization.
- Missing example for a non-obvious but correctly documented feature.
- Small inconsistency with established family conventions.
- Test readability or diagnostic-output improvement.

Base severity on impact, likelihood, affected consumers, and recoverability.

## Review areas

### 1. Scope, architecture, and package boundaries

Verify that:

- The change addresses the requirement without unrelated redesign.
- The feature belongs in the selected package.
- Core remains framework-agnostic.
- Extension packages depend on core, never the reverse.
- ASP.NET Core and infrastructure concerns remain separated.
- Application-specific policy stays in the consuming application.
- Provider-specific behavior is isolated.
- New abstractions represent generally reusable behavior.
- Placeholders and extraction artifacts are not exposed as accidental API.
- The simplest design satisfying the requirement was preferred.
- Composition is preferred unless inheritance is a deliberate extension
  contract.

Flag convenience APIs that silently take ownership of policy the application
should control.

### 2. Cross-repository consistency

Compare the affected repository with both siblings and verify that:

- Equivalent concepts use the same names and semantics.
- Shared conventions are implemented consistently.
- Code and test layout follow the same recognizable pattern.
- README sections and examples are structured consistently.
- NuGet metadata and release workflows have equivalent quality.
- Target-framework and dependency decisions are coordinated.
- Differences are caused by package responsibility, not repository drift.
- A core change is validated against both extension packages.
- Documentation links and package references point to compatible releases.
- Consumers can move between packages without learning conflicting conventions.

Prefer a family-wide convention over three local conventions. Do not duplicate
code across packages merely for visual similarity; reuse must respect package
boundaries and dependency direction.

### 3. Public API design

Review every added or changed `public` or `protected` member as a long-lived
consumer contract.

Verify:

- The API has a clear, necessary use case.
- Names follow established .NET conventions and sibling terminology.
- The API is discoverable without excessive overloads.
- Invalid states are prevented or rejected at construction boundaries.
- Nullability annotations match runtime behavior.
- Generic constraints accurately express supported values.
- Mutability is deliberate and documented.
- Records, classes, structs, interfaces, and enums are chosen intentionally.
- Returned collections communicate ownership and mutability.
- Enumerables are materialized when deferred execution is unpredictable.
- Exceptions are appropriate, deterministic, and documented.
- Optional parameters do not create unsuitable permanent defaults.
- Extension methods have clear namespaces and no ambiguous overloads.
- Inheritance and virtual members are supported as real extension points.
- Public APIs have complete and accurate XML documentation.
- Implementation-only or provider-specific types do not leak unnecessarily.

Before adding an interface, factory, strategy, or policy object, identify the
consumer variation it enables.

### 4. Compatibility and semantic versioning

Classify each change for:

- **Source compatibility:** Will consumer source still compile?
- **Binary compatibility:** Will compiled consumers still load and run?
- **Behavioral compatibility:** Are meaning, defaults, ordering, exceptions,
  and side effects preserved?
- **Wire compatibility:** Are JSON fields, statuses, headers, error codes, and
  serialized values preserved?
- **Dependency compatibility:** Can consumers resolve a compatible graph?
- **Target-framework compatibility:** Are previous consumers still supported?
- **Package compatibility:** Are ID, assembly identity, assets, and transitive
  behavior stable?
- **Persistence compatibility:** Are stored formats still readable, if any?

Treat these as compatibility-sensitive:

- Public types, members, constructors, and method signatures.
- Generic constraints and nullability contracts.
- Enum numeric values and meanings.
- Error codes and their meanings.
- Documented messages and defaults.
- Result invariants.
- HTTP status mappings, response fields, casing, and headers.
- Serialization and persistence formats.
- Cancellation and exception behavior.
- Package dependencies and their minimum versions.
- Supported target frameworks.

Require:

- Semantic-version impact to be stated.
- A migration path for intentional breaking changes.
- A major version for breaking changes after stable publication.
- Compatibility tests where inspection is insufficient.
- Deliberate API baseline tooling for stable releases where practical.

Adding an enum member, tightening validation, changing an exception type, or
changing nullability may be breaking even if a method signature looks similar.

### 5. Correctness and invariants

Verify:

- Success and failure states remain mutually consistent.
- A successful typed result cannot contain a null value.
- Failed results expose a meaningful non-`None` primary error.
- Validation failures retain their validation-error invariants.
- Error codes remain stable and machine-readable.
- Technical descriptions and user-facing messages stay distinct.
- Mappings and factories cover every relevant case.
- Unknown enum and future extension values fail safely.
- Equality and record behavior suit the contract.
- Empty, malformed, duplicate, and boundary values are deliberate.
- Exceptions are not silently swallowed.
- Expected failures use result flow without hiding programming defects.
- No partial state is returned after failure.
- Defaults match documentation and examples.

### 6. Security, privacy, and safe defaults

Assume exceptions, upstream responses, HTTP headers, database errors,
filesystem content, and caller-controlled strings are untrusted.

Verify:

- Exception messages and stack traces are not public by default.
- SQL, connection strings, credentials, tokens, paths, headers, and internal
  host information cannot leak through public messages or serialized output.
- `Error.Description` and `Error.Exception` remain diagnostic channels.
- `UserMessage` and public HTTP bodies contain intentionally safe content.
- Upstream response text requires explicit vetting before proxying.
- Inputs used in paths, URLs, headers, logs, or output are validated or encoded.
- Reads and collections have explicit bounds for attacker-controlled input.
- Deserialization is bounded and does not enable unsafe type activation.
- Logging avoids injection, secrets, and uncontrolled cardinality.
- Temporary files and artifacts do not expose sensitive content.
- Security-sensitive defaults are conservative.
- CI and release workflows use least-privilege permissions.
- Secrets are never printed, packaged, committed, or exposed to pull requests.
- Third-party actions and build dependencies are deliberate and maintained.

If diagnostic data may be sensitive, document ownership of redaction, storage,
access, and retention.

### 7. Async, cancellation, concurrency, and resource ownership

Verify:

- Asynchronous I/O accepts and propagates `CancellationToken` where appropriate.
- Caller cancellation is not converted into a normal error.
- `OperationCanceledException` is not swallowed.
- Libraries avoid `.Result`, `.Wait()`, and sync-over-async.
- Streams, responses, files, and disposables have clear ownership.
- Methods document whether they consume or dispose caller resources.
- Multiple enumeration and deferred I/O are avoided.
- Concurrent access cannot corrupt shared state.
- Static mutable state is avoided or safely synchronized.
- Locks do not span asynchronous waits.
- File operations define encoding, atomicity, corruption, and concurrency.
- Retries, timeouts, and backoff remain explicit application policy unless the
  package owns them.
- Failure paths do not leak resources.
- Cleanup is deterministic.

### 8. Modern C# and .NET quality

Verify:

- Nullable reference types remain enabled and warnings are understood.
- Implicit usings remain intentional.
- Language features work for every targeted TFM.
- Modern syntax improves clarity rather than merely appearing current.
- Argument validation uses clear BCL helpers where appropriate.
- Pattern matching, records, required members, collection expressions, and
  primary constructors suit the contract.
- Public mutable state is minimized.
- Culture-sensitive comparison and formatting are explicit.
- String comparisons use an intentional `StringComparison`.
- Time uses an appropriate abstraction and UTC for absolute instants.
- Numeric overflow, ranges, and enum validation are considered.
- Reflection, dynamic code, trimming, and AOT behavior are avoided or tested.
- Platform-specific APIs are guarded or isolated.
- Analyzer suppressions are narrow and justified.
- No dependency replaces a straightforward BCL solution.
- Code remains readable without comments that merely restate it.

Modern code has clear contracts, safe defaults, useful compiler assistance, and
low accidental complexity. It need not use the newest syntax everywhere.

### 9. ASP.NET Core and HTTP behavior

For `Megaraz.ResultPattern.AspNetCore`, verify:

- Pure mapping logic remains testable without hosting an application.
- MVC and minimal-API adapters have equivalent observable behavior.
- Status codes correctly represent mapped results.
- `200`, `201`, and `204` handle bodies consistently.
- `Location` is preserved for created responses.
- Failure DTOs are explicit, immutable, safe, and documented.
- JSON names, casing, omission, and collection shapes remain stable.
- Public error bodies do not expose technical descriptions by default.
- Status and body customization is explicit and composable.
- Applications can replace defaults without rewriting the mapper.
- Incoming content has a configurable maximum size.
- Content consumption and ownership are documented.
- Serializer options and upstream error extraction are customizable.
- Empty, malformed, oversized, non-JSON, and unexpected responses are covered.
- Transport exceptions are mapped through an explicit boundary.
- Caller cancellation is preserved.
- Authentication, authorization, routing, and localization remain application
  responsibilities.
- Adapters do not discard headers or metadata from the pure mapper.

Treat documented HTTP defaults and response DTOs as wire contracts.

### 10. Infrastructure behavior

For `Megaraz.ResultPattern.Infrastructure`, verify:

- Infrastructure errors use `ErrorType.External`.
- Detailed infrastructure classification stays in this package.
- Provider exceptions do not leak into provider-neutral contracts.
- Provider integrations are separated and clearly named.
- Exception text is not used as a public message.
- Database, filesystem, and operational details remain diagnostic.
- Applications control logging, retention, redaction, retry, and operations.
- File and persistence APIs define ownership, encoding, bounds, cancellation,
  concurrency, corruption, and failure behavior.
- No hidden global logging or storage side effect is introduced.
- The package does not collect application-specific repository, unit-of-work,
  or logging abstractions.

### 11. Performance and allocation behavior

Evaluate performance in proportion to likely usage.

Verify:

- Hot paths avoid clearly unnecessary allocations.
- Collections are sized or streamed appropriately.
- Large or untrusted inputs are not buffered without a bound.
- LINQ does not obscure repeated or expensive enumeration.
- Regex, reflection, serialization options, and expensive objects are not
  recreated unnecessarily.
- Async methods provide real asynchronous value.
- Caches are bounded, thread-safe, and correctly invalidated.
- Optimizations do not weaken correctness or API clarity.
- Performance claims use representative benchmarks.

Do not require micro-optimizations without a credible workload or measurement.

### 12. Tests

New or changed observable behavior requires tests.

Verify:

- Tests target every supported TFM unless documented constraints prevent it.
- Tests assert public behavior rather than private implementation details.
- Happy paths and important failures are covered.
- Regression tests fail before the fix and pass afterward.
- Boundary, null, malformed, oversized, cancellation, and concurrency behavior
  is covered when relevant.
- Tests are deterministic and independent of order.
- Tests do not rely on the public internet, machine state, current time, locale,
  or race-prone delays.
- Temporary resources are isolated and cleaned up.
- Error codes, enum values, serialization shapes, and defaults have
  compatibility coverage where appropriate.
- MVC, minimal-API, and pure-mapper parity is tested where relevant.
- Exception and cancellation assertions distinguish expected behavior.
- Test names describe observable scenarios and outcomes.
- Coverage metrics support rather than replace test review.

Package-specific coverage includes:

#### Core

- Result construction and invariants.
- Typed and non-typed success and failure.
- Validation aggregation.
- Error-code and error-context validation.
- Stable reason-code behavior.
- Nullability, generic constraints, and extensions.

#### ASP.NET Core

- Every default status mapping and custom policy.
- Success, created, no-content, and failure bodies.
- `Location` behavior.
- Inbound success and failure mapping.
- Empty, malformed, non-JSON, and oversized bodies.
- Custom serialization and error extraction.
- Transport exceptions and cancellation.
- MVC and minimal-API parity.
- Pagination boundaries.

#### Infrastructure

- Error factories and invariants.
- Stable database-error codes and classifications.
- Exception retention without public disclosure.
- Custom classifications and provider-neutral behavior.
- Serialization compatibility if errors are serialized.
- Filesystem, cancellation, retention, corruption, and concurrency behavior for
  future file-based features.

### 13. Dependencies and target frameworks

Verify:

- Every runtime dependency provides substantial value.
- Development dependencies use `PrivateAssets="All"` where appropriate.
- Package dependencies state a deliberate minimum compatible version.
- Exact versions and upper bounds are avoided unless required and documented.
- The minimum dependency version consumers may restore is tested.
- Transitive dependencies do not leak unwanted implementation packages.
- Dependency licenses are compatible.
- Known vulnerabilities and diamond-dependency risks are reviewed.
- Core remains the only family runtime dependency of an extension unless
  another dependency is essential.
- Supported TFMs are intentional, documented, and aligned where appropriate.
- Implementation behaves consistently on every TFM.
- Adding or dropping a TFM includes compatibility and release analysis.
- Newer TFMs provide value rather than needlessly reducing reach.
- Platform-specific APIs do not break apparently portable packages.

Do not recommend `.NET Standard` automatically. Choose TFMs from actual
consumer requirements and document the decision.

### 14. NuGet package quality

Inspect the generated `.nupkg` and symbol package when applicable.

Verify:

- ID, version, title, description, authors, tags, copyright, and repository URL
  are accurate and consistent across the family.
- License metadata matches the repository license.
- The README is included and renders correctly on NuGet.org.
- Release notes or changelog accurately describe the version.
- Repository metadata and commit provenance are present.
- Source Link works and symbols follow release policy.
- Builds are deterministic and suitable for CI.
- Only intended assemblies and content are packaged.
- Tests, local paths, secrets, and repository-only files are excluded.
- Dependencies and TFM asset groups match the contract.
- XML documentation is included when intended.
- Package size is reasonable.
- The package installs into a clean consumer project.
- A documented minimal example compiles and runs.
- Package validation has no unexplained compatibility errors.
- The verified CI artifact is the artifact published.
- Published versions are immutable and prereleases are marked.
- Deprecation guidance exists where appropriate.

Consider automated package validation and public API compatibility checks before
the first stable `1.0.0` release.

### 15. Open-source repository readiness

Verify:

- `README.md` explains purpose, non-goals, installation, frameworks, usage,
  customization, compatibility, and security-sensitive behavior.
- `LICENSE` exists and matches package metadata.
- `CONTRIBUTING.md` contains accurate setup and test guidance.
- A security policy explains private vulnerability reporting.
- Issue and pull-request guidance is clear.
- Repository and package links are valid.
- CI runs on pull requests and the release branch.
- Release automation is documented and reproducible.
- Generated files and IDE artifacts are ignored.
- No secrets, personal data, or monolith artifacts remain.
- Copyright and attribution are accurate.
- Examples compile against the published API.
- Package and application responsibilities are distinguished.
- Project claims are true of the current release.
- All three repositories offer a consistent contributor and consumer
  experience.

Recommended health files include:

- `README.md`
- `LICENSE`
- `CONTRIBUTING.md`
- `SECURITY.md`
- `CHANGELOG.md` or generated release notes
- Pull-request and issue templates
- Dependency-update configuration
- CI and trusted release workflows

Information may be combined when it remains clear and reliably discoverable.

### 16. CI, release, and supply-chain safety

Verify:

- CI restores, builds, tests, and packs in Release configuration.
- CI covers every supported TFM.
- Warnings are visible and unexplained warnings fail the appropriate gate.
- Pull requests use read-only workflow permissions unless more is essential.
- Workflow permissions are explicitly minimized.
- Third-party actions are trusted and version-pinned per repository policy.
- Untrusted code cannot access release credentials.
- Publishing occurs only from intentional tags or approved releases.
- Package version matches the tag and release notes.
- Publishing consumes verified artifacts when practical.
- NuGet publishing uses short-lived or trusted credentials where available.
- Secrets are scoped to the publishing environment.
- Package contents are inspected before publication.
- Duplicate publication fails safely.
- The release does not rebuild different bits after verification.
- Dependency review and vulnerability scanning are enabled where practical.
- Provenance and Source Link identify the source revision.
- Failed validation prevents publication.
- Published NuGet versions are treated as immutable; fixes use a new version.
- Equivalent CI and release policy is maintained across all three repositories.

### 17. Documentation and developer experience

Verify:

- Public APIs have useful XML summaries, parameters, returns, exceptions, and
  remarks where behavior is non-obvious.
- README examples compile and use the current API.
- Defaults, limits, ownership, cancellation, and failure behavior are explicit.
- Examples show the simple path before advanced customization.
- Terminology is consistent across code, tests, README, and package metadata.
- Documentation never implies technical errors are safe for clients.
- Error codes and response shapes are identified as versioned contracts.
- Consumers know when to use core, ASP.NET Core, or infrastructure.
- Breaking changes include migration guidance.
- Documentation is concise enough to maintain.
- Sibling READMEs use a common structure and cross-link correctly.

## Required verification

Run the commands for the repository under review.

### `Megaraz.ResultPattern`

```bash
dotnet restore Megaraz.ResultPattern.slnx
dotnet build Megaraz.ResultPattern.slnx --configuration Release --no-restore
dotnet test Megaraz.ResultPattern.slnx --configuration Release --no-build
dotnet pack Megaraz.ResultPattern/Megaraz.ResultPattern.csproj \
  --configuration Release --no-build --no-restore
```

### `Megaraz.ResultPattern.AspNetCore`

```bash
dotnet restore Megaraz.ResultPattern.AspNetCore.slnx
dotnet build Megaraz.ResultPattern.AspNetCore.slnx \
  --configuration Release --no-restore
dotnet test Megaraz.ResultPattern.AspNetCore.slnx \
  --configuration Release --no-build
dotnet pack \
  Megaraz.ResultPattern.AspNetCore/Megaraz.ResultPattern.AspNetCore.csproj \
  --configuration Release --no-build --no-restore
```

### `Megaraz.ResultPattern.Infrastructure`

```bash
dotnet restore Megaraz.ResultPattern.Infrastructure.slnx
dotnet build Megaraz.ResultPattern.Infrastructure.slnx \
  --configuration Release --no-restore
dotnet test Megaraz.ResultPattern.Infrastructure.slnx \
  --configuration Release --no-build
dotnet pack \
  Megaraz.ResultPattern.Infrastructure/Megaraz.ResultPattern.Infrastructure.csproj \
  --configuration Release --no-build --no-restore
```

When package contracts or metadata change, also:

1. Inspect the `.nupkg` contents.
2. Confirm dependency groups and supported TFMs.
3. Confirm packaged README, license, XML docs, and repository metadata.
4. Verify Source Link and symbol behavior.
5. Install into a clean consumer project.
6. Compile and run a representative documented example.
7. Compare the public API with the latest published or approved baseline.
8. Report warnings and skipped or unavailable validation.

If a shared contract changes, build and test affected siblings against the new
packed dependency rather than relying only on project references. For a
family-wide release, run the complete verification set in dependency order:
core first, followed by both extension packages.

## Review output format

Use the following format:

```markdown
# Code Review: <repository or change>

**Reviewed:** <YYYY-MM-DD>
**Scope:** <commit, branch, pull request, files, or release>
**Package version:** <current or proposed version>
**Target frameworks:** <TFMs>
**Reviewer:** <name or agent>

## Decision

<Ready to merge | Ready with follow-up | Not ready to merge>
<Ready to publish | Not ready to publish | Publication not assessed>

One paragraph explaining the decision and highest material risk.

## Findings

### P0 — Release blockers

List findings or write `None`.

### P1 — Must fix before release

List findings or write `None`.

### P2 — Should fix

List findings or write `None`.

### P3 — Improvements

List findings or write `None`.

Each finding must use:

#### [P#] Concise finding title

- **Location:** `path/to/file.cs:line`
- **Evidence:** What the implementation demonstrably does.
- **Impact:** Why this matters to package consumers.
- **Recommendation:** Focused remediation.
- **Acceptance criteria:** Observable proof that it is resolved.

## Compatibility assessment

- **Source compatibility:** Compatible / Breaking / Not applicable
- **Binary compatibility:** Compatible / Breaking / Not assessed
- **Behavioral compatibility:** Compatible / Changed / Breaking
- **Wire compatibility:** Compatible / Changed / Not applicable
- **Dependency compatibility:** Compatible / Changed / Breaking
- **Target-framework compatibility:** Compatible / Changed / Breaking
- **Serialized or persisted formats:** Compatible / Changed / Not applicable
- **Required version impact:** Patch / Minor / Major / Prerelease only

Explain every `Changed`, `Breaking`, or `Not assessed` result.

## Cross-repository consistency

Summarize:

- Sibling packages inspected.
- Shared conventions affected.
- Intentional differences and their justification.
- Cross-package validation performed or still required.

## Security assessment

Summarize public-message safety, diagnostic handling, input bounds,
cancellation, resource ownership, workflow security, and residual risks.

## Package and open-source readiness

Summarize NuGet metadata, package contents, documentation, license, contribution
and security guidance, Source Link, provenance, CI, release workflow, and
consumer smoke testing.

## Verification

| Check | Result | Notes |
|---|---|---|
| Restore | Pass / Fail / Not run | |
| Release build | Pass / Fail / Not run | |
| Tests | Pass / Fail / Not run | Include TFMs and counts |
| Pack | Pass / Fail / Not run | |
| Package inspection | Pass / Fail / Not run | |
| Consumer smoke test | Pass / Fail / Not run | |
| Public API comparison | Pass / Fail / Not run | |
| Sibling compatibility | Pass / Fail / Not run | |
| Dependency vulnerability review | Pass / Fail / Not run | |

List warnings, skipped tests, environmental limitations, and commands not run.

## Positive observations

List design choices worth preserving. Do not use this section to dilute
actionable findings.

## Recommended next steps

Provide a short ordered list beginning with release blockers and compatibility
decisions.
```

## Release decision rules

A package is not ready to publish when:

- Any P0 remains open.
- A P1 affects correctness, security, boundaries, or compatibility.
- Required restore, Release build, tests, or pack failed.
- A supported TFM was not validated without an accepted reason.
- Package contents were not inspected after packaging changes.
- A breaking change lacks versioning and migration guidance.
- Public behavior changed without tests and documentation.
- Release credentials or workflow permissions are unsafe.
- The publishing artifact is not the verified artifact.
- A shared-contract change has not been assessed against affected siblings.

A package may be ready with follow-up when only well-understood P2 or P3 items
remain and none undermine the documented public contract or family consistency.

## Definition of a publishable package

A package is publishable when it:

- Has a focused purpose and respected package boundary.
- Fits coherently into the three-package family.
- Has a small and intentional public API.
- Maintains documented invariants.
- Is secure and conservative by default.
- Offers explicit customization where genuine policy varies.
- Does not force application-specific policy.
- Supports documented TFMs and minimum dependencies.
- Has meaningful tests for public behavior and regressions.
- Preserves compatibility according to semantic versioning.
- Produces a clean, traceable, correctly described NuGet artifact.
- Includes enough documentation for adoption without reading the source.
- Has reproducible CI and a least-privilege release process.
- Clearly reports limitations and residual risks.

## Authoritative references

Use current official guidance when a review depends on platform, packaging, or
security behavior:

- [.NET library guidance](https://learn.microsoft.com/dotnet/standard/library-guidance/)
- [NuGet package compatibility rules](https://learn.microsoft.com/dotnet/standard/library-guidance/nuget-package-compatibility-rules)
- [Versioning .NET libraries](https://learn.microsoft.com/dotnet/standard/library-guidance/versioning)
- [Dependencies and .NET libraries](https://learn.microsoft.com/dotnet/standard/library-guidance/dependencies)
- [Target frameworks](https://learn.microsoft.com/dotnet/standard/frameworks)
- [Create a NuGet package with MSBuild](https://learn.microsoft.com/nuget/create-packages/creating-a-package-msbuild)
- [NuGet package README guidance](https://learn.microsoft.com/nuget/nuget-org/package-readme-on-nuget-org)
- [GitHub Actions security hardening](https://docs.github.com/actions/security-for-github-actions/security-guides/security-hardening-for-github-actions)

Repository policy and documented package contracts take precedence when they
are stricter than general guidance.
