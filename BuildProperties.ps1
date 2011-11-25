properties {
	$rootDir            = Split-Path $psake.build_script_file
	$sourceDir          = "$rootDir\src"
	$toolsDir           = "$rootDir\tools"
	$buildDir           = "$rootDir\build"
	$testReportsDir     = "$buildDir\test-reports"
	$testDir            = "$buildDir\Debug-$frameworkVersion\UnitTests"
	$installerDir       = "$buildDir\Installer"
	$packageTemplateDir = "$rootDir\nuget"
	$tempNugetDir       = "$buildDir\temp_nuget"
	$artifactsDir       = "$buildDir\Artifacts"
	$exclusions         = @("*JetBrains*", "*Microsoft*", "log4net.dll", "NAnt.Core.dll", "TestDriven.Framework.dll", "*.pdb", "*.xml")
}