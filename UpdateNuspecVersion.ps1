Param([string]$preRelease,[string]$major,[string]$minor)
$children = gci ./ -recurse *.nuspec 
foreach( $child in $children)
{
    $nuspecFile = gi $child.fullName
    [xml] $content = Get-Content $nuspecFile
    $end = Get-Date
    $start = Get-Date "5/17/2017"

    $today = Get-Date
    $today = $today.ToShortDateString()
    $today = Get-Date $today

	$minutes = New-TimeSpan -Start $today -End $end

    $versionPart = $content.package.metadata.version.Split('.')
    $revisionNumber = New-TimeSpan -Start $start -End $end
    
    if(!$major) {$major = $versionPart[0]}
    if(!$minor) {$minor = $versionPart[1]}

    if($preRelease){
      	$buildNumber = ("{0:00}" -f [math]::Round($minutes.Hours)) + ("{0:00}" -f ([math]::Round($minutes.Minutes)))
		$content.package.metadata.version = $major +"." + $minor + "." + $revisionNumber.Days + "-$preRelease" + ("{0:00000}" -f $buildNumber)
    }
    else{
        $content.package.metadata.version = $major +"." + $minor + ".0"
    }

    Write-Output $child.fullName + "Setting nuget package version: " + $content.package.metadata.version
	Write-Output "OK"
	$content.Save($child.FullName)
}

