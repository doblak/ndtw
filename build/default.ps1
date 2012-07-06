#This build assumes the following directory structure
#
#  \Build          - This is where the project build code lives
#  \BuildArtifacts - This folder is created if it is missing and contains output of the build
#  \Code           - This folder contains the source code or solutions you want to build
#
Properties {
	$base_dir = 			resolve-path .
	$root_dir = 			resolve-path .\..
	$build_dir = 			"$base_dir\Build"
	$solution_dir = 		"$root_dir"
	$configuration =		"Release"
	$platform =				"Any CPU"
	$version = 				. .\psake_ext.ps1 Get-Version-From-Git-Tag
}

FormatTaskName (("-"*25) + "[{0}]" + ("-"*25))

Task Default -depends Build

Task Clean {
	Write-Host "Creating Build directory" -ForegroundColor Green
	if (Test-Path $build_dir) 
	{	
		rd $build_dir -rec -force | out-null
	}
	
	mkdir $build_dir | out-null
	
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
}


Task Build -Depends Init {	
	Write-Host "Building NDtw.sln" -ForegroundColor Green
	Exec { msbuild "$solution_dir\NDtw.sln" /t:Build /p:Configuration=$configuration /p:Platform=$platform /v:quiet /p:OutDir="$build_dir/" } 
}