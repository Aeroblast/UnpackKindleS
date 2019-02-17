using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
namespace UnpackKindleS
{

    class Program
    {
        static bool dedrm = false;
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: <xxx_nodrm.azw3 or xxx.azw.res or the directory> [<output_path>] [switches ...]");
                return;
            }
            foreach (string a in args)
            {
                if (a.ToLower() == "-dedrm") dedrm = true;
            }
            foreach (string a in args)
            {
                switch (a.ToLower())
                {
                    case "-batch": ProcBatch(args); break;
                }
            }
            ProcPath(args);

        }
        static void ProcBatch(string[] args)
        {
            string[] dirs = Directory.GetDirectories(args[0]);
            foreach (string s in dirs)
            {
                if (!s.Contains("EBOK")) return;
                string[] args2 = new string[2];
                args2[0] = s;
                if (args.Length >= 2 && Directory.Exists(args[1])) args2[1] = args[1];
                else args2[1]=Environment.CurrentDirectory;

                ProcPath(args2);
            }
        }

        static void ProcPath(string[] args)
        {
            string azw3_path = null;
            string azw6_path = null;
            string dir;
            string p = args[0];
            if (Directory.Exists(p))
            {
                string[] files = Directory.GetFiles(p);
                if (dedrm)
                {
                    foreach (string n in files)
                    {
                        if (Path.GetExtension(n).ToLower() == ".azw")
                        {
                            DeDRM(n);
                        }
                    }
                    files = Directory.GetFiles(p);
                }

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
            else if (Path.GetExtension(p).ToLower() == ".azw3")
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
            else if (Path.GetExtension(p).ToLower() == ".res")
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

            Azw3File azw3 = null;
            Azw6File azw6 = null;
            if (azw3_path != null)
                azw3 = new Azw3File(azw3_path);
            if (azw6_path != null)
                azw6 = new Azw6File(azw6_path);
            if (azw3 != null)
            {
                string outname = "[" + azw3.mobi_header.extMeta.id_string[100] + "] " + azw3.title + ".epub";
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
                    if (outdir == "") { outdir = Environment.CurrentDirectory; }
                    Util.Packup(Path.Combine(outdir, outname));
                }

            }
            else
            {
                Console.WriteLine("Cannot find .azw3 file");
            }
        }
        static void test()
        {
            Azw6File azw6 = new Azw6File(@"sample.azw.res");
            Azw3File azw3 = new Azw3File(@"sample_nodrm.azw3");
            Epub mainfile = new Epub(azw3, azw6);
            mainfile.Save("temp");
        }


        static void DeDRM(string file)
        {
            Process p = new Process();
            p.StartInfo.FileName = "dedrm.bat";
            p.StartInfo.Arguments = "\"" + file + "\"";
            p.Start();
            p.WaitForExit();
        }
    }
}
