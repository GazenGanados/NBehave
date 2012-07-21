﻿
function nunitVersion() {
  return  (get-item src/packages/nunit* -Exclude nunit.runners*).Name -replace "Nunit."
}

function mbUnitVersion() {
  return (get-item src/packages/mbunit*).Name -replace "mbunit."
}

function xunitVersion() {
  return (get-item src/packages/xunit*).Name -replace "xunit."
}

function xmlPoke([string]$file, [string]$xpath, $value, [hashtable]$namespaces) {
    [xml] $fileXml = Get-Content $file
	$xmlNameTable = new-object System.Xml.NameTable
	$xmlNameSpace = new-object System.Xml.XmlNamespaceManager($xmlNameTable)

	foreach($key in $namespaces.keys)
	{
		$xmlNameSpace.AddNamespace($key, $namespaces.$key);
	}

    $node = $fileXml.SelectSingleNode($xpath, $xmlNameSpace)
    if ($node) {
        $node.InnerText = $value

        $fileXml.Save($file)
    }
}

function xmlPeek([string]$file, [string]$xpath, [hashtable]$namespaces) {
    [xml] $fileXml = Get-Content $file
	$xmlNameTable = new-object System.Xml.NameTable
	$xmlNameSpace = new-object System.Xml.XmlNamespaceManager($xmlNameTable)

	foreach($key in $namespaces.keys)
	{
		$xmlNameSpace.AddNamespace($key, $namespaces.$key);
	}

    $node = $fileXml.SelectSingleNode($xpath, $xmlNameSpace)
	return $node.InnerText
}

function xmlList([string]$file, [string]$xpath, [hashtable]$namespaces) {
    [xml] $fileXml = Get-Content $file
	$xmlNameTable = new-object System.Xml.NameTable
	$xmlNameSpace = new-object System.Xml.XmlNamespaceManager($xmlNameTable)

	foreach($key in $namespaces.keys)
	{
		$xmlNameSpace.AddNamespace($key, $namespaces.$key);
	}
  $nodes = @()
    $node = $fileXml.SelectNodes($xpath, $xmlNameSpace)
  $node | ForEach-Object { $nodes += @($_.Value)}

  return $nodes
}

function Run-ILMerge($directory, $outAssembly, $assemblies) {
  new-item -path $directory -name "temp_merge" -type directory -ErrorAction SilentlyContinue

  $outFile = "$directory\temp_merge\$outAssembly"
  Write-Host "$outFile"

  if($framework -eq "4.0") {
    Exec { .\tools\ilmerge\ilmerge.exe /out:$outFile $assemblies /targetplatform:"v4,$env:ProgramFiles\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0" }
  }
  else {
    Exec { .\tools\ilmerge\ilmerge.exe /out:$outFile $assemblies /targetplatform:"v2,$env:ProgramFiles\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5" }
  }

  Get-ChildItem "$directory\temp_merge\**" -Include *.dll, *.pdb | Copy-Item -Destination $directory
  Remove-Item "$directory\temp_merge" -Recurse -ErrorAction SilentlyContinue
}