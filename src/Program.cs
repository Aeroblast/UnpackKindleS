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
        static bool append_log = false;
        static bool overwrite = false;
        static bool rename_when_exist = false;
        static bool rename_xhtml_with_id = false;
        static void Main(string[] args)
        {
            string temp_environment_dir = Environment.CurrentDirectory;
            if (!Directory.Exists("template"))
                Environment.CurrentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            Console.WriteLine("UnpackKindleS Ver." + Version.version);
            Console.WriteLine("https://github.com/Aeroblast/UnpackKindleS");
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: <xxx_nodrm.azw3 or xxx.azw.res or the directory> [<output_path>] [switches ...]");
                return;
            }
            if (!Directory.Exists(args[0]) && !File.Exists(args[0]))
            {
                Console.WriteLine("The file or folder does not exist:" + args[0]);
                Console.WriteLine("Usage: <xxx_nodrm.azw3 or xxx.azw.res or the directory> [<output_path>] [switches ...]");
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
                if (a.ToLower() == "--append-log")
                {
                    append_log = true;
                }
                if (a.ToLower() == "--overwrite")
                {
                    overwrite = true;
                }
                if (a.ToLower() == "--rename-when-exist")
                {
                    rename_when_exist = true;
                }
                if (a.ToLower() == "--rename-xhtml-with-id")
                {
                    rename_xhtml_with_id = true;
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
            if (append_log)
            {
                Log.Append("..\\lastrun.log");
            }
            else
                Log.Save("..\\lastrun.log");

            Environment.CurrentDirectory = temp_environment_dir;
        }
        static void ProcBatch(string[] args)
        {
            Log.log("Batch Process:" + args[0]);
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
            else
            {
                if (dedrm && Path.GetExtension(p).ToLower() == ".azw")
                {
                    DeDRM(p);
                    dir = Path.GetDirectoryName(p);
                    string[] files = Directory.GetFiles(dir);
                    foreach (string n in files)
                    {
                        if (Path.GetExtension(n) == ".res")
                        {
                            azw6_path = n;
                        }
                        if (Path.GetExtension(n).ToLower() == ".azw3")
                        {
                            azw3_path = n;
                        }
                    }
                }
                else
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
                string auther = "";
                if (azw3.mobi_header.extMeta.id_string.ContainsKey(100))
                {
                    auther = "[" + azw3.mobi_header.extMeta.id_string[100].Split('&')[0] + "] ";
                }
                string outname = auther + azw3.title + ".epub";
                outname = Util.FilenameCheck(outname);
                Epub epub = new Epub(azw3, azw6, rename_xhtml_with_id);
                Log.log(azw3);
                string output_path;
                if (args.Length >= 2 && Directory.Exists(args[1]))
                {
                    output_path = Path.Combine(args[1], outname);
                }
                else
                {
                    string outdir = Path.GetDirectoryName(args[0]);
                    output_path = Path.Combine(outdir, outname);
                }
                if (File.Exists(output_path))
                {
                    Log.log("[Warn]Output already exist.");
                    if (rename_when_exist)
                    {
                        string r_dir = Path.GetDirectoryName(output_path);
                        string r_name = Path.GetFileNameWithoutExtension(output_path);
                        string r_path = Path.Combine(r_dir, r_name);
                        output_path = "";
                        for (int i = 2; i < 50; i++)
                        {
                            string r_test = r_path + "(" + i + ").epub";
                            if (!File.Exists(r_test))
                            {
                                output_path = r_test;
                                break;
                            }
                        }
                        Log.log("[Warn]Save as...");
                    }
                    else if (!overwrite)
                    {
                        Console.WriteLine("Output file already exist. N(Abort,Defualt)/y(Overwrite)/r(Rename)?");
                        Console.WriteLine("Output path:" + output_path);
                        string input = Console.ReadLine().ToLower();
                        if (input == "y")
                        {
                            Log.log("[Warn]Old file will be replaced.");
                        }
                        else if (input == "r")
                        {
                            string r_dir = Path.GetDirectoryName(output_path);
                            string r_name = Path.GetFileNameWithoutExtension(output_path);
                            string r_path = Path.Combine(r_dir, r_name);
                            output_path = "";
                            for (int i = 2; i < 50; i++)
                            {
                                string r_test = r_path + "(" + i + ").epub";
                                if (!File.Exists(r_test))
                                {
                                    output_path = r_test;
                                    break;
                                }
                            }
                            Log.log("[Warn]Save as...");
                        }
                        else
                        {
                            Log.log("[Error]Operation aborted. You can use --overwrite or --rename-when-exist to avoid pause.");
                            output_path = "";
                        }

                    }
                    else
                    {
                        Log.log("[Warn]Old file will be replaced.");
                    }
                }
                if (output_path != "")
                    epub.Save(output_path);
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
            else { outputdir = Path.Combine(Path.GetDirectoryName(args[0]), Util.FilenameCheck(azw.header.title)); }
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
        static bool CreateDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    string parent = Path.GetDirectoryName(path);
                    if (Directory.Exists(parent) || parent == "") Directory.CreateDirectory(path);
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
