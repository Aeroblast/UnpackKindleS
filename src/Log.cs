using System;
using System.IO;


namespace UnpackKindleS
{
    public class Log
    {
        static string t = "";
        static string level = "";
        public static void log_tab(string s)
        {
            log(level + s);
        }
        public static void log(string s)
        {
            t += s + "\r\n";
            if (s.StartsWith("[Warn")) { Console.ForegroundColor = ConsoleColor.Yellow; }
            if (s.StartsWith("[Error")) { Console.ForegroundColor = ConsoleColor.Red; }
            if (s.StartsWith("[Info")) { Console.ForegroundColor = ConsoleColor.Green; }
            Console.WriteLine(s);
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void log(Azw3File azw3)
        {
            log("|Azw3 Over View");
            log("|[" + azw3.author + "]" + azw3.title);
            log("|Meta:");
            level = "| |";
            foreach (var a in azw3.mobi_header.extMeta.id_string) log_tab(IdMapping.id_map_strings[a.Key] + " " + a.Value);
            foreach (var a in azw3.mobi_header.extMeta.id_value) log_tab(IdMapping.id_map_values[a.Key] + " " + a.Value);
            foreach (var a in azw3.mobi_header.extMeta.id_hex) log_tab(IdMapping.id_map_hex[a.Key] + " " + a.Value);
            log("|Sections:");
            int i = 0;
            log_tab("Section|   bytes  |type|comment");
            foreach (var a in azw3.sections)
            {
                log_tab(String.Format("    {0}|{1}|{2}|{3}", Util.Number(i, 3), Util.Number(a.GetSize(), 10), a.type, a.comment));
                i++;
            }
            log("|Flows (flow0 is xhtmls)");
            i = 1;
            foreach (var a in azw3.flowProcessLog) { log_tab(Util.Number(i, 3) + ":" + a); i++; }
        }
        public static void Save(string path)
        {
            File.WriteAllText(path, t);
        }
        public static void Append(string path)
        {
            File.AppendAllText(path, "\r\n\r\n" + t);
        }
    }
}
