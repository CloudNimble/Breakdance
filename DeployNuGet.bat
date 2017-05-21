@echo off
echo Would you like to push the packages to NuGet when finished?
set /p choice="Enter y/n: "

set version=1.0.0-beta2g

del builds\nuget\*.nupkg
@echo on
".nuget\nuget.exe" pack src\AdvancedREI.Restier.Testier\AdvancedREI.Restier.Testier.nuspec -OutputDirectory builds\nuget\ -version %version% -symbols
if /i %choice% equ y (
    ".nuget/nuget.exe" push builds\nuget\AdvancedREI.Restier.Testier.*.nupkg -Source https://www.nuget.org/api/v2/package
)
pause