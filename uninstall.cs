//If your script uses other assemblies you can reference them here:
//refAssemblies: System.dll, TCAdmin.SDK.dll,TCAdmin.GameHosting.SDK.dll, TCAdmin.Interfaces.dll
using TCAdmin.GameHosting.SDK.Objects.Extensions;
using System;

public class CSharpScript : CSharpScriptBase
{
    public void Main()
    {
        var dbman = TCAdmin.SDK.Database.DatabaseManager.CreateDatabaseManager();

        //Delete existing scripts and variables
        foreach (var script in TCAdmin.GameHosting.SDK.Objects.GlobalGameScript.GetGlobalGameScripts().FindAllByCustomField("__TCA:MODULE", "File Server Resource Manager")) { script.Delete(); }
        foreach (TCAdmin.SDK.Objects.DefaultVariable modulevar in TCAdmin.SDK.Objects.DefaultVariable.GetDefaultVariables("TCAdmin.GameHosting.SDK.Objects.Service"))
        {
            if (modulevar.Name == "FSRM_DiskQuota" | modulevar.Name == "FSRM_DiskUsage")
            {
                modulevar.Delete();
            }
        }
    }
}