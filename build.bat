@echo off
cd /d "%~dp0"
echo.
echo  YmapDedupe.exe olusturuluyor...
echo.

set CSC=%windir%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%windir%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo  HATA: Windows'un C# derleyicisi bulunamadi.
  echo  Beklenen yer: %windir%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
  pause
  exit /b 1
)

"%CSC%" /nologo /codepage:65001 /target:winexe /optimize+ /out:YmapDedupe.exe /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll YmapDedupe.cs

if errorlevel 1 (
  echo.
  echo  DERLEME HATASI! Yukaridaki hata satirlarini kopyalayip gonderin.
  pause
  exit /b 1
)

echo.
echo  TAMAM: YmapDedupe.exe olustu. Ayni klasorde bulabilirsiniz.
echo.
pause
