$scriptPath = Split-Path $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# Need to download MSBuild From: https://www.microsoft.com/en-us/download/confirmation.aspx?id=40760 

$MSBUILD = 'C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe'
$COREPROJ = "src\LagoVista.Core.UWP\Core UWP.csproj"
$UIPROJ =   "src\LagoVista.UWP.UI\UWP UI.csproj"

& $MSBUILD "$ScriptPath\$COREPROJ" /p:Configuration=Release
& $MSBUILD "$ScriptPath\$COREPROJ" /p:Configuration=Release /p:Platform=ARM
& $MSBUILD "$ScriptPath\$COREPROJ" /p:Configuration=Release /p:Platform=x86
& $MSBUILD "$ScriptPath\$COREPROJ" /p:Configuration=Release /p:Platform=x64

& $MSBUILD "$ScriptPath\$UIPROJ" /p:Configuration=Release
& $MSBUILD "$ScriptPath\$UIPROJ" /p:Configuration=Release /p:Platform=ARM
& $MSBUILD "$ScriptPath\$UIPROJ" /p:Configuration=Release /p:Platform=x86
& $MSBUILD "$ScriptPath\$UIPROJ" /p:Configuration=Release /p:Platform=x64
