$module = 'PlcGhostBuster'



if ((Test-Path ".\Output") -eq $false) {mkdir "Output" | Out-Null}
if ((Test-Path ".\Output\$module") -eq $false) {mkdir ".\Output\$module"  | Out-Null}



dotnet build .\src\$module.csproj
dotnet publish .\src\$module.csproj -o .\Output\$module\

$manifestSplat = @{
    PowerShellVersion = '5.1'
    Path              = ".\Output\$module\$module.psd1"
    Author            = 'Nimrod Ken Dror'
    NestedModules     = @("PlcGhostBuster.dll")
    CmdletsToExport = @('Get-PlcControllerTags', 'Get-PlcTagValue', 'Set-PlcTagValue')
}

New-ModuleManifest @manifestSplat