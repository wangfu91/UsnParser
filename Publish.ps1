Push-Location .\UsnParser

dotnet publish -c release -r win-x64 -o ../publish/win-x64 --self-contained

dotnet publish -c release -r win-x86 -o ../publish/win-x86 --self-contained

Pop-Location