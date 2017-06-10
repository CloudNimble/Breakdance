@echo off
echo Would you like to push the packages to NuGet when finished?
set /p choice="Enter y/n: "

@echo on
if /i %choice% equ y (
    ".nuget/nuget.exe" push src\AdvancedREI.Breakdance\bin\debug\AdvancedREI.Breakdance.*.nupkg -Source https://www.nuget.org/api/v2/package
    ".nuget/nuget.exe" push src\AdvancedREI.Breakdance.WebApi\bin\debug\AdvancedREI.Breakdance.WebApi.*.nupkg -Source https://www.nuget.org/api/v2/package
    ".nuget/nuget.exe" push src\AdvancedREI.Breakdance.Restier\bin\debug\AdvancedREI.Breakdance.Restier.*.nupkg -Source https://www.nuget.org/api/v2/package
)
pause