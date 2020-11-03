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

        //Create DiskQuota Variable
        var quota_var = new TCAdmin.SDK.Objects.DefaultVariable();
        quota_var.Source = typeof(TCAdmin.GameHosting.SDK.Objects.Service).ToString();
        quota_var.IsCustom = true;
        quota_var.Name = "FSRM_DiskQuota";
        quota_var.Description = "[FSRM] DiskQuota";
        quota_var.DefaultValue = string.Format("$ipy<%ReturnValue = ThisService.Variables.GetValue('{0}')%>", quota_var.Name);
        quota_var.VariableType = 1;
        quota_var.DefaultValueIsScript = true;
        quota_var.ReadOnly = false;
        quota_var.ViewOrder = 0;
        var newid = Convert.ToInt32(dbman.Execute("SELECT max(variable_id) + 1 FROM tc_default_variables").Rows[0][0]);
        if(newid< 1000) {
            newid = 1000;
        }
        quota_var.VariableId = newid;
        quota_var.Save();

        var quota_config = quota_var.GetVariableConfig(0);

        quota_config.ScriptParameter = true;
        quota_config.SaveValueFromScript = true;
        quota_config.AdminAccess = true;
        quota_config.SubAdminAccess = false;
        quota_config.ResellerAccess = false;
        quota_config.UserAccess = false;
        quota_config.ServerOwnerAccess = false;
        quota_config.ParentVariableSource = "";
        quota_config.ParentVariableSourceId = "";
        quota_config.ParentVariableId = 0;
        quota_config.ParentVariableValue = "";
        quota_config.ValueRequired = true;
        quota_config.RequiredMessage = "The disk quota is required";
        quota_config.Label = "Disk Quota (GB)";
        quota_config.Description = "Specify the maximum size allowed for this game server's root directory.";
        quota_config.ViewOrder = 0;
        quota_config.ItemType =  TCAdmin.SDK.Objects.DynamicFormItemType.NumericTextBox;
        quota_config.Configuration = @"<?xml version=""1.0"" encoding=""utf-16"" standalone=""yes""?>
<values>
  <add key=""MinValue"" value=""0"" type=""System.Double,mscorlib"" />
  <add key=""MaxValue"" value=""70368744177664"" type=""System.Double,mscorlib"" />
  <add key=""MaxLength"" value=""0"" type=""System.Double,mscorlib"" />
  <add key=""DecimalDigits"" value=""2"" type=""System.Double,mscorlib"" />
  <add key=""DecimalSeparator"" value=""."" type=""System.String,mscorlib"" />
  <add key=""GroupSeparator"" value="","" type=""System.String,mscorlib"" />
  <add key=""GroupSizes"" value=""3"" type=""System.Double,mscorlib"" />
  <add key=""NegativePattern"" value=""-n"" type=""System.String,mscorlib"" />
  <add key=""PositivePattern"" value=""n"" type=""System.String,mscorlib"" />
  <add key=""ShowSpinButtons"" value=""False"" type=""System.Boolean,mscorlib"" />
  <add key=""WriteAllDecimals"" value=""False"" type=""System.Boolean,mscorlib"" />
  <add key=""StepIncrement"" value=""1"" type=""System.Double,mscorlib"" />
</values>";
        quota_config.Save();

        //Create DiskUsage Variable
        var usage_var = new TCAdmin.SDK.Objects.DefaultVariable();
        usage_var.Source = typeof(TCAdmin.GameHosting.SDK.Objects.Service).ToString();
        usage_var.IsCustom = true;
        usage_var.Name = "FSRM_DiskUsage";
        usage_var.Description = "[FSRM] DiskUsage";
        usage_var.DefaultValue = string.Format("$ipy<%ReturnValue = ThisService.Variables.GetValue('{0}')%>", usage_var.Name);
        usage_var.VariableType = 1;
        usage_var.DefaultValueIsScript = true;
        usage_var.ReadOnly = false;
        usage_var.ViewOrder = 0;
        newid = Convert.ToInt32(dbman.Execute("SELECT max(variable_id) + 1 FROM tc_default_variables").Rows[0][0]);
        if (newid < 1000)
        {
            newid = 1000;
        }
        usage_var.VariableId = newid;
        usage_var.Save();

        //Create Set Disk Quota script
        var quota_script = new TCAdmin.GameHosting.SDK.Objects.GlobalGameScript();
        quota_script.OperatingSystem = TCAdmin.SDK.Objects.OperatingSystem.Windows;
        quota_script.ScriptEngineId = 8;
        quota_script.Description = "[FSRM] Limit the folder size using File Server Resource Manager";
        quota_script.ServiceEvent = TCAdmin.GameHosting.SDK.Objects.ServiceEvent.CustomAction;
        quota_script.Name = "Set Disk Quota";
        quota_script.PromptVariables = true;
        quota_script.ExecuteInPopup = true;
        quota_script.CustomFields["__TCA:MODULE"] = "File Server Resource Manager";
        quota_script.ScriptContents = @"
