del /F /Q .\artifacts\*.*
dotnet pack CacheCow.sln -o .\artifacts -c Release
if "%1" == "" (
	echo dotnet nuget push "artifacts\*.nupkg" -s nuget.org --no-symbols
	dotnet nuget push "artifacts\*.nupkg" -s nuget.org --no-symbols )
if NOT "%1" == ""  (
	echo dotnet nuget push "artifacts\*.nupkg" -s nuget.org --no-symbols --api-key "%1"
	dotnet nuget push "artifacts\*.nupkg" -s nuget.org --no-symbols --api-key "%1" 
	)