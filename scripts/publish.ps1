param(
	$nugetApiKey = "$env:NUGET_API_KEY"
)

function require-param { 
    param($value, $paramName)
    
    if($value -eq $null) { 
        write-error "The parameter -$paramName is required."
    }
}

require-param $nugetApiKey -paramName "nugetApiKey"

#safely find the solutionDir
$ps1Dir = (Split-Path -parent $MyInvocation.MyCommand.Definition)
$solutionDir = Split-Path -Path $ps1Dir -Parent
$nugetExePath = resolve-path(join-path $solutionDir ".nuget")

$packages = dir "$solutionDir\artifacts\packages\CacheCow.*.nupkg"

foreach($package in $packages) { 
    #$package is type of System.IO.FileInfo
    & "$nugetExePath\Nuget.exe" push $package.FullName $nugetApiKey
}