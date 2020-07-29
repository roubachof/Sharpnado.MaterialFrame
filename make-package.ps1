$formsVersion = "3.6.0.220655"

$netstandardProject = ".\MaterialFrame\MaterialFrame\MaterialFrame.csproj"
$droidProject = ".\MaterialFrame\MaterialFrame.Android\MaterialFrame.Android.csproj"
$iosProject = ".\MaterialFrame\MaterialFrame.iOS\MaterialFrame.iOS.csproj"
$uwpProject = ".\MaterialFrame\MaterialFrame.UWP\MaterialFrame.UWP.csproj"

echo "  Setting Xamarin.Forms version to $formsVersion"

$findXFVersion = '(Xamarin.Forms">\s+<Version>)(.+)(</Version>)'
$replaceString = "`$1 $formsVersion `$3"

(Get-Content $netstandardProject -Raw) -replace $findXFVersion, "$replaceString" | Out-File $netstandardProject
(Get-Content $droidProject -Raw) -replace $findXFVersion, "$replaceString" | Out-File $droidProject
(Get-Content $iosProject -Raw) -replace $findXFVersion, "$replaceString" | Out-File $iosProject
(Get-Content $uwpProject -Raw) -replace $findXFVersion, "$replaceString" | Out-File $uwpProject

echo "  cleaning Sharpnado.MaterialFrame solution"
$errorCode = msbuild .\MaterialFrame\MaterialFrame.sln /t:Clean /p:Configuration=Release
if ($errorCode -gt 0)
{
    echo "  Error while cleaning solution"
    return 1
}

echo "  restoring Sharpnado.MaterialFrame solution"
$errorCode = msbuild .\MaterialFrame\MaterialFrame.sln /t:Restore /p:Configuration=Release
if ($errorCode -gt 0)
{
    echo "  Error while restoring solution"
    return 1
}

echo "  building Sharpnado.MaterialFrame solution"
$errorCode = msbuild .\MaterialFrame\MaterialFrame.sln /t:Build /p:Configuration=Release
if ($errorCode -gt 0)
{
    echo "  Error while building solution"
    return 1
}


echo "  cleaning Android9"
$errorCode = msbuild .\MaterialFrame\MaterialFrame.Android\MaterialFrame.Android.csproj /t:Clean /p:Configuration=ReleaseAndroid9.0
if ($errorCode -gt 0)
{
    echo "  Error while cleaning Android9"
    return 1
}

echo "  building Android9"
$errorCode = msbuild .\MaterialFrame\MaterialFrame.Android\MaterialFrame.Android.csproj /t:Build /p:Configuration=ReleaseAndroid9.0
if ($errorCode -gt 0)
{
    echo "  Error while building Android9"
    return 1
}

$version = (Get-Item MaterialFrame\MaterialFrame\bin\Release\netstandard2.0\Sharpnado.MaterialFrame.dll).VersionInfo.FileVersion

echo "  packaging Sharpnado.MaterialFrame.nuspec (v$version)"
nuget pack .\Sharpnado.MaterialFrame.nuspec -Version $version