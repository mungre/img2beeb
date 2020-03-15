@echo off
set CSC=c:\windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
pushd %~dp0
if NOT EXIST %CSC% (
  echo C# compiler not found at "%CSC%"
  goto :exit
)
if NOT EXIST bin mkdir bin
%CSC% -optimize -out:bin\img2beeb.exe src\Program.cs src\Properties\AssemblyInfo.cs -reference:WPF\WindowsBase.dll -reference:WPF\PresentationCore.dll -reference:System.Xaml.dll
if NOT EXIST gifs mkdir gifs
if NOT EXIST mode2 mkdir mode2
if EXIST bin\img2beeb.exe bin\img2beeb.exe
:exit
popd
