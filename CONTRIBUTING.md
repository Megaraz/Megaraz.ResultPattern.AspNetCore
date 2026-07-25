# Contributing to Megaraz.ResultPattern.AspNetCore

Thank you for your interest in contributing. This package provides HTTP and
ASP.NET Core adapters for `Megaraz.ResultPattern`.

## Development setup

Install the .NET 10 SDK, then clone the repository and run the test suite:

```bash
git clone https://github.com/Megaraz/Megaraz.ResultPattern.AspNetCore.git
cd Megaraz.ResultPattern.AspNetCore
dotnet test Megaraz.ResultPattern.AspNetCore.slnx --configuration Release
```

The solution targets .NET 8, .NET 9, and .NET 10. Tests run for each target
framework. GitHub Actions runs the same restore, build, test, and pack steps for
pushes and pull requests targeting `main`.

## Making changes

- Keep changes focused on the behavior being addressed.
- Preserve nullable reference type safety and existing public API behavior
  unless the change intentionally updates that contract.
- Add or update tests for new or changed public behavior.
- Keep test names focused on observable behavior rather than implementation
  details.
- Update the README or XML documentation when public behavior changes.

Before opening a pull request, run:

```bash
dotnet test Megaraz.ResultPattern.AspNetCore.slnx --configuration Release
```

## Issues and pull requests

For bugs, include the expected behavior, actual behavior, relevant code, and a
minimal reproduction when possible. For feature requests, explain the use case
and proposed public API.

Pull requests should describe what changed, why it changed, how it was tested,
and any compatibility or public API considerations. Keep pull requests small
and focused. All CI checks should pass before merging.

## License

By contributing, you agree that your contributions will be licensed under the
MIT License included in this repository.
