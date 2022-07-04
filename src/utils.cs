using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Runtime.InteropServices;
using System.Xml;
using System.Drawing;
using System.Drawing.Imaging;
namespace UnpackKindleS
{

    class Util
    {

        public static byte[] SubArray(byte[] src, ulong start, ulong length)
        {
            byte[] r = new byte[length];
            for (ulong i = 0; i < length; i++) { r[i] = src[start + i]; }
            return r;
        }
        public static byte[] SubArray(byte[] src, int start, int length)
        {
            byte[] r = new byte[length];
            for (int i = 0; i < length; i++) { r[i] = src[start + i]; }
            return r;
        }
        public static string ToHexString(byte[] src, uint start, uint length)
        {
            //https://stackoverflow.com/a/14333437/48700
            char[] c = new char[length * 2];
            int b;
            for (int i = 0; i < length; i++)
            {
                b = src[start + i] >> 4;
                c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
                b = src[start + i] & 0xF;
                c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
            }
            return new string(c);
        }
        public static UInt64 GetUInt64(byte[] src, ulong start)
        {
            byte[] t = SubArray(src, start, 8);
            Array.Reverse(t);
            return BitConverter.ToUInt64(t);
        }
        //big edian handle:
        public static UInt32 GetUInt32(byte[] src, ulong start)
        {
            byte[] t = SubArray(src, start, 4);
            Array.Reverse(t);
            return BitConverter.ToUInt32(t);
        }
        public static UInt16 GetUInt16(byte[] src, ulong start)
        {
            byte[] t = SubArray(src, start, 2);
            Array.Reverse(t);
            return BitConverter.ToUInt16(t);
        }
        public static byte GetUInt8(byte[] src, ulong start)
        {
            return src[start];
        }

        public static string GuessImageType(byte[] data)
        {
            if (data.Length < 4) return null;
            if (data[0] == 0xff && data[1] == 0xd8
            // && data[data.Length - 2] == 0xff && data[data.Length - 1] == 0xd9//有的会在后面跟几个0字节
            )
                return ".jpg";
            if (Encoding.ASCII.GetString(data, 0, 4) == "GIF8")
                return ".gif";
            if (Encoding.ASCII.GetString(data, 0, 4) == "\x89\x50\x4e\x47")
                return ".png";

            return null;
        }

        public static string GetOuterXML(string data, string tagname)
        {
            int start = data.IndexOf("<" + tagname); if (start < 0) return null;
            int end = data.IndexOf("</" + tagname + ">") + 3 + tagname.Length;
            return data.Substring(start, end - start);
        }
        public static string GetInnerXML(XmlElement e)
        {
            string r = "";
            if (e.ChildNodes == null) return e.InnerXml;
            foreach (XmlNode n in e.ChildNodes)
            {

                if (n.NodeType == XmlNodeType.Element)
                {
                    r += "    " + GetOuterXML((XmlElement)n).Replace("\n", "    \n") + "\n";
                    continue;
                }
                if (n.NodeType == XmlNodeType.Text)
                {
                    r += n.OuterXml;
                }
            }
            return r;
        }
        public static string GetOuterXML(XmlElement e)
        {
            string inner = GetInnerXML(e);
            string attr = "";
            if (e.Attributes != null)
                foreach (XmlAttribute a in e.Attributes)
                {
                    attr += string.Format(" {0}=\"{1}\"", a.Name, a.Value);
                }
            if (inner == "")
            {
                return string.Format("<{0}{1} />", e.Name, attr);
            }
            return String.Format("<{0}{2}>{1}</{0}>", e.Name, inner, attr);
        }


        public static T GetStructBE<T>(byte[] data, int offset)
        {
            int size = Marshal.SizeOf(typeof(T));
            Byte[] data_trimed = SubArray(data, offset, size);
            Array.Reverse(data_trimed);
            IntPtr structPtr = Marshal.AllocHGlobal(size);
            Marshal.Copy(data_trimed, 0, structPtr, size);
            T r = (T)Marshal.PtrToStructure(structPtr, typeof(T));
            Marshal.FreeHGlobal(structPtr);
            return r;
        }

        public static UInt64 DecodeBase32(string s)
        {
            UInt64 r = 0;
            foreach (char c in s)
            {
                uint v;
                if (char.IsDigit(c))
                {
                    v = (uint)c - (uint)'0';
                }
                else
                {
                    v = (uint)c - (uint)'A' + 10;
                }
                r = (r * 32) + v;

            }
            return r;
        }
        public static string Number(int number, int length = 4)
        {
            string r = number.ToString();
            for (int j = length - r.Length; j > 0; j--) r = "0" + r;
            return r;
        }

        public static string FilenameCheck(string s)
        {
            return s
            .Replace('?', '？')
            .Replace('\\', '＼')
            .Replace('/', '／')
            .Replace(':', '：')
            .Replace('*', '＊')
            .Replace('"', '＂')
            .Replace('|', '｜')
            .Replace('<', '＜')
            .Replace('>', '＞')
            ;
        }

        public static string EscapeInvalidXmlCharacters(string in_string)
        {
            // from https://stackoverflow.com/a/641632
            if (in_string == null) return null;

            StringBuilder str_buf = new StringBuilder();
            char ch;

            for (int i = 0; i < in_string.Length; i++)
            {
                ch = in_string[i];
                if ((ch >= 0x0020 && ch <= 0xD7FF) ||
                    (ch >= 0xE000 && ch <= 0xFFFD) ||
                    ch == 0x0009 ||
                    ch == 0x000A ||
                    ch == 0x000D)
                {
                    str_buf.Append(ch);
                }
                else
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(Char.ToString(ch));

                    str_buf.Append("&#x" + Convert.ToHexString(bytes) + ";");
                }
            }
            return str_buf.ToString();
        }

        public static (int, int) GetImageSize(byte[] data)
        {
            using (var img = Image.FromStream(new MemoryStream(data)))
            {
                return (img.Width, img.Height);
            }
        }
    }

    [System.Serializable]
    public class UnpackKindleSException : System.Exception
    {
        public UnpackKindleSException(string message) : base(message) { }
        protected UnpackKindleSException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}