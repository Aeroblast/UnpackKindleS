cd /d %~dp0
rd /Q /S bin

cd src
rd /Q /S bin

dotnet publish -c release -r win-x64 --self-contained

md ..\bin
move bin\release\netcoreapp3.0\win-x64\publish ..\bin\app

md ..\bin\app\template\
copy template\template_cover.txt ..\bin\app\template\template_cover.txt
copy template\template_ncx.txt ..\bin\app\template\template_ncx.txt
copy template\template_opf.txt ..\bin\app\template\template_opf.txt

rd /Q /S bin
rd /Q /S obj

cd ..
copy dedrm.bat bin\dedrm.bat
copy template_batch\_Tool_Drop_Dump_azwres.txt bin\_Tool_Drop_Dump_azwres.bat
copy template_batch\_Tool_Proc_MyKindleContent.txt bin\_Tool_Proc_MyKindleContent.bat
copy template_batch\_Tool_Drop_Single.txt bin\_Tool_Drop_Single.bat
copy template_batch\_Tool_Drop_Single_dedrm.txt bin\_Tool_Drop_Single_dedrm.bat

pause