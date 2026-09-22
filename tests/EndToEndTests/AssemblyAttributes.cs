using Xunit.Sdk;
using Xunit.v3;
using QubicaCinema.EndToEndTests.Fixtures;

// The whole topology, booted once for the assembly: see CinemaAppFixture.
[assembly: AssemblyFixture<CinemaAppFixture>]

// One topology, shared by every test in this assembly: they must run one at a time.
[assembly: Parallelization(Mode = ParallelMode.None)]
