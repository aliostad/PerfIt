del /F /Q .\artifacts\*.*
dotnet pack PerfIt.sln -o ..\..\artifacts
dotnet nuget push "artifacts\*.nupkg" -s nuget.org