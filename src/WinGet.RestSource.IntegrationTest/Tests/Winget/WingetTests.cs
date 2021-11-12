// -----------------------------------------------------------------------
// <copyright file="WingetTests.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.WinGet.RestSource.IntegrationTest.Winget
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using CliWrap;
    using CliWrap.Buffered;
    using Microsoft.Extensions.Configuration;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// API Object Result Test.
    /// </summary>
    public class WingetTests : IAsyncLifetime
    {
        private const string PowerToysPackageIdentifier = "Microsoft.PowerToys";
        private readonly ITestOutputHelper log;
        private readonly string restSourceName;
        private readonly string restSourceUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="WingetTests"/> class.
        /// </summary>
        /// <param name="log">ITestOutputHelper.</param>
        public WingetTests(ITestOutputHelper log)
        {
            this.log = log;

            var configuration = new ConfigurationBuilder()

                // Defaults specified in the Test.runsettings.json
                .AddJsonFile("Test.runsettings.json", true)

                // But they can be overridden using environment variables
                .AddEnvironmentVariables()
                .Build();

            this.restSourceName = configuration["RestSourceName"] ?? throw new ArgumentNullException();
            this.restSourceUrl = configuration["RestSourceUrl"] ?? throw new ArgumentNullException();

            this.log.WriteLine($"{nameof(this.restSourceName)}: {this.restSourceName}");
            this.log.WriteLine($"{nameof(this.restSourceUrl)}: {this.restSourceUrl}");
        }

        /// <inheritdoc/>
        public async Task InitializeAsync()
        {
            await AddSource();
        }

        /// <inheritdoc/>
        public async Task DisposeAsync()
        {
            await RemoveSource();
        }

        [Fact]
        public async Task WingetUpgrade()
        {
            await this.TestWingetQuery("upgrade Microsoft.PowerShell", output => output.Contains("No applicable update found."));
            await this.TestWingetQuery("upgrade Microsoft.PowerShelllll", output => output.Contains("No installed package found matching input criteria."));
        }

        [Fact]
        public async Task WingetList()
        {
            await this.TestWingetQuery("list \"Windows Software Development Kit\"", output => output.Contains("Microsoft.WindowsSDK"));
        }

        [Fact]
        public async Task WingetSearch()
        {
            await this.TestWingetSearchQuery("PowerToys", PowerToysPackageIdentifier);
            await this.TestWingetSearchQuery("powertoys", PowerToysPackageIdentifier);
            await this.TestWingetSearchQuery("PowerT", PowerToysPackageIdentifier);
            await this.TestWingetSearchQuery("owertoy", PowerToysPackageIdentifier);
            await this.TestWingetSearchQuery("nonexistentpackage");

            await this.TestWingetSearchQuery("--name PowerToys", PowerToysPackageIdentifier);
            await this.TestWingetSearchQuery("--name powertoys", PowerToysPackageIdentifier);
            await this.TestWingetSearchQuery("--name PowerT", PowerToysPackageIdentifier);
            await this.TestWingetSearchQuery("--name owertoy", PowerToysPackageIdentifier);
            await this.TestWingetSearchQuery("--name nonexistentpackage");
        }

        private async Task TestWingetSearchQuery(string query, params string[] expectedPackageIdentifiers)
        {
            await this.TestWingetQuery($"search {query}", null, expectedPackageIdentifiers);
        }

        private async Task TestWingetQuery(string query, Func<string, bool> validator, params string[] expectedPackageIdentifiers)
        {
            string output = await RunWinget($"{query} -s {this.restSourceName}");
            if (validator != null)
            {
                Assert.True(validator(output));
            }
            else
            {
                var results = ParseWingetOutput(output);
                var validators = expectedPackageIdentifiers.Select(id => (WingetApp app) => Assert.Equal(id, app.Id)).ToArray();
                Assert.Collection(results, validators);
            }
        }

        private static IEnumerable<WingetApp> ParseWingetOutput(string output)
        {
            var rows = output.Split(Environment.NewLine);

            if (rows[0].Contains("No package found matching input criteria."))
            {
                return Enumerable.Empty<WingetApp>();
            }

            return rows
                .Skip(2)
                .Select(r =>
                {
                    var props = r.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    return props.Length == 3 ? new WingetApp(props[0], props[1], props[2]) : null;
                })
                .Where(r => r != null)
                .ToList();
        }

        private async Task AddSource()
        {
            await RemoveSource();
            await RunWinget($"source add -n {this.restSourceName} -a {this.restSourceUrl} -t \"Microsoft.Rest\"");
        }

        private async Task RemoveSource()
        {
            await RunWinget($"source remove {this.restSourceName}");
        }

        private static async Task<string> RunWinget(string arguments)
        {
            var result = await Cli.Wrap(@"winget")
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

            return result.StandardOutput ?? result.StandardError;
        }

        private record WingetApp(string Name, string Id, string Version);
    }
}
