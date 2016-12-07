#. ./BuildAll.ps1

$scriptPath = Split-Path $MyInvocation.MyCommand.Path

. ./UpdateNuspecVersion.ps1 -preRelease alpha -major 0 -minor 8

$oldChildren = gci $scriptPath -Recurse *.nupkg
foreach( $oldChild in $oldChildren)
{
	Remove-Item $oldChild -Recurse
}

$children = gci $scriptPath -recurse *.nuspec
foreach( $child in $children)
{
	$projectPath = Split-Path $child.FullName

	Write-Output $child.FullName
	nuget pack -OutputDirectory D:\LocalNuget $child.FullName
	nuget pack $child.FullName
}