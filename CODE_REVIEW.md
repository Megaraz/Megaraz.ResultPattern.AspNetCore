# Code Review: Megaraz.ResultPattern.AspNetCore

**Reviewed:** 2026-07-25  
**Scope:** Current library implementation, package metadata, repository readiness,
and consistency with the local `Megaraz.ResultPattern` core repository.

## Overall assessment

The library has a good starting shape: it is small, nullable-enabled, targets the
same TFMs as the core package, generates XML documentation, and keeps the HTTP
mapping logic free of a hard ASP.NET Core dependency. `HttpError` also follows the
core package's extension model by deriving from `Error` and retaining
`ErrorType.External`.

It is not yet ready to publish as a dependable public NuGet/portfolio project.
There are no tests or release workflows, the repository does not currently
contain the expected open-source files, and several public API choices hard-code
application policies that downstream applications need to own. Resolve the
P0 items before publishing; P1 items should be resolved before treating the API
as stable.

## Backlog

| Priority | Item | Evidence | Acceptance criteria |
|---|---|---|---|
| P0 | [ ] Add a test project with behavioral coverage for every public API. | The solution contains only the library project; no `*.Tests` project or test files exist. `HttpResultMapper`, `HttpResponseToResultExtensions`, `HttpError`, and `PaginationParameters` all define externally observable behavior. | Add a multi-targeted test project matching the core package's testing approach. Cover success/failure status mappings, validation payloads, every `HttpError` factory, malformed/empty/non-JSON response paths, cancellation propagation, JSON options, and pagination boundaries. |
| P0 | [ ] Add CI and tag-based package publishing workflows, based on the core repository. | This repository has no `.github/workflows`; the core repository has `ci.yml` and `publish.yml` that restore, build, test, pack, and publish. | Run restore, Release build, tests, and pack on pushes/PRs. Publish only a verified package from version tags, with least-privilege permissions and the existing NuGet login pattern. |
| P0 | [ ] Make the repository genuinely open-source ready. | The root has no `LICENSE` or `CONTRIBUTING.md`, though the package declares `PackageLicenseExpression` as `MIT` (`Megaraz.ResultPattern.AspNetCore.csproj:18`). | Add the MIT license text, contribution guidance adapted from the core repository, and a security/contact policy if one is intended. Ensure all repository links and package claims are truthful at release. |
| P1 | [x] Replace fixed HTTP/result mapping rules with an explicit, composable policy. | `HttpResultMapper` fixes all success and failure status codes (`HttpResultMapper.cs:15-97`), and `MapHttpErrorFailure` fixes the HTTP-error mapping (`HttpResultMapper.cs:100-126`). Consumers cannot alter mappings without bypassing the library. | Provide a documented default policy plus a focused customization mechanism, for example a mapper/options object or callbacks for `ErrorType`, `HttpErrorType`, success status, and response-body creation. Preserve the existing defaults as the uncomplicated path. |
| P1 | [x] Provide an actual ASP.NET Core adapter, or rename/split the package to reflect that it is HTTP-only. | The package is named `.AspNetCore`, but has no ASP.NET Core references. Its README asks callers to manually translate `MappedHttpResponse` using `Results.Json` (`README.md:10-14`). That example discards `MappedHttpResponse.Location`. | Either expose tested minimal-API/controller adapters that produce `IResult`/`ActionResult` and correctly write status, body, and `Location`, or publish this assembly as an HTTP-agnostic package and create a separate optional ASP.NET Core integration package. |
| P1 | [x] Stop treating untrusted upstream response content as a safe client message by default. | `GetFailureMessageAsync` extracts an external response body and `CreateWithDefaultMessage` assigns it as both technical description and `UserMessage` (`HttpResponseToResultExtensions.cs:54-62`, `HttpError.cs:101-108`). `Result.Failure(error)` consequently exposes it through `Result.Message`. This conflicts with the core README's distinction between technical descriptions and application-owned user-facing messages. | Default `UserMessage` to an application-controlled generic message (or empty, consistent with core factories). Keep remote detail only in `Description`/diagnostics. Offer an explicit opt-in safe-message policy for APIs that intentionally proxy upstream messages. |
| P1 | [x] Fix `PaginationParameters` so its documented normalization invariant cannot be broken. | `Normalize(1, 1, 0)` returns `PageSize = 0` (`PaginationParameters.cs:31-34`). The record's public `init` properties can also be changed with a `with` expression after normalization (`PaginationParameters.cs:10-13`). | Reject `maxPageSize < 1`, validate all construction paths, and use an immutable type/shape that cannot represent invalid values. Avoid making the default maximum a global, hidden application policy; let endpoint/application configuration supply it. |
| P1 | [x] Define and document a stable response contract. | `MappedHttpResponse.Body` remains `object?` for application-defined success values and custom policies, while default failures use explicit immutable DTOs with a public `Message` and `Code` (`MappedHttpResponse.cs`, `HttpResultMapper.cs`). README JSON examples document the default success and failure shapes. | Decide which fields are safe/public for all failure types, model the contract with explicit immutable DTOs, and document example success, validation, and non-validation JSON. If codes are public, explain their stability; if not, omit them consistently. |
| P1 | [ ] Add standard package provenance and release metadata. | Unlike the core project, this project does not reference `Microsoft.SourceLink.GitHub` and has no `PackageReleaseNotes` (`Megaraz.ResultPattern.AspNetCore.csproj:3-31`; compare core `.csproj:24-27`). | Add Source Link as a private build asset, preserve repository/source metadata, add per-version release notes or a changelog, and inspect the generated `.nupkg` in CI. Consider an icon once one is available. |
| P2 | [x] Make deserialization and error-body parsing configurable rather than hard-coded. | A private `JsonSerializerOptions(JsonSerializerDefaults.Web)` is always used (`HttpResponseToResultExtensions.cs:9,51`). Response-message discovery hard-codes property names such as `"message"`, `"detail"`, and `"error_description"` (`HttpResponseToResultExtensions.cs:153-170`). | Provide per-call or configured serializer options/type metadata and an optional error-message extractor. Retain a conservative default parser, but do not make a package assume every upstream API's JSON conventions. |
| P2 | [x] Define the outbound HTTP client failure boundary. | `MapToResultAsync` maps only a nullable `HttpResponseMessage` to `TransportFailure` (`HttpResponseToResultExtensions.cs:18-19,79-80`), but `HttpClient` ordinarily signals DNS, connection, timeout, and request failures by throwing. | Add a documented companion API/handler for `HttpRequestException` and selected timeout failures, or remove the nullable-response convention and clearly state that callers map transport exceptions themselves. Never convert caller cancellation into a failure result. |
| P2 | [x] Bound response-body reads and document consumption behavior. | Both success and failure mapping read the complete body into a string (`HttpResponseToResultExtensions.cs:136-141`), with no size limit. The extension also consumes the `HttpContent` stream. | Allow a maximum inspected/error-body size and document that content is consumed. Keep cancellation propagated. Support a caller-provided deserialization path when streaming is important. |
| P2 | [x] Use collection types that express the response DTO contract and materialize enumerables. | `ValidationErrorResponseBody.ValidationErrors` is `IEnumerable<ValidationErrorItem>?` and receives a deferred LINQ sequence (`MappedHttpResponse.cs:34`, `HttpResultMapper.cs:57-70`). | Use a non-null `IReadOnlyList<ValidationErrorItem>` (or array) and materialize it at mapping time. This aligns with the core `Result.ValidationErrors` contract and produces predictable serialization. |
| P2 | [x] Align extension factory ergonomics with the core package's localization and ownership model. | `HttpError` embeds English descriptions/user messages and exposes no `messageFactory` or user-message argument for built-in cases (`HttpError.cs:49-99`). `Custom` also uses one supplied string for both technical and user-facing information (`HttpError.cs:46-47`). | Separate technical detail from optional user message for every factory. Provide an application-owned localization/customization hook comparable to the core factories, while keeping stable default error codes. |
| P3 | [ ] Expand README and API XML documentation into usable public-package documentation. | The README is only an installation snippet and one outbound mapping example; it omits inbound mapping, error status behavior, cancellation, customization, pagination, and location handling. Several public method parameters have summary-only XML documentation. | Add a purpose/non-goals statement, supported TFMs, installation, minimal-API and controller examples, inbound/outbound mapping guidance, customization/localization guidance, the response contract, and a compatibility/versioning policy. Add meaningful parameter/exception/remarks documentation where behavior is non-obvious. |
| P3 | [x] Standardize code style with the core project before first stable release. | `MappedHttpResponse.cs` and `PaginationParameters.cs` use block namespace syntax while the rest of the extension and the core project use file-scoped namespaces. | Apply the core repository's style consistently and enable equivalent analyzers/formatting checks in CI if the project adopts them. |

