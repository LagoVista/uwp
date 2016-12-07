$scriptpath = Split-Path $MyInvocation.MyCommand.Path

$children = gci $scriptpath -recurse *.nupkg

# Add this key via control panel environment variables [NUGETAPIKEY] to be obtained from the LagoVista Nuget Account

foreach( $child in $children)
{
	Write-Output "Publishing Output $child.fullName" 
    nuget push $child.fullName -Source https://www.nuget.org/api/v2/package -ApiKey $env:NUGETAPIKEY
}
