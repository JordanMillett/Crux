@echo off

rmdir /s /q ".\Game\bin\Release\"
rmdir /s /q ".\Game\bin\Builds\"

:: win-x64
dotnet publish Game\Game.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishTrimmed=false -o Game\bin\Builds\win-x64-dotnet-single-file\
dotnet publish Game\Game.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o Game\bin\Builds\win-x64-standalone-single-file\
dotnet publish Game\Game.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -p:PublishTrimmed=false -o Game\bin\Builds\win-x64-dotnet-multi-file\
dotnet publish Game\Game.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -o Game\bin\Builds\win-x64-standalone-multi-file\

:: win-x86
dotnet publish Game\Game.csproj -c Release -r win-x86 --self-contained false -p:PublishSingleFile=true -p:PublishTrimmed=false -o Game\bin\Builds\win-x86-dotnet-single-file\
dotnet publish Game\Game.csproj -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o Game\bin\Builds\win-x86-standalone-single-file\
dotnet publish Game\Game.csproj -c Release -r win-x86 --self-contained false -p:PublishSingleFile=false -p:PublishTrimmed=false -o Game\bin\Builds\win-x86-dotnet-multi-file\
dotnet publish Game\Game.csproj -c Release -r win-x86 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -o Game\bin\Builds\win-x86-standalone-multi-file\

:: linux-x64
:: dotnet publish Game\Game.csproj -c Release -r linux-x64 --self-contained false -p:PublishSingleFile=true -p:PublishTrimmed=false -o Game\bin\Builds\linux-x64-dotnet-single-file\
:: dotnet publish Game\Game.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o Game\bin\Builds\linux-x64-standalone-single-file\
:: dotnet publish Game\Game.csproj -c Release -r linux-x64 --self-contained false -p:PublishSingleFile=false -p:PublishTrimmed=false -o Game\bin\Builds\linux-x64-dotnet-multi-file\
:: dotnet publish Game\Game.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -o Game\bin\Builds\linux-x64-standalone-multi-file\