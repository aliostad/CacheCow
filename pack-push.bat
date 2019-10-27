del /F /Q .\artifacts\*.*
dotnet pack CacheCow.sln -o .\artifacts -c Release
if "%1" == "" (
	dotnet nuget push "artifacts\*.nupkg" -n -s nuget.org )
if NOT "%1" == ""  (
	dotnet nuget push "artifacts\*.nupkg" -n -s nuget.org -k "%1" )