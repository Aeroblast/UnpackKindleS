cd /d %~dp0
dotnet publish -c release -r win-x64 --self-contained
rename bin\release\netcoreapp2.2\win-x64\publish app
move bin\release\netcoreapp2.2\win-x64\app bin\app
copy template_cover.txt bin\app\template_cover.txt
copy template_ncx.txt bin\app\template_ncx.txt
copy template_opf.txt bin\app\template_opf.txt
pause