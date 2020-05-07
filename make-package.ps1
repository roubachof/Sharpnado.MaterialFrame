$formsVersion = "3.4.0.1008975"

echo "  <<<< WARNING >>>>> You need to launch 2 times this script to make sure Xamarin.Forms version was correctly resolved..."

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

echo "  building Sharpnado.MaterialFrame solution"
msbuild .\MaterialFrame\MaterialFrame.sln /t:Clean,Restore,Build /p:Configuration=Release > build.txt

echo "  building Android9"
msbuild .\MaterialFrame\MaterialFrame.Android\MaterialFrame.Android.csproj /t:Clean,Restore,Build /p:Configuration=ReleaseAndroid9.0 > build.Android9.txt

$version = (Get-Item MaterialFrame\MaterialFrame\bin\Release\netstandard2.0\Sharpnado.MaterialFrame.dll).VersionInfo.FileVersion

echo "  packaging Sharpnado.MaterialFrame.nuspec (v$version)"
nuget pack .\Sharpnado.MaterialFrame.nuspec -Version $version