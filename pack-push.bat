del /F /Q .\artifacts\*.*
dotnet pack -o ..\..\artifacts
dotnet nuget push "artifacts\*.nupkg" -s nuget.org