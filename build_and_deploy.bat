call .\rebuild_content
dotnet publish .\LudJam\ -c Release -r win-x64 /p:PublishReadyToRun=false /p:TieredCompilation=false --self-contained
del .\LudJam\bin\Release\net6.0\win-x64\publish\*.pdb
butler push .\LudJam\bin\Release\net6.0\win-x64\publish notexplosive/pet-the-cat:windows