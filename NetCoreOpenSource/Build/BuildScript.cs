using System;
using FlubuCore.Context;
using FlubuCore.Context.Attributes.BuildProperties;
using FlubuCore.IO;
using FlubuCore.Scripting;
using FlubuCore.Tasks.Attributes;
using FlubuCore.Tasks.Versioning;

namespace Build
{
    public class BuildScript : DefaultBuildScript
    {
        [FromArg("apiKey")]
        public string NugetApiKey { get; set; }

        //// With attribute solution is stored in flubu session so it doesn't need to be defined in restore and build task.
        [SolutionFileName] public string SolutionFileName { get; set; } = "NetCoreOpenSource.sln";

        //// BuildConfiguration is stored in flubu session so it doesn't need to be defined in build task and test tasks.
        [BuildConfiguration] public string BuildConfiguration { get; set; } = "Release";

        [ProductId] public string ProductId { get; set; } = "NetCoreOpenSourceFlubuExample";

        //// Target fetches build version from changelog.md files ignoring both prefixes if they occur before build version. Build Version is also stored
        ////  in flubu session. Alternatively flubu supports fetching of build version out of the box with GitVersionTask. Just apply [GitVersion] attribute on property
        [FetchBuildVersionFromFile(ProjectVersionFileName = "Changelog.md", PrefixesToRemove = new [] { "## NetCoreOpenSource" })]
        public BuildVersion BuildVersion { get; set; }

        public FullPath OutputDir => RootDirectory.CombineWith("output");

        protected override void ConfigureTargets(ITaskContext context)
        {
            var clean = context.CreateTarget("Clean")
                .SetDescription("Clean's the solution.")
                .AddCoreTask(x => x.Clean()
                    .AddDirectoryToClean(OutputDir, true));

            var restore = context.CreateTarget("Restore")
                .SetDescription("Restore's nuget packages in all projects.")
                .AddCoreTask(x => x.Restore());

            
            //// UpdateNetCoreVersionTask updates NetCoreOpenSource project version. Version is fetched from flubu session.
            //// Alternatively you can set version in Build task through build task fluent interface.
            var build = context.CreateTarget("Build")
                .SetDescription("Build's the solution.")
                .DependsOn(clean)
                .DependsOnAsync(restore)
                .AddCoreTask(x => x.UpdateNetCoreVersionTask("NetCoreOpenSource/NetCoreOpenSource.csproj"))
                .AddCoreTask(x => x.Build()
                    .Version(BuildVersion.Version.ToString()));
                  

           var tests = context.CreateTarget("Run.tests")
               .SetDescription("Run's all test's in the solution")
                .AddCoreTask(x => x.Test()
                    .Project("NetCoreOpenSource.Tests")
                    .NoBuild());
       
           var pack = context.CreateTarget("Pack")
               .SetDescription("Prepare's nuget package.")
               .AddCoreTask(x => x.Pack()
                   .NoBuild()
                   .OutputDirectory(OutputDir));

           var branch = context.BuildSystems().Travis().BranchName;

           //// Examine travis.yaml to see how to pass api key from travis to FlubuCore build script.
           var nugetPush = context.CreateTarget("Nuget.publish")
               .SetDescription("Publishes nuget package.")
               .DependsOn(pack)
               .AddCoreTask(x => x.NugetPush($"{OutputDir}/NetCoreOpenSource.nupkg")
                   .ServerUrl("https://www.nuget.org/api/v2/package")
                   .ApiKey(NugetApiKey)
               )
               .When((c) => c.BuildSystems().RunningOn == BuildSystemType.TravisCI
                            && !string.IsNullOrEmpty(branch)
                            && branch.EndsWith("stable", StringComparison.OrdinalIgnoreCase));
            

          var rebuild = context.CreateTarget("Rebuild")
               .SetDescription("Builds the solution and runs all tests.")
               .SetAsDefault()
               .DependsOn(build, tests);

          context.CreateTarget("Rebuild.Server")
              .SetDescription("Builds the solution, runs all tests and publishes nuget package.")
              .DependsOn(rebuild, nugetPush);
        }
    }
}
