@echo off

rmdir /s /q ".\Game\bin\Release\"

dotnet publish -p:PublishProfile=Publish

:: xcopy /e /i /h ".\Crux\Assets\" ".\Game\bin\Release\net8.0\win-x64\Crux\Assets\"
:: xcopy /e /i /h ".\Game\Assets\" ".\Game\bin\Release\net8.0\win-x64\Game\Assets\"

".\Game\bin\Release\net8.0\win-x64\Game.exe"
