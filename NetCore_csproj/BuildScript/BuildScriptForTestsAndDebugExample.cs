using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FlubuCore.Context;
using FlubuCore.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BuildScript.BuildScript
{
    /// <summary>
    /// See FlubuExample.Tests BuildScriptTests.cs
    /// </summary>
    public class BuildScriptForTestsAndDebugExample : DefaultBuildScript
    {
        protected override void ConfigureBuildProperties(IBuildPropertiesContext context)
        {
        }

        protected override void ConfigureTargets(ITaskContext session)
        {
            session.CreateTarget("TestAndDebugExample")
                .SetAsDefault()
                .Do(CreateFile);
        }


        public void CreateFile(ITaskContext context)
        {
           using(File.Create("test123.txt"))
           {
           }
        } 
    }
}
