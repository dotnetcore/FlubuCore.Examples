using System;
using System.Collections.Generic;
using System.Text;
using FlubuCore.Context;
using FlubuCore.Context.Attributes.BuildProperties;
using FlubuCore.Context.FluentInterface.TaskExtensions;
using FlubuCore.IO;
using FlubuCore.Scripting;
using FlubuCore.Tasks.Versioning;

namespace BuildScript
{
    public class BuildScript : DefaultBuildScript
    {
        //// With attribute solution is stored in flubu session so it doesn't need to be defined in restore and build task.
        [SolutionFileName] 
        public string SolutionFileName { get; set; } = "WindowsService.sln";

        //// BuildConfiguration is stored in flubu session so it doesn't need to be defined in build task and test tasks.
        [BuildConfiguration]
        public string BuildConfiguration { get; set; } = "Release";

        [ProductId]
        public string ProductId { get; set; } = "WindowsServiceExample";

        [BuildVersion]
        public BuildVersion BuildVersion { get; set; } = new BuildVersion(new Version(1, 0 , 1 , 0));

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
            
            var build = context.CreateTarget("Build")
                .SetDescription("Build's the solution.")
                .DependsOn(clean)
                .DependsOn(restore)
                .AddCoreTask(x => x.Build()
                    .Version(BuildVersion.Version.ToString()));

            var tests = context.CreateTarget("Run.tests")
               .SetDescription("Run's all test's in the solution")
                .AddCoreTask(x => x.Test()
                    .Project("WindowsService.Tests")
                    .NoBuild());

            var package = context
                .CreateTarget("Package")
                .AddCoreTask(x => x.Publish("WindowsService"))
                .AddCoreTask(x => x.CreateZipPackageFromProjects("WindowsService", "net48", "WindowsService"));

            var rebuild = context.CreateTarget("Rebuild")
               .SetDescription("Builds the solution and runs all tests.")
               .SetAsDefault()
               .DependsOn(build, tests);
        }
    }
}
