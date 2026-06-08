global using Xunit;

// FirebaseApp.Create is a process-wide singleton; running test classes in parallel
// would race two factories into creating it twice. Keep the suite sequential.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
