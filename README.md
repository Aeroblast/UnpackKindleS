# UnpackKindleS
将 XXX_nodrm.azw3(通过DeDRM工具生成) 和 azw.res(Kindle for PC 1.19 or later) 合并为 epub

## 使用场景
当你需要将azw.res里的高清资源和azw3合并输出到epub中时。这个项目还没有支持其他情况。

## 使用方法

### 使用前
请确保转换的输入文件已经经过了DeDRM（仅azw3）

请确保你的PC装有7z,并且确认packup.bat中的执行命令可用；如果想用其他打包工具可以自己编辑packup.bat

安装dotnet core



### 使用方法

命令行参数为：
<XXX_nodrm.azw3或azw.res或包含以上文件的文件夹名称> [<输出文档>]

程序会在
第一个参数为文件时搜索同目录的其他文件

不指定**输出文档**的情况下，输出为第一个参数的同目录下

**简单的使用方法**：将需要转换的文件拖到run.bat上即可

## 其他说明

这个程序大量参考了[KindleUnpack](https://github.com/kevinhendricks/KindleUnpack) ,
并且将我之前写的 [UnpackKindleHDRes](https://github.com/Aeroblast/UnpackKindleHDRes) 功能加进去了。
初衷是为了转换日亚的轻小说

由于没有那么多测试，可能有各种各样的问题，会尽力修。

目前Log输出基本没做，有问题打断点。

