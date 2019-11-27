using System;
using FlubuCore.Context;
using FlubuCore.Scripting;

namespace Build
{
    public class BuildScript : DefaultBuildScript
    {
        [FromArg("apiKey")]
        public string NugetApiKey { get; set; }
        
        protected override void ConfigureBuildProperties(IBuildPropertiesContext context)
        {
            context.Properties.Set(BuildProps.ProductId, "NetCoreOpenSourceFlubuExample");
            
            //// Solution is stored in flubu session so it doesn't need to be defined in restore and build task.
            context.Properties.Set(BuildProps.Solution, "NetCoreOpenSource.sln");
            
            //// Solution is stored in flubu session so it doesn't need to be defined in build, test and pack task.
            context.Properties.Set(BuildProps.BuildConfiguration, "Release");
            context.Properties.Set(BuildProps.BuildVersion, "Output");
        }

        protected override void ConfigureTargets(ITaskContext context)
        {
            var clean = context.CreateTarget("Clean")
                .SetDescription("Clean's the solution.")
                .AddCoreTask(x => x.Clean()
                    .AddDirectoryToClean(OutputDirectory, true));

            var restore = context.CreateTarget("Restore")
                .SetDescription("Restore's nuget packages in all projects.")
                .AddCoreTask(x => x.Restore());

            //// Target fetches build version from changelog.md files ignoring both prefixes if they occur before build version. Build Version is stored
            ////  in flubu session. In script it can be accesses through context.Properties.Get<Version>(BuildProps.BuildVersion);
            //// Alternatively flubu supports fetching of build version out of the box with GitVersionTask.
            var fetchBuildVersion = context.CreateTarget("fetch.buildVersion")
                .SetAsHidden()
                .SetDescription("Fetches build version from Changelog.md file.")
                .AddTask(x => x.FetchBuildVersionFromFileTask()
                    .ProjectVersionFileName("Changelog.md")
                    .RemovePrefix("## NetCoreOpenSource ")
                    .RemovePrefix("## NetCoreOpenSource"));
            
            //// UpdateNetCoreVersionTask updates NetCoreOpenSource project version. Version is fetched from flubu session.
            //// Alternatively you can set version in Build task through build task fluent interface.
            var build = context.CreateTarget("Build")
                .SetDescription("Build's the solution.")
                .DependsOn(clean)
                .DependsOnAsync(restore, fetchBuildVersion)
                .AddCoreTask(x => x.UpdateNetCoreVersionTask("NetCoreOpenSource/NetCoreOpenSource.csproj"))
                .AddCoreTask(x => x.Build());
                  

           var tests = context.CreateTarget("Run.tests")
               .SetDescription("Run's all test's in the solution")
                .AddCoreTask(x => x.Test()
                    .Project("NetCoreOpenSource.Tests")
                    .NoBuild());
       
           var pack = context.CreateTarget("Pack")
               .SetDescription("Prepare's nuget package.")
               .AddCoreTask(x => x.Pack()
                   .NoBuild()
                   .OutputDirectory(OutputDirectory));

           var branch = context.BuildSystems().Travis().Branch;

           //// Examine travis.yaml to see how to pass api key from travis to FlubuCore build script.
           var nugetPush = context.CreateTarget("Nuget.publish")
               .SetDescription("Publishes nuget package.")
               .DependsOn(pack)
               .AddCoreTask(x => x.NugetPush($"{OutputDirectory}/NetCoreOpenSource.nupkg")
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
