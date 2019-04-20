cd /d %~dp0
rd /Q /S bin
dotnet publish -c release -r win-x64 --self-contained
rename bin\release\netcoreapp2.2\win-x64\publish lib
move bin\release\netcoreapp2.2\win-x64\lib bin\lib
copy template_cover.txt bin\lib\template_cover.txt
copy template_ncx.txt bin\lib\template_ncx.txt
copy template_opf.txt bin\lib\template_opf.txt
copy template_opf.txt bin\lib\template_opf.txt
copy dedrm.bat bin\dedrm.bat
copy template_batch\_Tool_Drop_Dump_azwres.txt bin\_Tool_Drop_Dump_azwres.bat
copy template_batch\_Tool_Drop_MyKindleContent.txt bin\_Tool_Drop_MyKindleContent.bat
copy template_batch\_Tool_Drop_MyKindleContent-nodrm.txt bin\_Tool_Drop_MyKindleContent-nodrm.bat
copy template_batch\_Tool_Drop_Single.txt bin\_Tool_Drop_Single.bat
rd /Q /S bin\release 
pause