#Exit if variable is not set or set default quota if game is configured to do so.
if(!$ThisService.Variables.HasValue(""FSRM_DiskQuota"") -or $ThisService.Variables[""FSRM_DiskQuota""].ToString() -eq """" ){
  
  if($ThisGame.CustomFields[""__FSRM:APPLY_QUOTA_TO_ALL_SERVICES""] -eq $null) {
      exit
  }
  
  if($ThisGame.CustomFields[""__FSRM:APPLY_QUOTA_TO_ALL_SERVICES""] -eq ""true"") {
    $ThisService.Variables[""FSRM_DiskQuota""] = $ThisGame.CustomFields[""__FSRM:DEFAULT_QUOTA""]
  } else {
      exit
  }
}

#Install (might require a reboot or 2)
$srv=Get-WindowsFeature FS-Resource-Manager
if($srv.Installed -eq $false){
  write-host ""File Server Resource Manager is not installed. Installing...""
  Install-WindowsFeature –Name FS-Resource-Manager –IncludeManagementTools
    write-host ""File Server Resource Manager has been installed.""
}

Import-Module FileServerResourceManager

#Try to get current quota. 
$quota = Get-FsrmQuota -Path $ThisService.RootDirectory 2> $null
$isnew = $false
$updated = $false
  
if($quota -eq $null) {
  write-host ([string]::Format(""{0} - Creating quota..."", $ThisService.ConnectionInfo))
  New-FsrmQuota -Path $ThisService.RootDirectory -Description $ThisService.ConnectionInfo -Size (($ThisService.Variables[""FSRM_DiskQuota""] - 0) *1024 * 1024 * 1024)
  $isnew = $true
}else{ 
  if($quota.Size -eq ($ThisService.Variables[""FSRM_DiskQuota""] - 0) *1024 * 1024 * 1024) {
    #No change needed
  }else{
    write-host ([string]::Format(""{0} - Updating quota..."", $ThisService.ConnectionInfo))
    Set-FsrmQuota -Path $ThisService.RootDirectory -Description $ThisService.ConnectionInfo -Size (($ThisService.Variables[""FSRM_DiskQuota""] - 0) *1024 * 1024 * 1024) 
    $updated=$true
  }
}
$quota = Get-FsrmQuota -Path $ThisService.RootDirectory 2> $null

$ThisService.Variables[""FSRM_DiskUsage""] = [math]::Round($quota.Usage/1024/1024/1024,2)

$ThisService.Save()
if($isnew -eq $true) {
  write-host ([string]::Format(""{0} - New quota set to {1}GB."", $ThisService.ConnectionInfo, $ThisService.Variables[""FSRM_DiskQuota""]))
} else {
  if($updated -eq $true){
    write-host ([string]::Format(""{0} - Quota updated to {1}GB. Current Usage: {2}GB"", $ThisService.ConnectionInfo, $ThisService.Variables[""FSRM_DiskQuota""], $ThisService.Variables[""FSRM_DiskUsage""] ))
  }else{
    write-host ([string]::Format(""{0} - Quota was already {1}GB. Current Usage: {2}GB"", $ThisService.ConnectionInfo, $ThisService.Variables[""FSRM_DiskQuota""], $ThisService.Variables[""FSRM_DiskUsage""] ))
  }
}";
        quota_script.GenerateKey();
        quota_script.Save();

        var quota_reinstall_script = new TCAdmin.GameHosting.SDK.Objects.GlobalGameScript();
        quota_reinstall_script.OperatingSystem = TCAdmin.SDK.Objects.OperatingSystem.Windows;
        quota_reinstall_script.ScriptEngineId = 8;
        quota_reinstall_script.Description = "[FSRM] Limit the folder size using File Server Resource Manager";
        quota_reinstall_script.ServiceEvent = TCAdmin.GameHosting.SDK.Objects.ServiceEvent.AfterReinstall;
        quota_reinstall_script.Name = "Set Disk Quota";
        quota_reinstall_script.IgnoreErrors = true;
        quota_reinstall_script.CustomFields["__TCA:MODULE"] = "File Server Resource Manager";
        quota_reinstall_script.ScriptContents=string.Format("$Script.Execute(0, {0})", quota_script.ScriptId);
        quota_reinstall_script.GenerateKey();
        quota_reinstall_script.Save();

        quota_reinstall_script.RowStatus = TCAdmin.SDK.Objects.RowStatus.NewModified;
        quota_reinstall_script.ServiceEvent = TCAdmin.GameHosting.SDK.Objects.ServiceEvent.AfterCreate;
        quota_reinstall_script.GenerateKey();
        quota_reinstall_script.Save();

        quota_reinstall_script.RowStatus = TCAdmin.SDK.Objects.RowStatus.NewModified;
        quota_reinstall_script.ServiceEvent = TCAdmin.GameHosting.SDK.Objects.ServiceEvent.QueryMonitoring;
        quota_reinstall_script.GenerateKey();
        quota_reinstall_script.Save();

        //Create Delete Disk Quota script
        var delete_quota_action_script = new TCAdmin.GameHosting.SDK.Objects.GlobalGameScript();
        delete_quota_action_script.OperatingSystem = TCAdmin.SDK.Objects.OperatingSystem.Windows;
        delete_quota_action_script.ScriptEngineId = 8;
        delete_quota_action_script.Description = "[FSRM] Delete quota in File Server Resource Manager";
        delete_quota_action_script.ServiceEvent = TCAdmin.GameHosting.SDK.Objects.ServiceEvent.CustomAction;
        delete_quota_action_script.Name = "Delete Disk Quota";
        delete_quota_action_script.ExecuteInPopup = true;
        delete_quota_action_script.CustomFields["__TCA:MODULE"] = "File Server Resource Manager";
        delete_quota_action_script.CustomPrompt = "Delete this service's quota?";
        delete_quota_action_script.ScriptContents = @"Import-Module FileServerResourceManager

#Exit if variable is not set or set default quota if game is configured to do so.
if($ThisService.Variables.HasValue(""FSRM_DiskQuota"") -eq $false){
  write-host ([string]::Format(""{0} - Service doesn't have a quota"", $ThisService.ConnectionInfo))
  exit
}

write-host ([string]::Format(""{0} - Removing quota..."", $ThisService.ConnectionInfo))
Remove-FsrmQuota -Path $ThisService.RootDirectory -AsJob | Wait-Job
$ThisService.Variables[""FSRM_DiskQuota""] = $null
$ThisService.Save()
write-host ([string]::Format(""{0} - Quota removed successfully"", $ThisService.ConnectionInfo))";
        delete_quota_action_script.GenerateKey();
        delete_quota_action_script.Save();

        var delete_quota_service_deleted_script = new TCAdmin.GameHosting.SDK.Objects.GlobalGameScript();
        delete_quota_service_deleted_script.OperatingSystem = TCAdmin.SDK.Objects.OperatingSystem.Windows;
        delete_quota_service_deleted_script.ScriptEngineId = 8;
        delete_quota_service_deleted_script.Description = "[FSRM] Delete quota in File Server Resource Manager";
        delete_quota_service_deleted_script.ServiceEvent = TCAdmin.GameHosting.SDK.Objects.ServiceEvent.AfterDelete;
        delete_quota_service_deleted_script.Name = "Delete Disk Quota";
        delete_quota_service_deleted_script.IgnoreErrors = true;
        delete_quota_service_deleted_script.CustomFields["__TCA:MODULE"] = "File Server Resource Manager";
        delete_quota_service_deleted_script.ScriptContents = string.Format("$Script.Execute(0, {0})", delete_quota_action_script.ScriptId);
        delete_quota_service_deleted_script.GenerateKey();
        delete_quota_service_deleted_script.Save();


    }
}