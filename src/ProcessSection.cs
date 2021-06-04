using System;
using System.Xml;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace UnpackKindleS
{
    public class Section
    {
        public string type;
        public byte[] raw;
        virtual public int GetSize() { return raw.Length; }

        public string comment = "";

        public Section(byte[] raw)
        {
            this.raw = raw;
            if (raw.Length < 4) { type = "Empty Section"; return; }
            type = Encoding.ASCII.GetString(raw, 0, 4);
            switch (type)
            {
                case "??\r\n": type = "End Of File"; break;
                case "?6?\t": type = "Place Holder"; break;
                case "\0\0\0\0": type = "Empty Section0"; break;
            }
        }
        public Section(Section s) { type = s.type; raw = s.raw; }
        public Section(string type, byte[] raw) { this.type = type; this.raw = raw; }
    }
    public class Text_Section : Section { public Text_Section(byte[] r) : base("Text", null) { size = r.Length; } int size; override public int GetSize() { return size; } }
    public class Image_Section : Section
    {
        public string ext;
        public Image_Section(Section s, string ext) : base("Image", s.raw)
        {
            this.ext = ext;
        }
    }
    public class FDST_Section : Section
    {
        public UInt32[] table;
        public FDST_Section(Section section) : base(section)
        {
            if (type != "FDST") throw new UnpackKindleSException("Error on SectionFDST Process");
            var n = Util.GetUInt32(raw, 8);
            table = new UInt32[n];
            for (uint i = 0; i < n; i++) table[i] = Util.GetUInt32(raw, 12 + 8 * i);
        }
    }
    public class RESC_Section : Section
    {
        public XmlDocument metadata;
        public XmlDocument spine;
        public RESC_Section(Section section) : base(section)
        {
            if (type != "RESC") throw new UnpackKindleSException("Error on SectionRESC Process");
            int zero = raw.Length - 1; while (raw[zero] == 0) zero--;
            string data = Encoding.UTF8.GetString(raw, 16, zero - 15);
            data = data.Substring(data.IndexOf('<'));
            string meta = Util.GetOuterXML(data, "metadata");
            if (meta != null)
            {
                metadata = new XmlDocument();
                metadata.LoadXml(meta);
            }
            string spi = Util.GetOuterXML(data, "spine");
            if (spi != null)
            {
                spine = new XmlDocument();
                spine.LoadXml(spi);
            }
            else
            {
                throw new UnpackKindleSException("RESC Section has none spine");
            }
        }
    }
    public class INDX_Section : Section
    {
        public INDX_Section_Header header;
        public INDX_Section(byte[] data) : base(data)
        {
            if (type != "INDX") throw new UnpackKindleSException("INDX Section Header Error");
            header = Util.GetStructBE<INDX_Section_Header>(data, 4);
        }
    }

    public class INDX_Section_Main : INDX_Section
    {
        public uint tag_table_start, tag_table_end, ctrl_byte_count;
        public uint tag_table_length { get { return (tag_table_end - tag_table_start) / 4; } }
        public byte tag(int i) { return raw[tag_table_start + i * 4]; }
        public byte tag_value(int i) { return raw[tag_table_start + i * 4 + 1]; }
        public byte mask(int i) { return raw[tag_table_start + i * 4 + 2]; }
        public byte endflag(int i) { return raw[tag_table_start + i * 4 + 3]; }
        public INDX_Section_Main(byte[] data, string type) : base(data) { this.type = type; ReadTag(); }
        public bool isSample { get { return or_dict != null; } }
        public UInt16[] or_dict;
        public void ReadTag()//for main section
        {
            uint off = header.tag_part_start;
            string TAGX = Encoding.ASCII.GetString(raw, (int)off, 4);
            if (TAGX != "TAGX") return;
            tag_table_start = off + 12;
            tag_table_end = off + Util.GetUInt32(raw, off + 4);
            ctrl_byte_count = Util.GetUInt32(raw, off + 8);

            //Sample Book, mobi_index.py:99
            uint o_count = Util.GetUInt32(raw, 0xa4);
            uint o_entries = Util.GetUInt32(raw, 0xa8);
            uint op1 = Util.GetUInt32(raw, 0xac);
            uint op2 = Util.GetUInt32(raw, 0xb0);
            if (header.codepage == 65002 || o_count != 0 || o_entries > 0)
            {
                if (o_count != 1 ||
                Encoding.ASCII.GetString(raw, (int)op1, 4) != "ORDT" ||
                Encoding.ASCII.GetString(raw, (int)op2, 4) != "ORDT")
                { throw new UnpackKindleSException("Sample Book Assumption Failure."); }
                Log.log("Book Sample Assumption.");
                or_dict = new UInt16[o_entries];
                for (uint i = 0; i < o_entries; i++)
                    or_dict[i] = Util.GetUInt16(raw, op2 + 4 + i * 2);
            }
        }

    }
    public class INDX_Section_Extra : INDX_Section
    {
        public UInt32[] index_pos;
        INDX_Section_Main main_sec;
        public INDX_Section_Tag[] tags;
        public Hashtable[] tagmaps;
        public string[] texts;
        public INDX_Section_Extra(byte[] data, INDX_Section_Main m) : base(data) { type = "INDX Section (Extra)"; main_sec = m; }
        public void ReadTagMap()//for the following extra section
        {
            index_pos = new UInt32[header.any_count + 1];
            for (uint i = 0; i < index_pos.Length - 1; i++)
            {
                index_pos[i] = Util.GetUInt16(raw, header.index_offset + i * 2 + 4);
            }
            index_pos[header.any_count] = header.index_offset;
            tagmaps = new Hashtable[header.any_count];
            texts = new string[header.any_count];
            for (uint i = 0; i < header.any_count; i++)
            {
                uint length = raw[index_pos[i]];
                byte[] text = Util.SubArray(raw, index_pos[i] + 1, length);
                if (main_sec.isSample)
                {
                    for (int j = 0; j < text.Length; j++) text[j] = (byte)main_sec.or_dict[text[j]];
                }
                texts[i] = Encoding.UTF8.GetString(text);
                tagmaps[i] = GetTagMap(index_pos[i] + 1 + length, index_pos[i + 1]);

            }

        }
        public Hashtable GetTagMap(uint start_pos, uint end_pos)
        {
            Hashtable hashtable;
            int ctrl_byte_index = 0;
            uint data_start = start_pos + main_sec.ctrl_byte_count;
            tags = new INDX_Section_Tag[main_sec.tag_table_length];
            for (int i = 0; i < main_sec.tag_table_length; i++)
            {
                if (main_sec.endflag(i) == 0x01)
                {
                    ctrl_byte_index++;
                    continue;
                }
                byte c = raw[start_pos + ctrl_byte_index];
                byte v = (byte)(c & main_sec.mask(i));
                if (v != 0)
                {
                    if (v == main_sec.mask(i))
                    {
                        if (CountBit(v) > 1)
                        {
                            GetVariableWidthValue(data_start);
                            data_start += consumed;
                            tags[i] = new INDX_Section_Tag(main_sec.tag(i), 0, value, main_sec.tag_value(i));
                        }
                        else
                            tags[i] = new INDX_Section_Tag(main_sec.tag(i), 1, 0, main_sec.tag_value(i));
                    }
                    else
                    {
                        int mask = main_sec.mask(i);
                        while ((mask & 1) == 0)
                        {
                            mask = mask >> 1;
                            v = (byte)(v >> 1);
                        }
                        tags[i] = new INDX_Section_Tag(main_sec.tag(i), v, 0, main_sec.tag_value(i));
                    }
                }
            }
            hashtable = new Hashtable();
            List<int> values = new List<int>();
            foreach (INDX_Section_Tag tag in tags)
            {
                if (tag == null) continue;
                if (tag.count != 0)
                    for (int j = 0; j < tag.count; j++)
                        for (int i = 0; i < tag.tag_value; i++)
                        {
                            GetVariableWidthValue(data_start);
                            data_start += consumed;
                            values.Add(value);
                        }
                else
                {
                    uint consum = 0;
                    while (consum < tag.value)
                    {
                        GetVariableWidthValue(data_start);
                        data_start += consumed;
                        consum += consumed;
                        values.Add(value);
                    }
                    if (consum != tag.value)
                        throw new UnpackKindleSException("tag decode error");
                }
                hashtable[tag.tag] = values;
            }

            //to-do:some check

            return hashtable;

        }
        int value; uint consumed;
        void GetVariableWidthValue(uint offset)
        {
            consumed = 0; value = 0;
            bool finish = false;
            while (!finish)
            {
                byte x = raw[offset + consumed];
                consumed++;
                if ((x & 0x80) > 0) finish = true;
                value = (value << 7) | (x & 0x7f);
            }
        }
        int CountBit(int a) { int count = 0; for (int i = 0; i < 8; i++) { if ((a & 1) > 0) count++; a = a >> 1; } return count; }

    }
    class CTOC_Section : Section
    {
        public Hashtable ctoc_data;
        public CTOC_Section(byte[] data) : base(data)
        {
            type = "CTOC";
            ctoc_data = new Hashtable();
            int offset = 0, idx_offs;
            while (offset < data.Length)
            {
                if (data[offset] == 0) break;
                idx_offs = offset;
                GetVariableWidthValue(offset);
                offset += consumed;
                string name = Encoding.UTF8.GetString(data, offset, value);
                offset += value;
                ctoc_data[idx_offs] = name;
            }

        }
        int value; int consumed;
        void GetVariableWidthValue(int offset)
        {
            consumed = 0; value = 0;
            bool finish = false;
            while (!finish)
            {
                byte x = raw[offset + consumed];
                consumed++;
                if ((x & 0x80) > 0) finish = true;
                value = (value << 7) | (x & 0x7f);
            }
        }
    }
    class Font_Section : Section
    {
        public uint original_length, flags, font_data_start, xor_length, xor_start;
        const uint ZLIB = 0x1, XOR = 0x2;
        public byte[] data;
        public string ext;
        public Font_Section(Section section) : base(section)
        {
            original_length = Util.GetUInt32(raw, 0x04);
            flags = Util.GetUInt32(raw, 0x08);
            font_data_start = Util.GetUInt32(raw, 0x0C);
            xor_length = Util.GetUInt32(raw, 0x10);
            xor_start = Util.GetUInt32(raw, 0x14);
            data = Util.SubArray(raw, font_data_start, (uint)raw.Length - font_data_start);
            if ((XOR & flags) > 0)
            {
                //http://idpf.org/epub/20/spec/FontManglingSpec.html
                byte[] key = Util.SubArray(raw, xor_start, xor_length);
                for (int i = 0; i < 1040 && i < data.Length; i++)
                {
                    data[i] ^= key[i % key.Length];
                }

            }
            if ((ZLIB & flags) > 0)
            {

                using (var ms = new MemoryStream(data, 2, data.Length - 2))//Microsoft's DeflateStream class in System.Io.Compression doesn't understand the first two bytes.
                using (var zs = new DeflateStream(ms, CompressionMode.Decompress))
                {
                    byte[] temp = new byte[original_length];
                    zs.Read(temp, 0, (int)original_length);
                    data = temp;
                }
            }
            string header = Encoding.ASCII.GetString(data, 0, 4);
            switch (header)
            {
                case "OTTO":
                    ext = ".otf"; break;
                case "ttcf":
                case "true":
                case "\0\x1\0\0":
                    ext = ".ttf"; break;
                default: throw new Exception("Error at dump font.");
            }
        }
    }


}