using System;
using Cake.Common.Build;
using Cake.FileHelpers;
using FlubuCore.Context;
using FlubuCore.Context.FluentInterface.Interfaces;
using FlubuCore.Scripting;
using FlubuCore.Scripting.Attributes;

namespace UsingCakeAddinInFlubuExample
{
   /// <summary>
   /// Example uses Cake.FileHelpers addin.
   /// This is just an example that using Cake addins works. Using this addin in real scenario would be meaningless as you could just use File.ReadAllLines() with FlubuCore.
   /// </summary
    [NugetPackage("Cake.FileHelpers", "3.1.0")]
    [NugetPackage("FlubuCore.CakePlugin", "1.1.0")]
    public class BuildScript : DefaultBuildScript
    {
        protected override void ConfigureBuildProperties(IBuildPropertiesContext context)
        {
        }

        protected override void ConfigureTargets(ITaskContext context)
        {
            context.CreateTarget("Cake.Addon")
                .SetAsDefault()
                .Do(CakeAddinExample);
        }
        
        private void CakeAddinExample(ITaskContext context)
        {
            var test = context.CakeTasks().Bitrise().IsRunningOnBitrise;
            context.LogInfo($"IsRunning on bitrise: {test}");
            var lines = context.CakeTasks().FileReadLines("ExampleFile.txt");
            context.LogInfo(lines[0]);
        }
    }
}
