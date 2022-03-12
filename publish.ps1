$rids = "win-x64", "win-x86"

Push-Location .\UsnParser

foreach ($rid in $rids) {
    dotnet publish -c release -r $rid --self-contained -o ..\publish\$rid /p:PublishSingleFile=true /p:PublishTrimmed=true
}

Pop-Location

New-Item -ItemType "directory" -Name "release" -Force

foreach ($rid in $rids) {
    $path = ".\publish\$rid"
    
    $compress = @{
        Path             = $path
        CompressionLevel = "Optimal"
        DestinationPath  = ".\release\UsnParser-$rid.zip"
    }

    Compress-Archive @compress -Force
}