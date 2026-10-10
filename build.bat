@echo off
rem Rebuilds AQTAgent.dll and AQTrainer.exe with the .NET Framework compiler that ships with Windows.
rem Usage:  build.bat ["C:\path\to\All Quiet in the Trenches"]
setlocal
set "GAME=H:\SteamLibrary\steamapps\common\All Quiet in the Trenches"
if not "%~1"=="" set "GAME=%~1"
set "M=%GAME%\All Quiet in the Trenches_Data\Managed"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%M%\Assembly-CSharp.dll" ( echo Cannot find "%M%\Assembly-CSharp.dll". Pass the game folder as an argument. & exit /b 1 )
if not exist "%CSC%" ( echo Cannot find the .NET Framework csc.exe. & exit /b 1 )

echo Building AQTAgent.dll ...
"%CSC%" /nologo /noconfig /nostdlib+ /codepage:65001 /langversion:5 /target:library /optimize+ /out:AQTAgent.dll ^
  /r:"%M%\mscorlib.dll" /r:"%M%\System.dll" /r:"%M%\System.Core.dll" ^
  /r:"%M%\Assembly-CSharp.dll" /r:"%M%\UnityEngine.dll" /r:"%M%\UnityEngine.CoreModule.dll" ^
  /r:"%M%\UnityEngine.IMGUIModule.dll" /r:"%M%\UnityEngine.InputLegacyModule.dll" ^
  /r:"%M%\UnityEngine.TextRenderingModule.dll" /r:"%M%\Unity.TextMeshPro.dll" /r:"%M%\UnityEngine.UI.dll" ^
  /r:"%M%\Ink-Libraries.dll" /r:"%M%\Cinemachine.dll" /r:"%M%\Kybernetik.UltEvents.dll" /r:"%M%\netstandard.dll" ^
  agent\*.cs
if errorlevel 1 exit /b 1

echo Building AQTrainer.exe ...
"%CSC%" /nologo /noconfig /nostdlib+ /target:exe /platform:anycpu /optimize+ /out:AQTrainer.exe ^
  /r:"%M%\mscorlib.dll" /r:"%M%\System.dll" /r:"%M%\System.Core.dll" ^
  injector\*.cs
if errorlevel 1 exit /b 1

echo Done.
