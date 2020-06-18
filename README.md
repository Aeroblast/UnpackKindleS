# UnpackKindleS
将 XXX_nodrm.azw3(通过DeDRM工具生成) 和 azw.res(Kindle for PC 1.19 or later) 合并为 epub。推荐使用Kindle for PC 1.19~1.28之间的版本。

## 使用场景
当你需要将azw.res里的高清资源和azw3合并输出到epub中时。也可以单独转换azw3或解体azw.res。这个项目没有支持其他情况。经过配置dedrm.bat可以调用DeDRM程序，达到一键批量转换的效果。

## 使用方法

### 使用Release版

Release版可执行文件为Windows 64bit，不需要安装任何依赖。[【下载可执行文件】](https://github.com/Aeroblast/UnpackKindleS/releases)

如果配置环境遇到困难，可以参阅完善中的[【FAQ】](https://github.com/Aeroblast/UnpackKindleS/blob/master/FAQ.md)。仍无法解决请提issue。

#### 简单的使用方法

下载Release版，将包含命令行程序UnpackKindleS.exe及相关的Windows批处理脚本。

Release版中，另外包含一个 [精简版的DeDRM](https://github.com/Aeroblast/AZW3_PC_DeDRM)，因此不需要额外的工具。

最简流程：1.在Kindle for PC中下载所有需要导出的书籍；2.运行`_Tool_Proc_MyKindleContent.bat`，将会在同目录下输出EPUB。

转换完成后，会在程序目录输出lastrun.log，可以用于检查输出结果。

最简流程中的`_Tool_Proc_MyKindleContent.bat`针对默认安装，默认存放书籍位置为`C:\Users\用户名\Documents\My Kindle Content`。如果存放在其他位置，使用文本编辑器编辑`_Tool_Proc_MyKindleContent.bat`，将`%USERPROFILE%\Documents\My Kindle Content`修改为相应的路径即可。

其他使用方式：

+ 将azw.res和已经去除DRM的azw3放在同一目录，任选其一或者将文件夹拖到```_Tool_Drop_Single.bat```上。在同目录下输出EPUB。

+ 将My Kindle Content中类似```B0XXXXXXXX_EBOK```的文件夹拖到```_Tool_Drop_Single_dedrm.bat```上。在同目录下输出EPUB。

+ 提取azw.res中的高清插图：将相应的文件拖到```_Tool_Drop_Dump_azwres.bat```上。图片输出到一个名字为该资源书籍标题的文件夹中。

#### 详细说明
命令行参数格式：
 `` KindleUnpackS <XXX_nodrm.azw3或azw.res或包含以上文件的文件夹名称> [<输出文档>] [<开关>] ``

开关选项：

`` -dedrm `` 调用dedrm.bat处理处理文件夹时遇到的.azw文件。需要在dedrm.bat配置路径。

`` -batch `` 检测文件夹中含有EBOK的文件夹并处理。用于处理My Kindle Content。

`` --just-dump-res `` 提取高清插图。

可以参考提供的bat。

指定输出目录的功能大概能用。

### 使用开发版

安装dotnet core，请使用源码中的bat，其他同上。


## 其他说明

这个程序大量参考了[KindleUnpack](https://github.com/kevinhendricks/KindleUnpack) ,
并且将我之前写的 [UnpackKindleHDRes](https://github.com/Aeroblast/UnpackKindleHDRes) 功能加进去了。
初衷是为了转换日亚的轻小说

由于没有那么多测试，可能有各种各样的问题，会尽力修，欢迎提供样本。

转换完可以看一眼命令行输出或者lastrun.log是不是所有flow和图片Section都被转换了。

