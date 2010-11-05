Include "versioning.ps1"
Include "psake_ext.ps1"

Properties {
	$project_dir         = $psake.build_script_file.Directory.Parent.FullName
	$solution_dir        = "$project_dir\src"
	$build_dir           = "$project_dir\build"
	$test_dir            = "$build_dir\Debug-$framework\UnitTests"
	$project_name        = "NBehave"
	$project_config      = "release"
	$version             = GetVersion "$project_dir\version" $false
}

Task default -Depends RunBuild

Task RunBuild {
	Invoke-Task "Version"
	Invoke-Task "Compile"
	Invoke-Task "Test"
	Invoke-Task "Distribute"
	Invoke-Task "ILMerge"
}

Task Clean {
	if(Test-Path $build_dir)
	{
		Remove-Item $build_dir -Recurse
	}	
}

Task Increment {
	GetVersion "$project_dir\version" $true
}

Task Version {
	Generate-Assembly-Info true $project_name $project_name $project_name $version "$solution_dir\CommonAssemblyInfo.cs"
}

Task Compile {
	Exec { msbuild "$project_dir\src\NBehave.sln" /property:Configuration=AutomatedDebug-$framework /v:m /p:TargetFrameworkVersion=v$framework /toolsversion:$framework }
}

Task Test {
	new-item -path "${build_dir}" -name "test-reports" -type directory -ErrorAction SilentlyContinue
	
	$arguments = Get-Item "$test_dir\*Specifications*.dll"
	Exec { ..\tools\nunit\nunit-console-x86.exe $arguments /xml:${build_dir}\test-reports\UnitTests-$framework.xml}
}

Task Distribute -depends DistributeVSPlugin, DistributeBinaries, DistributeExamples

Task DistributeVSPlugin -precondition { return $framework -eq "4.0" }{
	
	new-item -path "${build_dir}" -name "plugin" -type directory -ErrorAction SilentlyContinue
	$destination = "$build_dir\plugin\"
	$source = "$build_dir\Debug-$framework\VSPlugin"
	
	Get-ChildItem "$source\*.*" -Include *.dll, *.vsixmanifest, *.pkgdef, *.pdb | Copy-Item -Destination $destination
	
	$namespaces = @{ "vsx" = "http://schemas.microsoft.com/developer/vsx-schema/2010"}
	$xpath = "/vsx:Vsix/vsx:Identifier/vsx:"
	xmlPoke "$destination\extension.vsixmanifest" $xpath"InstalledByMsi" "true" $namespaces
	xmlPoke "$destination\extension.vsixmanifest" $xpath"Version" $version $namespaces
}

Task DistributeBinaries {
	new-item -path "${build_dir}" -name "dist" -type directory -ErrorAction SilentlyContinue
	new-item -path "${build_dir}" -name "dist\v$framework" -type directory -ErrorAction SilentlyContinue

	$destination = "$build_dir\dist\v$framework"
	$source = "$build_dir\Debug-$framework\NBehave"
	$exclusions = @("*Microsoft*", "log4net.dll", "NAnt.Core.dll", "TestDriven.Framework.dll")
	
	Get-ChildItem "$source\*.*" -Include *NBehave*, *.dll -Exclude $exclusions | Copy-Item -Destination $destination
}

Task DistributeExamples -precondition { return $framework -eq "3.5" }{
	$examplesdest_dir = "$build_dir\Examples\"
	$examplessource_dir = "$solution_dir\NBehave.Examples"
	
	Remove-Item $examplesdest_dir -Recurse -ErrorAction SilentlyContinue
	New-Item -Path $build_dir -Name "Examples" -type directory -ErrorAction SilentlyContinue
	
	$exclusions = @("**\bin\**\*.*", "*/obj/*")
	
	$items = Get-ChildItem $examplessource_dir -Recurse 
	$items = $items | where {$_.FullName -notmatch "obj" -and $_.FullName -notmatch "bin"} 
	$items | Copy-Item -Destination {Join-Path $examplesdest_dir $_.FullName.Substring($examplessource_dir.length)}

	$examplesdest_dir_lib = "$examplesdest_dir\lib\"
	New-Item -Path $examplesdest_dir -Name "lib" -type directory
	Get-ChildItem "$build_dir\dist\v3.5\*.*" -Include *NBehave*, *.dll | Copy-Item -Destination $examplesdest_dir_lib
	
	$namespaces = @{ "xsi" = "http://schemas.microsoft.com/developer/msbuild/2003"}
	$xpath = "//xsi:Reference/xsi:HintPath/../@Include"
	
	$path = "$examplesdest_dir\NBehave.Examples.csproj" 
	$nodes = xmlList $path $xpath $namespaces
	
	foreach($node in $nodes)
	{
		$xpath = "//xsi:Reference[@Include='$node']/xsi:HintPath"
		$pathToReference = xmlPeek $path $xpath $namespaces
		$dllFile = [regex]::Match($pathToReference, "(?<dllfile>(\w+\.)+dll$)").Groups["dllfile"].Value
		
		$xpath = "//xsi:Reference[@Include='$node']/xsi:HintPath"
		xmlPoke $path $xpath "lib\$dllfile" $namespaces
	}
	
	zip "$build_dir\NBehave.Examples.zip" $examplesdest_dir
}

Task BuildInstaller {
	Exec { ..\tools\nsis\makensis.exe /DVERSION=$version "$solution_dir\Installer\NBehave.nsi"}
}

Task ILMerge {
	$key = "$solution_dir\NBehave.snk"
	$directory = "$build_dir\dist\v$framework"
	$name = "NBehave.Narrator.Framework"
	
	$assemblies = @("$directory\gherkin.dll", 
					"$directory\NBehave.Spec.Framework.dll", 
					"$directory\Should.Core.dll", 
					"$directory\Should.Fluent.dll")
	
	ilmerge $key $directory $name $assemblies "dll"
}