global using Xunit;

// Disable parallel test execution since tests share the system clipboard
[assembly: CollectionBehavior(DisableTestParallelization = true)]
