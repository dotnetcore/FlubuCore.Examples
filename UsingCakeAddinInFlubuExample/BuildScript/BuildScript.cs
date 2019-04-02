using System;
using Cake.Common.Build;
using Cake.FileHelpers;
using FlubuCore.Context;
using FlubuCore.Context.FluentInterface.Interfaces;
using FlubuCore.Scripting;

//#nuget FlubuCore.CakePlugin, 1.0.0
//#nuget Cake.Core, 0.30.0
//#nuget Cake.FileHelpers, 3.1.0
//#nuget Cake.Common, 0.30.0
//#nuget FlubuCore.Diff, 1.0.1


namespace UsingCakeAddinInFlubuExample
{
   /// <summary>
   /// Example uses Cake.FileHelpers addin.
   /// This is just an example that using Cake addins works. Using this addin in real scenario would be meaningless as you could just use File.ReadAllLines() with FlubuCore.
   /// </summary>
    public class BuildScript : DefaultBuildScript
    {
        protected override void ConfigureBuildProperties(IBuildPropertiesContext context)
        {
        }

        protected override void ConfigureTargets(ITaskContext context)
        {
            context.CreateTarget("Cake.Addion")
                .SetAsDefault()
                .Do(CakeAddinExample);
        }
        
        private void CakeAddinExample(ITaskContext context)
        {
            context.Tasks().DiffTask("1.txt", "2.txt", "output.html").Execute(context);
            var test = context.CakeTasks().Bitrise().IsRunningOnBitrise;
            context.LogInfo($"IsRunning on bitrise: {test}");
            var lines = context.CakeTasks().FileReadLines("ExampleFile.txt");
            context.LogInfo(lines[0]);
        }
    }
}
