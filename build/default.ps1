#This build assumes the following directory structure
#
#  \Build          - This is where the project build code lives
#  \BuildArtifacts - This folder is created if it is missing and contains output of the build
#  \Code           - This folder contains the source code or solutions you want to build
#
Properties {
	$base_dir = 			resolve-path .
	$root_dir = 			resolve-path .\..
	$build_dir = 			"$base_dir\build"
	$release_dir = 			"$base_dir\release"
	$solution_dir = 		"$root_dir"
	$packageinfo_dir = 		"$base_dir\packaging"
	$tools_dir = 			"$base_dir\tools"
	$configuration =		"Release"
	$platform =				"Any CPU"
	. .\psake_ext.ps1
	$version = 				Get-Version-From-Git-Tag
}

FormatTaskName (("-"*25) + "[{0}]" + ("-"*25))

$framework = '4.0'

include .\psake_ext.ps1

Task Default -depends Package

Task Clean {
	Write-Host "Cleaning Build directory" -ForegroundColor Green
	if (Test-Path $build_dir) 
	{	
		rd $build_dir -rec -force | out-null
	}
	
	if (Test-Path $release_dir) 
	{	
		rd $release_dir -rec -force | out-null
	}
	
	Write-Host "Cleaning NDtw.sln" -ForegroundColor Green
	Exec { msbuild "$solution_dir\NDtw.sln" /t:Clean /p:Configuration=$configuration /p:Platform=$platform /v:quiet } 
}

Task Init -Depends Clean {	
	Write-Host "Initializing AssemblyInfo" -ForegroundColor Green

	. .\psake_ext.ps1
	
    Generate-Assembly-Info `
        -file "$solution_dir\NDtw\Properties\AssemblyInfo.cs" `
        -title "NDtw $version" `
        -description "NDtw - Dynamic Time Warping (DTW) algorithm" `
        -product "NDtw" `
        -version $version `
        -copyright "Darjan Oblak 2012"
		
	Generate-Assembly-Info `
        -file "$solution_dir\NDtw.Visualization.Wpf\Properties\AssemblyInfo.cs" `
        -title "NDtw Visualization Wpf $version" `
        -description "NDtw - Dynamic Time Warping (DTW) algorithm" `
        -product "NDtw Visualization Wpf $version" `
        -version $version `
        -copyright "Darjan Oblak 2012"
		
	Generate-Assembly-Info `
        -file "$solution_dir\NDtw.Examples\Properties\AssemblyInfo.cs" `
        -title "NDtw Examples $version" `
        -description "NDtw - Dynamic Time Warping (DTW) algorithm" `
        -product "NDtw Examples $version" `
        -version $version `
        -copyright "Darjan Oblak 2012"
		
			
	new-item $release_dir -itemType directory 
	new-item $build_dir -itemType directory 
}


Task Release -Depends Init {	
	Write-Host "Building NDtw.sln" -ForegroundColor Green
	Exec { msbuild "$solution_dir\NDtw.sln" /t:Build /p:Configuration=$configuration /p:Platform=$platform /v:quiet /p:OutDir="$build_dir/" } 
}

task Package -depends Release {
  $spec_files = @(Get-ChildItem $packageinfo_dir)
  foreach ($spec in $spec_files)
  {
    & $tools_dir\NuGet.exe pack $spec.FullName -o $release_dir -Version $version -Symbols -BasePath $base_dir
  }
}