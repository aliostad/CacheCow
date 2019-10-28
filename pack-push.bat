del /F /Q .\artifacts\*.*
dotnet pack CacheCow.sln -o .\artifacts -c Release
if "%1" == "" (
	dotnet nuget push "artifacts\*.nupkg" -s nuget.org --no-symbols )
if NOT "%1" == ""  (
	dotnet nuget push "artifacts\*.nupkg" -s nuget.org --api-key "%1" --no-symbols
	)