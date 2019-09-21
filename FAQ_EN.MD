# What are azw.res files?

Tested with Kindle for PC (Windows), Lightnovels from Amazon Japan are downloaded as two parts, saved in a folder that ends with ```_EBOK```. 

One part has a .azw extension and you can use DRM Tools to decrypt it and get an azw3 file. This one contians most contents of the book and there are many tools to deal with it.

Another part has an azw.res extension and it is not encrypted. The azw.res file contains HD version of the images in the book. 

The total file size of the two parts will be the File Size in the Product details.

Please note that such file could only be found with Kindle for PC 1.19~1.24. Eariler versions have no azw.res files. Later versions cannot process by DeDRM Tools.

# How to use dedrm.bat?

You should have non-DRM azw3 files to use UnpackKinldeS. To get non-DRM azw3 files, you can use [DeDRM Tools](https://github.com/apprenticeharper/DeDRM_tools). 

You can use calibre plugin to get a non-DRM file, then put it in the same folder with azw.res. The .bat is not used in this way.

If you want to merge lots of books at a time, use the standalone version of DeDRM Tools. You may follow the instructions of DeDRM Tools to make sure it works, then write the path to dedrm.bat.

Example:
```"C:\Python2\python2.exe" "D:\Apps\DeDRM_App\DeDRM_lib\DeDRM_App.pyw" %1```

```%1```is used to pass the azw file.



