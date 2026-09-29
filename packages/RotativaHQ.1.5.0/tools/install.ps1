#
# install.ps1
#
param($rootPath, $toolsPath, $package, $project)
 
# Bail out if scaffolding is disabled (probably because you're running an incompatible version of NuGet)
#if (-not (Get-Command Invoke-Scaffolder)) { return }
 
# Could use "Set-DefaultScaffolder" here if desired
 
#set default version of MVC to 5.0
$MvcVersion = "5.0.0.0"
 
#find out the version of MVC assembly in the target project
$project.Object.References | Where-Object { $_.Name -eq 'System.Web.Mvc' } | ForEach-Object { $MvcVersion = $_.Version}
Write-Host "MVC version: " $MvcVersion
 
#remove unnecessary Korzh.EasyQuery.MvcX assemblies from project references
if ($MvcVersion.StartsWith("4.")) {
    $project.Object.References | Where-Object { ($_.Name.StartsWith("RotativaHQ.MVC")) -and !($_.Name.StartsWith("RotativaHQ.MVC4")) } | ForEach-Object { 
        $_.Remove()
    }
}
elseif ($MvcVersion.StartsWith("5.")) {
    $project.Object.References | Where-Object { ($_.Name.StartsWith("RotativaHQ.MVC")) -and !($_.Name.StartsWith("RotativaHQ.MVC5")) } | ForEach-Object { 
        $_.Remove()
    }
}
