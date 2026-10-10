@echo off
rem Recompila AQTAgent.dll y AQTrainer.exe con el compilador de .NET Framework que ya trae Windows.
rem Uso:  build.bat ["C:\ruta\a\All Quiet in the Trenches"]
setlocal
set "GAME=H:\SteamLibrary\steamapps\common\All Quiet in the Trenches"
if not "%~1"=="" set "GAME=%~1"
set "M=%GAME%\All Quiet in the Trenches_Data\Managed"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%M%\Assembly-CSharp.dll" ( echo No encuentro "%M%\Assembly-CSharp.dll". Pasa la carpeta del juego como argumento. & exit /b 1 )
if not exist "%CSC%" ( echo No encuentro csc.exe de .NET Framework. & exit /b 1 )

echo Compilando AQTAgent.dll ...
"%CSC%" /nologo /noconfig /nostdlib+ /codepage:65001 /langversion:5 /target:library /optimize+ /out:AQTAgent.dll ^
  /r:"%M%\mscorlib.dll" /r:"%M%\System.dll" /r:"%M%\System.Core.dll" ^
  /r:"%M%\Assembly-CSharp.dll" /r:"%M%\UnityEngine.dll" /r:"%M%\UnityEngine.CoreModule.dll" ^
  /r:"%M%\UnityEngine.IMGUIModule.dll" /r:"%M%\UnityEngine.InputLegacyModule.dll" ^
  /r:"%M%\UnityEngine.TextRenderingModule.dll" /r:"%M%\Unity.TextMeshPro.dll" /r:"%M%\UnityEngine.UI.dll" ^
  /r:"%M%\Ink-Libraries.dll" /r:"%M%\Cinemachine.dll" /r:"%M%\Kybernetik.UltEvents.dll" /r:"%M%\netstandard.dll" ^
  agent\*.cs
if errorlevel 1 exit /b 1

echo Compilando AQTrainer.exe ...
"%CSC%" /nologo /noconfig /nostdlib+ /target:exe /platform:anycpu /optimize+ /out:AQTrainer.exe ^
  /r:"%M%\mscorlib.dll" /r:"%M%\System.dll" /r:"%M%\System.Core.dll" ^
  injector\*.cs
if errorlevel 1 exit /b 1

echo Listo.
