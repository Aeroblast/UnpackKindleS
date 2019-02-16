using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace UnpackKindleS
{

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: <xxx_nodrm.azw3 or xxx.azw.res or the directory> [<output_path>] [switches ...]");
                return;
            }
            string azw3_path = null;
            string azw6_path = null;
            string dir;
            string p = args[0];
            if (Path.GetExtension(p).ToLower() == ".azw3")
            {
                azw3_path = p;
                dir = Path.GetDirectoryName(p);
                string[] files = Directory.GetFiles(dir);
                foreach (string n in files)
                {
                    if (Path.GetExtension(n) == ".res")
                    {
                        azw6_path = n;
                        break;
                    }
                }
            }
            if (Path.GetExtension(p).ToLower() == ".res")
            {
                azw6_path = p;
                dir = Path.GetDirectoryName(p);
                string[] files = Directory.GetFiles(dir);
                foreach (string n in files)
                {
                    if (Path.GetExtension(n).ToLower() == ".azw3")
                    {
                        azw3_path = n;
                        break;
                    }
                }
            }
            if (Directory.Exists(p))
            {
                string[] files = Directory.GetFiles(p);
                foreach (string n in files)
                {
                    if (Path.GetExtension(n).ToLower() == ".azw3")
                    {
                        azw3_path = n;
                    }
                    if (Path.GetExtension(n) == ".res")
                    {
                        azw6_path = n;
                    }
                }
            }
            Azw3File azw3 = null;
            Azw6File azw6 = null;
            if (azw3_path != null)
                azw3 = new Azw3File(azw3_path);
            if (azw6_path != null)
                azw6 = new Azw6File(azw6_path);
            if (azw3 != null)
            {
                string outname = azw3.title + ".epub";
                Epub epub = new Epub(azw3, azw6);
                Directory.CreateDirectory("temp");
                epub.Save("temp");
                if (args.Length >= 2)
                    if (Directory.Exists(args[1]))
                    {
                        Util.Packup(Path.Combine(args[1], outname));
                        return;
                    }
                {
                    string outdir = Path.GetDirectoryName(args[0]);
                    Util.Packup(Path.Combine(outdir, outname));
                }

            }

        }
        static void test()
        {
            Azw6File azw6 = new Azw6File(@"sample.azw.res");
            Azw3File azw3 = new Azw3File(@"sample_nodrm.azw3");
            Epub mainfile = new Epub(azw3, azw6);
            mainfile.Save("temp");
        }



    }
}
