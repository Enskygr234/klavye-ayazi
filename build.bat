@echo off
rem Tek dosyalik klavye-ayazi.exe uretir: EXE\klavye-ayazi.exe
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o EXE
if errorlevel 1 (
  echo Derleme basarisiz.
  exit /b 1
)
echo.
echo Hazir: EXE\klavye-ayazi.exe
