Push-Location .\UsnParser

dotnet publish -c release -r win-x64 -o ../publish/win-x64

dotnet publish -c release -r win-x86 -o ../publish/win-x86

Pop-Location