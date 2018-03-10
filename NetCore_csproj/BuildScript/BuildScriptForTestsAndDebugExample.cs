using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FlubuCore.Context;
using FlubuCore.Scripting;

namespace BuildScript.BuildScript
{
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
            File.Create("test123.txt");
        } 
    }
}
