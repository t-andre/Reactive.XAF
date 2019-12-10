param(
    $Branch = "master",
    $nugetBin = "$PSScriptRoot\..\..\bin\Nupkg",
    $sourceDir = "$PSScriptRoot\..\..",
    $Filter ,
    [switch]$SkipReadMe,
    [string[]]$ChangedModules = @()
)
Import-Module XpandPwsh -Force -Prefix X
$ErrorActionPreference = "Stop"

New-Item $nugetBin -ItemType Directory -Force | Out-Null
Get-ChildItem $nugetBin | Remove-Item -Force -Recurse
$versionConverterSpecPath = "$sourceDir\Tools\Xpand.VersionConverter\Xpand.VersionConverter.nuspec"
if ($Branch -match "lab") {
    [xml]$versionConverterSpec = Get-XmlContent $versionConverterSpecPath
    $v = New-Object System.Version($versionConverterSpec.Package.metadata.version)
    if ($v.Revision -eq -1) {
        $versionConverterSpec.Package.metadata.version = "$($versionConverterSpec.Package.metadata.version).0"
    }
    $versionConverterSpec.Save($versionConverterSpecPath)
    
}
& (Get-XNugetPath) pack $versionConverterSpecPath -OutputDirectory $nugetBin -NoPackageAnalysis
if ($lastexitcode) {
    throw 
}


Set-Location $sourceDir
$assemblyVersions = & "$sourceDir\tools\build\AssemblyVersions.ps1" $sourceDir

# Get-ChildItem "$sourceDir\tools\nuspec" "Xpand*$filter*.nuspec" -Recurse | ForEach-Object {
$nuspecs = Get-ChildItem "$sourceDir\tools\nuspec" "Xpand.*$filter*.nuspec" -Exclude "*Tests*" -Recurse

$nugetPath = (Get-XNugetPath)

$packScript = {
    $name = $_.FullName
    $basePath = "$sourceDir\bin"
    if ($name -like "*Client*") {
        $basePath += "\ReactiveLoggerClient"
    }
    
    $packageName = [System.IO.Path]::GetFileNameWithoutExtension($_.FullName)
    $assemblyItem = $assemblyVersions | Where-Object { $_.name -eq $packageName }
    
    $version = $assemblyItem.Version
    if ($packageName -like "*All") {
        [xml]$coreNuspec = Get-Content "$sourceDir\tools\nuspec\$packagename.nuspec"
        $version = $coreNuspec.package.metadata.Version
    }
 
    Invoke-Script {
        Write-Output "$nugetPath pack $name -OutputDirectory $($nugetBin) -Basepath $basePath -Version $version " #-f Blue
        & $nugetPath pack $name -OutputDirectory $nugetBin -Basepath $basePath -Version $version
    }
    
}
$varsToImport = @("assemblyVersions", "SkipReadMe", "nugetPath", "sourceDir", "nugetBin", "SkipReadMe")
$conLimit = [System.Environment]::ProcessorCount
$nuspecs | Invoke-Parallel -LimitConcurrency $conLimit -VariablesToImport $varsToImport -Script $packScript
# $nuspecs | ForEach-Object { Invoke-Command $packScript -ArgumentList $_ }
function AddReadMe {
    param(
        $BaseName,
        $Directory
    )
    if ($BaseName -like "Xpand.XAF*") {
        $name = $_.BaseName.Replace("Xpand.XAF.Modules.", "")
        $id = "Xpand.XAF.Modules.$name.$name" + "Module"
        $message = @"
++++++++++++++++++++++++  ++++++++      ❇️ 🅴🆇🅲🅻🆄🆂🅸🆅🅴 🆂🅴🆁🆅🅸🅲🅴🆂?❇️
++++++++++++++++++++++##  ++++++++          https://github.com/sponsors/apobekiaris
++++++++++++++++++++++  ++++++++++      
++++++++++    ++++++  ++++++++++++      ➤  ɪғ ʏᴏᴜ ʟɪᴋᴇ ᴏᴜʀ ᴡᴏʀᴋ ᴘʟᴇᴀsᴇ ᴄᴏɴsɪᴅᴇʀ ᴛᴏ ɢɪᴠᴇ ᴜs ᴀ STAR. 
++++++++++++  ++++++  ++++++++++++          https://github.com/eXpandFramework/DevExpress.XAF/stargazers
++++++++++++++  ++  ++++++++++++++
++++++++++++++    ++++++++++++++++      ➤ ​​̲𝗣​̲𝗮​̲𝗰​̲𝗸​̲𝗮​̲𝗴​̲𝗲​̲ ​̲𝗻​̲𝗼​̲𝘁​̲𝗲​̲𝘀
++++++++++++++  ++  ++++++++++++++  
++++++++++++  ++++    ++++++++++++          ☞ Build the project before opening the model editor.
++++++++++  ++++++++  ++++++++++++          ☞ Documentation can be found @ https://github.com/eXpandFramework/DevExpress.XAF/wiki/$name".
++++++++++  ++++++++++  ++++++++++          ☞ The package only adds the required references. To install $id add the next line in the constructor of your XAF module.
++++++++  ++++++++++++++++++++++++              RequiredModuleTypes.Add(typeof($id));
++++++  ++++++++++++++++++++++++++
        
"@
        Set-Content "$Directory\ReadMe.txt" $message
    }
    else {
        Remove-Item "$Directory\ReadMe.txt" -Force -ErrorAction SilentlyContinue
    }
}
Get-ChildItem "$nugetBin" *.nupkg | ForEach-Object {
    $zip = "$($_.DirectoryName)\$($_.BaseName).zip" 
    Move-Item $_.FullName $zip
    $unzipDir = "$($_.DirectoryName)\$($_.BaseName)"
    Expand-Archive $zip $unzipDir
    Remove-Item $zip
    AddReadme $_.BaseName $unzipDir
    Compress-Files "$unzipDir" $zip 
    Move-Item $zip $_.FullName
    Remove-Item $unzipDir -Force -Recurse
}
if ($ChangedModules) {
    Write-HostFormatted "ChangedModules" -Section
    $ChangedModules
    $nupks = Get-ChildItem $nugetBin
    & (Get-NugetPath) list -source $nugetBin | ConvertTo-PackageObject|Where-Object{$_.Id -notlike "*.all.*"} | ForEach-Object {
        $p = $_
        if ($p.Id -notin $ChangedModules) {
            $nupks | Where-Object { $_.BaseName -eq "$($p.Id).$($p.Version)" }
        }
    } | Remove-Item
}
