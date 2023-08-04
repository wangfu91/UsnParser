$rids = "win-x64" , "win-arm64"

Push-Location .\UsnParser

foreach ($rid in $rids) {
    dotnet publish -c release -r $rid -o ..\publish\$rid --self-contained true
    Remove-Item ..\publish\$rid\UsnParser.pdb -Force
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