# 什么是azw.res文件？
根据Kindle for PC (Windows)测试，日亚的轻小说在特定版本下载为两部分（似乎是Kindle for PC 1.19~1.24），位于一个以```_EBOK```结尾的文件夹中，前面是该书的ASIN。一个部分后缀为azw，有DRM，经过DeDRM后后缀将是azw3；另一个部分后缀为azw.res，是一个未加密的文件。这两个部分加起来的大小是商品页面的“文件大小（ファイルサイズ）”所标注的大小。

azw3将包含书籍的主要信息，包括正文和图片，主流的工具都可以处理这个类型。azw.res提供了额外的高清图片，分辨率一般为高度1600px或者2048px（大约某个时间点以后的部分文库，待确认，可以参考下面的统计情况）。

附一个Kindle for PC 1.23 Windows [【MEGA盘】](https://mega.nz/#!t1ACHQgR!ZpiiF6G7fSwgYkXsi7_UGm2zYBpmkBDCaRqtLJnt3_E)

高清资源规格部分统计情况(长/短：长边/短边固定为该像素值；高：高度固定为该像素值)：

文库 | 发行 | 像素数（观测样本起止时间）
-|-|-
電撃文庫 | KADOKAWA | 长1600(2016.6)/长2048(2016.12-最新)
メディアワークス文庫 | KADOKAWA | 长1600(-2015.9)/长2048(2016.10-最新) 
富士见L文庫 | KADOKAWA | 1600(2016.12)
富士見ファンタジア文庫 | KADOKAWA | 高1600(-2018.3)
ＭＦ文庫Ｊ | KADOKAWA | 高1600(2012.2-2018.2)/长2048(最新)
角川文庫 | KADOKAWA | 1600(2016.5)/长2048(最新)
講談社ラノベ文庫 | 講談社 | 长2048(2017.7-最新)
ガガガ文庫 | 小学館 | 长1600(最新)
GA文庫 | SBクリエイティブ | 长2048
美少女文庫 | フランス書院 | 短1563/长2233/不确定宽高
ぷちぱら文庫 | パラダイム | 长1600(-2015)/长1920(2015.7-最新)
# 如何配置dedrm.bat？
[DeDRM工具](https://github.com/apprenticeharper/DeDRM_tools) 是一个可以去除DRM的工具，有Calibre插件和单独的脚本。

插件版可以方便地转换azw3，如果需要与高清资源合并转换，将二者放入同一文件夹中即可。

以下教程根据Windows编写，Mac用户请自求多福（理论上可以搞……）

**强烈推荐：** 对于批量一键转换，由于Calibre没有一个插件能处理多个文件输入，因此使用独立的脚本，即DeDRM_tools的Release中的DeDRM_Windows_Application。

根据DeDRM文档和实际试验，需要安装：
+ ActivePython 2.7，运行环境。[【官方下载（居然还得注册）】](http://www.activestate.com/activepython/downloads) [【百度网盘(随便找的)】](https://pan.baidu.com/s/1jGBo9QA)
+ Visual C++ Compiler for Python 2.7 ，安装PyCryoto可能需要，[【官方下载】](https://www.microsoft.com/en-us/download/details.aspx?id=44266)
+ PyCrypto，需要的模块，[【官方下载】](http://www.voidspace.org.uk/python/modules.shtml#pycrypto)
+ 通过pip安装pylzma ，例：```pip2 install pylzma```

确保安装的如上模块，可以正常使用DeDRM_App，则可以按照自己的安装路径修改dedrm.bat。

例子：
```"C:\Python2\python2.exe" "D:\Apps\DeDRM_App\DeDRM_lib\DeDRM_App.pyw" %1```

```%1```将用于传递参数。
