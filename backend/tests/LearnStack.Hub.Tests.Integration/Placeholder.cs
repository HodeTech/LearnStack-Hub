namespace LearnStack.Hub.Tests.Integration;

/// <summary>
/// P02c-0 ships zero integration tests. The CI job `backend-integration`
/// is `if: false`; `make test-backend` filters this project out. P02c-2
/// adds the first Testcontainers-backed test (Hub-side internal API
/// endpoint smoke) and flips the CI gate.
/// </summary>
internal sealed class Placeholder;
