del /F /Q .\artifacts\*.*
dotnet pack CacheCow.sln -o .\artifacts -c Release

IF NOT DEFINED [%api_key%]
	dotnet nuget push "artifacts\*.nupkg" -s nuget.org
ELSE
	dotnet nuget push "artifacts\*.nupkg" -s nuget.org -k $(api_key)