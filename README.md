# UnpackKindleS
将 XXX_nodrm.azw3(通过DeDRM工具生成) 和 azw.res(Kindle for PC 1.19 or later) 合并为 epub

## 使用场景
当你需要将azw.res里的高清资源和azw3合并输出到epub中时。也可以单独转换azw3。这个项目还没有支持其他情况。

## 使用方法

### 使用Release版

[到这里下载可执行文件](https://github.com/Aeroblast/UnpackKindleS/releases)

#### 简单的使用方法

使用Release版**不要**下载源码中的bat。

方式1：将需要转换的azw3和azw.res放在同一目录，任选其一或者将文件夹拖到_Tool_Drop_Single.bat上。输出在同目录下。

方式2：配置好dedrm.bat，将My Kindle Content拖到_Tool_Drop_MyKindleContent.bat上。批量输出在My Kindle Content 里面。

#### 详细说明
命令行参数格式：
 `` KindleUnpackS <XXX_nodrm.azw3或azw.res或包含以上文件的文件夹名称> [<输出文档>] [<开关>] ``

开关选项：

`` -dedrm `` 调用dedrm.bat处理处理文件夹时遇到的.azw文件。需要在dedrm.bat配置路径。

`` -batch `` 检测文件夹中含有EBOK的文件夹并处理。用于处理My Kindle Content。

可以参考提供的bat

### 使用开发版

安装dotnet core，，请使用源码中的bat，其他同上。


## 其他说明

这个程序大量参考了[KindleUnpack](https://github.com/kevinhendricks/KindleUnpack) ,
并且将我之前写的 [UnpackKindleHDRes](https://github.com/Aeroblast/UnpackKindleHDRes) 功能加进去了。
初衷是为了转换日亚的轻小说

由于没有那么多测试，可能有各种各样的问题，会尽力修。

转换完可以看一眼命令行输出或者lastrun.log是不是所有flow和图片Section都被转换了。