## Design notes

### Keep framework dependencies modular

The current framework-neutral mapper is useful, but it should be a lower-level
HTTP component, not an incomplete ASP.NET Core abstraction. A clean public
shape would let users choose only the layer they need:

1. A dependency-light HTTP/result mapping package containing `HttpError`,
   configurable inbound mapping, and configurable response policies.
2. An optional ASP.NET Core package that translates that model into minimal API
   `IResult` values and/or MVC results without forcing either framework on
   non-web consumers.

This preserves the project's stated purpose while avoiding a package name that
promises integration it does not yet deliver.

### Preserve core-package conventions

The core package intentionally lets applications own error-code shapes,
presentation language, and messages. This extension should preserve that
boundary: HTTP status mapping can have useful defaults, but upstream payloads,
localized messages, serializer behavior, error schemas, page-size limits, and
public API response shapes must be caller-configurable rather than package
assumptions.

### Positive implementation details to retain

- `HttpError` correctly layers HTTP-specific classification over
  `ErrorType.External`.
- `HttpResponseToResultExtensions` uses cancellation-aware content reads and
  does not swallow `OperationCanceledException`.
- The projects match the core package's nullable, implicit-using, XML-doc, TFM,
  package-ID, and repository-metadata conventions.
- `PaginationParameters` currently centralizes normalization rather than
  duplicating it at endpoints; retain that intent after enforcing its
  invariants.
