$formsVersion = "3.6.0.220655"

$netstandardProject = ".\MaterialFrame\MaterialFrame\MaterialFrame.csproj"
$droidProject = ".\MaterialFrame\MaterialFrame.Android\MaterialFrame.Android.csproj"
$iosProject = ".\MaterialFrame\MaterialFrame.iOS\MaterialFrame.iOS.csproj"
$macOSProject = ".\MaterialFrame\MaterialFrame.macOS\MaterialFrame.macOS.csproj"
$uwpProject = ".\MaterialFrame\MaterialFrame.UWP\MaterialFrame.UWP.csproj"

$droidBin = ".\MaterialFrame\MaterialFrame.Android\bin\Release"
$droidObj = ".\MaterialFrame\MaterialFrame.Android\obj\Release"

echo "  Setting Xamarin.Forms version to $formsVersion"

$findXFVersion = '(Xamarin.Forms">\s+<Version>)(.+)(</Version>)'
$replaceString = "`$1 $formsVersion `$3"

(Get-Content $netstandardProject -Raw) -replace $findXFVersion, "$replaceString" | Out-File $netstandardProject
(Get-Content $droidProject -Raw) -replace $findXFVersion, "$replaceString" | Out-File $droidProject
(Get-Content $iosProject -Raw) -replace $findXFVersion, "$replaceString" | Out-File $iosProject
(Get-Content $macOSProject -Raw) -replace $findXFVersion, "$replaceString" | Out-File $macOSProject
(Get-Content $uwpProject -Raw) -replace $findXFVersion, "$replaceString" | Out-File $uwpProject

rm *.txt

echo "  deleting android bin-obj folders"
rm -Force -Recurse $droidBin
if ($LastExitCode -gt 0)
{
    echo "  Error deleting android bin-obj folders"
    return
}

rm -Force -Recurse $droidObj
if ($LastExitCode -gt 0)
{
    echo "  Error deleting android bin-obj folders"
    return
}

echo "  cleaning Sharpnado.MaterialFrame solution"
msbuild .\MaterialFrame\MaterialFrame.sln /t:Clean /p:Configuration=Release
if ($LastExitCode -gt 0)
{
    echo "  Error while cleaning solution"
    return
}

echo "  restoring Sharpnado.MaterialFrame solution"
msbuild .\MaterialFrame\MaterialFrame.sln /t:Restore /p:Configuration=Release
if ($LastExitCode -gt 0)
{
    echo "  Error while restoring solution"
    return
}

echo "  building Sharpnado.MaterialFrame solution"
msbuild .\MaterialFrame\MaterialFrame.sln /t:Build /p:Configuration=Release
if ($LastExitCode -gt 0)
{
    echo "  Error while building solution"
    return
}

echo "  deleting android obj folders"

rm -Force -Recurse $droidObj
if ($LastExitCode -gt 0)
{
    echo "  Error deleting android obj folder"
    return
}

echo "  cleaning Android9"
msbuild .\MaterialFrame\MaterialFrame.Android\MaterialFrame.Android.csproj /t:Clean /p:Configuration=ReleaseAndroid9.0
if ($LastExitCode -gt 0)
{
    echo "  Error while cleaning Android9"
    return
}

echo "  restoring Android9"
msbuild .\MaterialFrame\MaterialFrame.Android\MaterialFrame.Android.csproj /t:Restore /p:Configuration=ReleaseAndroid9.0
if ($LastExitCode -gt 0)
{
    echo "  Error while restoring Android9"
    return
}

echo "  building Android9"
msbuild .\MaterialFrame\MaterialFrame.Android\MaterialFrame.Android.csproj /t:Build /p:Configuration=ReleaseAndroid9.0
if ($LastExitCode -gt 0)
{
    echo "  Error while building Android9"
    return
}

$version = (Get-Item MaterialFrame\MaterialFrame\bin\Release\netstandard2.0\Sharpnado.MaterialFrame.dll).VersionInfo.FileVersion

echo "  packaging Sharpnado.MaterialFrame.nuspec (v$version)"
nuget pack .\Sharpnado.MaterialFrame.nuspec -Version $version