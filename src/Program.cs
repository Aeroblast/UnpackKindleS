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
        static bool end_of_proc = false;
        static string temp_path="_temp_";
        static void Main(string[] args)
        {
            Console.WriteLine("UnpackKindleS Ver." + Version.version);
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: <xxx_nodrm.azw3 or xxx.azw.res or the directory> [<output_path>] [switches ...]");
                return;
            }
            if(!Directory.Exists(args[0])&&!File.Exists(args[0]))
            {
                Console.WriteLine("Invaild input.\nUsage: <xxx_nodrm.azw3 or xxx.azw.res or the directory> [<output_path>] [switches ...]");
                return;
            }

            foreach (string a in args)
            {
                if (a.ToLower() == "-dedrm") dedrm = true;
                if (a.ToLower() == "--just-dump-res")
                {
                    DumpHDImage(args);
                    end_of_proc = true;
                }
            }
            if (!end_of_proc)
                foreach (string a in args)
                {
                    switch (a.ToLower())
                    {
                        case "-batch": ProcBatch(args); break;
                    }
                }
            if (!end_of_proc) ProcPath(args);
            Log.Save("..\\lastrun.log");
        }
        static void ProcBatch(string[] args)
        {
            string[] dirs = Directory.GetDirectories(args[0]);
            foreach (string s in dirs)
            {
                if (!s.Contains("EBOK")) continue;
                string[] args2 = new string[2];
                args2[0] = s;
                if (args.Length >= 2 && Directory.Exists(args[1])) args2[1] = args[1];
                else args2[1] = Environment.CurrentDirectory;
                try { ProcPath(args2); } catch (Exception e) { Log.log(e.ToString()); }

            }
            end_of_proc = true;
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
            {
                Log.log("==============START===============");
                azw3 = new Azw3File(azw3_path);
            }
            if (azw6_path != null)
                azw6 = new Azw6File(azw6_path);
            if (azw3 != null)
            {
                string outname = "[" + azw3.mobi_header.extMeta.id_string[100].Split('&')[0] + "] " + azw3.title + ".epub";
                outname=Util.FilenameCheck(outname);
                Epub epub = new Epub(azw3, azw6);
                if (Directory.Exists(temp_path)) DeleteDir(temp_path);
                Directory.CreateDirectory(temp_path);
                epub.Save(temp_path);
                Log.log(azw3);
                string output_path;
                if (args.Length >= 2)
                    if (Directory.Exists(args[1]))
                    {
                        output_path = Path.Combine(args[1], outname);
                    }
                {
                    string outdir = Path.GetDirectoryName(args[0]);
                    output_path = Path.Combine(outdir, outname);
                }
                Util.Packup(temp_path,output_path);
                DeleteDir(temp_path);
                Log.log("azw3 source:" + azw3_path);
                if (azw6_path != null)
                    Log.log("azw6 source:" + azw6_path);
            }
            else
            {
                Console.WriteLine("Cannot find .azw3 file in " + p);
            }
        }

        static void DumpHDImage(string[] args)
        {
            Log.log("Dump azw.res");
            Log.log("azw6 source:" + args[0]);
            string outputdir = "";
            if (!File.Exists(args[0])) { Log.log("File was not found:" + args[0]); return; }
            Azw6File azw = new Azw6File(args[0]);
            if (args.Length >= 3) outputdir = args[1];
            else { outputdir = Path.Combine(Path.GetDirectoryName(args[0]), azw.header.title); }
            if (!CreateDirectory(outputdir)) { return; }
            foreach (var a in azw.image_sections)
            {
                CRES_Section sec = (CRES_Section)azw.sections[a];
                string filename = Epub.ImageNameHD(a - 1, sec);
                File.WriteAllBytes(Path.Combine(outputdir, filename), sec.img);
                Log.log("Saved:" + Path.Combine(outputdir, filename));
            }
        }


        static void DeDRM(string file)
        {
            string fn = "";
            if (File.Exists("dedrm.bat")) { fn = "dedrm.bat"; }
            else if (File.Exists("..\\dedrm.bat")) { fn = "..\\dedrm.bat"; }
            else { Log.log("Cannot found dedrm.bat"); return; }
            Process p = new Process();
            p.StartInfo.FileName = fn;
            p.StartInfo.Arguments = "\"" + file + "\"";
            p.Start();
            p.WaitForExit();
        }
        static void DeleteDir(string path)
        {
            foreach (string p in Directory.GetFiles(path)) File.Delete(p);
            foreach (string p in Directory.GetDirectories(path)) DeleteDir(p);
            Directory.Delete(path);
        }
        static bool CreateDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    string parent = Path.GetDirectoryName(path);
                    if (Directory.Exists(parent)||parent=="") Directory.CreateDirectory(path);
                    else { CreateDirectory(parent); Directory.CreateDirectory(path); }
                }
            }
            catch (Exception e)
            {
                Log.log(e.ToString());
                return false;
            }

            return true;
        }
    }
}
