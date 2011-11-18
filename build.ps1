param($version = "0.1.0.0", $frameworkVersion = "4.0")

Include ".\BuildProperties.ps1"
Include ".\buildframework\psake_ext.ps1"

task Init -depends Clean, Version
task Default -depends Test #, ILMerge

task Clean { 
	if ($true -eq (Test-Path "$buildDir")) {
		Remove-Item $buildDir -Recurse
	}
	New-Item $buildDir -type directory
	New-Item $testReportsDir -type directory
}

Task Version {
	if ($environment -ne "Dev")  {
		$asmInfo = "$sourceDir\CommonAssemblyInfo.cs"
		$src = Get-Content $asmInfo
		$newSrc = foreach($row in $src) { 
			if ($row -match 'Assembly((Version)|(FileVersion))\s*\(\s*"\d+\.\d+\.\d+\.\d+"\s*\)') { 
				$row -replace "\d+\.\d+\.\d+\.\d+", $version 
			}
			else { $row }
		}
		Set-Content -path $asmInfo -value $newSrc			
	}
}

Task Compile -depends Compile-Console-x86, CompileAnyCpu

Task CompileAnyCpu {
	Exec { msbuild "$sourceDir\NBehave.sln" /p:Configuration=AutomatedDebug-$frameworkVersion /v:m /p:TargetFrameworkVersion=v$frameworkVersion /toolsversion:$frameworkVersion /t:Rebuild }
}

Task Compile-Console-x86 {
	$params = "Configuration=AutomatedDebug-$frameworkVersion-x86;Platform=x86;OutputPath=$buildDir\Debug-$frameworkVersion\NBehave\"
	Exec { msbuild "$sourceDir\NBehave.Console\NBehave.Console.csproj" /p:$params /p:TargetFrameworkVersion=v$frameworkVersion /v:m /toolsversion:$frameworkVersion /t:Rebuild }
	Move-Item "$buildDir\Debug-$frameworkVersion\NBehave\NBehave-Console.exe" "$buildDir\Debug-$frameworkVersion\NBehave\NBehave-Console-x86.exe" -Force
}

Task ILMerge -depends Compile {
	$snk = "$sourceDir\NBehave.snk"
	$buildDirFramework = "$buildDir\Debug-$frameworkVersion"
	$directory = "$buildDirFramework\NBehave"
	$resharperDir = "$buildDirFramework\Resharper"
	$out = "NBehave.Narrator.Framework.dll"
	$assemblies = @("$directory\NBehave.Narrator.Framework.dll", "$directory\Gherkin.dll", "$directory\NBehave.Gherkin.dll")
	
	Run-ILMerge $snk $directory $out $assemblies
	Remove-Item "$directory\Gherkin.dll"
	Remove-Item "$directory\NBehave.Gherkin.dll"
}

Task Test -depends Compile {
	new-item $testReportsDir -type directory -ErrorAction SilentlyContinue
	
	$arguments = Get-Item "$testDir\*Specifications*.dll"
	Exec { .\tools\nunit\nunit-console-x86.exe $arguments /xml:$testReportsDir\UnitTests-$frameworkVersion.xml}
}

