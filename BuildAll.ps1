$scriptPath = Split-Path $MyInvocation.MyCommand.Path
Set-Location $scriptPath
dotnet build --configuration release --runtime x86
dotnet build --configuration release --runtime x64
dotnet build --configuration release --runtime ARM
dotnet build --configuration release --runtime Release 