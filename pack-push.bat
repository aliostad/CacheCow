del /F /Q .\artifacts\*.*
dotnet pack CacheCow.sln -o .\artifacts
dotnet nuget push "artifacts\*.nupkg"