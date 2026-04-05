using Xunit;

// Disable parallelization to avoid file access conflicts on the shared ExtentReport HTML file.
[assembly: CollectionBehavior(DisableParallelization = true)]
