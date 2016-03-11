:: build params

SET platform=%1%
SET configuration=%2%

:: copy freerdp dlls to wfreerdp output folder

copy ..\libfreerdp\%platform%\%configuration%\*.dll %platform%\%configuration%
copy ..\libfreerdpchanman\%platform%\%configuration%\*.dll %platform%\%configuration%
copy *.dll %platform%\%configuration%

:: copy freerdp dlls and exe to Myrtille.Services output folder

copy ..\libfreerdp\%platform%\%configuration%\*.dll ..\..\..\Myrtille.Services\bin\%configuration%
copy ..\libfreerdpchanman\%platform%\%configuration%\*.dll ..\..\..\Myrtille.Services\bin\%configuration%
copy *.dll ..\..\..\Myrtille.Services\bin\%configuration%
copy %platform%\%configuration%\*.exe ..\..\..\Myrtille.Services\bin\%configuration%
