using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace UnpackKindleS
{

    public class Azw3File : AzwFile
    {
        public string title
        {
            get { return mobi_header.title; }
        }
        public string author
        {
            get
            {
                if (mobi_header.extMeta.id_string.ContainsKey(100))
                    return mobi_header.extMeta.id_string[100];
                else return "";
            }
        }
        public byte[] rawML;
        public MobiHeader mobi_header;

        public TextSectionDecoder decoder;
        public FDST_Section fdst;
        public RESC_Section resc;
        public List<string> xhtmls = new List<string>();
        public List<string> flows = new List<string>();//without first flow(main xhtmls)
        public string[] flowProcessLog;

        public Azw3File(string path) : base(path)
        {

            if (section_count > 0)
            {
                sections = new Section[section_count];
                if (ident == "BOOKMOBI")
                {
                    mobi_header = new MobiHeader(GetSectionData(0));
                    sections[0] = mobi_header;
                    if (mobi_header.codepage != 65001) { throw new UnpackKindleSException("not UTF8"); }
                    if (mobi_header.version != 8) { throw new UnpackKindleSException("Unhandle mobi version:" + mobi_header.version); }

                    GetRawML();
                    ProcessRes();
                    ProcessIndex();
                    BuildParts();
                    for (uint i = 0; i < section_count; i++) if (sections[i] == null) sections[i] = new Section(GetSectionData(i));
                }
            }
        }


        void GetRawML()
        {
            switch (mobi_header.compression)
            {
                case 2:
                    decoder = new PalmdocDecoder();
                    break;
                case 0x4448:
                    {
                        byte[] r;
                        r = GetSectionData(mobi_header.huffman_start_index);
                        HuffmanDecoder _decoder = new HuffmanDecoder(r);
                        sections[mobi_header.huffman_start_index] = new Huffman_Section(r);
                        for (uint i = 0; i < mobi_header.huffman_count - 1; i++)
                        {
                            r = GetSectionData(mobi_header.huffman_start_index + i + 1);
                            _decoder.AddCDIC(r);
                            sections[mobi_header.huffman_start_index + i + 1] = new HuffmanCDIC_Section(r);
                        }
                        decoder = _decoder;
                    }
                    break;

                default:
                    throw new UnpackKindleSException("Unhandled compression type.");
            }


            bool multibyte = (mobi_header.mobi_flags & 1) > 0;
            int trailers = 0;
            Func<byte[], byte[]> Trim = (data) =>
               {
                   for (int i = 0; i < trailers; i++)
                   {
                       int num = 0;
                       for (int j = Math.Max(data.Length - 4, 0); j < data.Length; j++)
                       { if (data[j] > 0x80) num = 0; num = (num << 7) | (data[j] & 0x7f); }
                       data = Util.SubArray(data, 0, data.Length - num);
                   }
                   if (multibyte)
                   { int num = (data[data.Length - 1] & 3) + 1; data = Util.SubArray(data, 0, data.Length - num); }
                   return data;
               };


            UInt16 t = mobi_header.mobi_flags;
            while (t > 1) { if ((t & 2) > 0) trailers++; t = (ushort)(t >> 1); }
            List<byte> rawMLraw = new List<byte>();
            for (int i = 0; i < mobi_header.records; i++)
            {
                sections[i + 1] = new Text_Section(GetSectionData((uint)i + 1));
                rawMLraw.AddRange(decoder.Decode(Trim(GetSectionData((uint)(i + 1)))));
            }
            rawML = rawMLraw.ToArray();

        }

        void ProcessRes()
        {
            for (uint i = mobi_header.first_res_index; i < section_count; i++)
            {
                sections[i] = new Section(GetSectionData(i));
                switch (sections[i].type)
                {
                    case "FDST":
                        sections[i] = new FDST_Section(sections[i]);
                        fdst = (FDST_Section)sections[i];
                        break;
                    case "RESC":
                        sections[i] = new RESC_Section(sections[i]);
                        resc = (RESC_Section)sections[i];
                        break;
                    default:
                        string r = Util.GuessImageType(sections[i].raw);
                        if (r != null)
                            sections[i] = new Image_Section(sections[i], r);
                        break;
                }

            }

        }

        public List<Skeleton_item> skeleton_table;
        public List<Fragment_item> frag_table;
        public List<Guide_item> guide_table;
        public List<IndexInfo_item> index_info_table;
        void ProcessIndex()
        {

            if (mobi_header.skel_index != 0xffffffff)
            {
                skeleton_table = new List<Skeleton_item>();
                INDX_Section_Main main_indx = new INDX_Section_Main(GetSectionData(mobi_header.skel_index), "INDX(Skeleton)");
                sections[mobi_header.skel_index] = main_indx;
                main_indx.ReadTag();
                for (uint i = 0; i < main_indx.header.any_count; i++)
                {
                    INDX_Section_Extra ext_indx =
                    new INDX_Section_Extra(GetSectionData(mobi_header.skel_index + i + 1), main_indx);
                    ext_indx.ReadTagMap();
                    sections[mobi_header.skel_index + i + 1] = ext_indx;
                    for (int j = 0; j < ext_indx.texts.Length; j++)
                    {
                        List<int> values = (List<int>)(ext_indx.tagmaps[j][1]);//有点毛病……结果OK
                        skeleton_table.Add(new Skeleton_item(ext_indx.texts[j], values[0], values[2], values[3]));
                    }
                }
            }
            if (mobi_header.frag_index != 0xffffffff)
            {
                Hashtable ctoc_dict = new Hashtable();
                frag_table = new List<Fragment_item>();
                INDX_Section_Main main_indx = new INDX_Section_Main(GetSectionData(mobi_header.frag_index), "INDX(Fragment)");
                sections[mobi_header.frag_index] = main_indx;
                main_indx.ReadTag();
                int ctoc_off = 0;
                for (uint i = 0; i < main_indx.header.ctoc_count; i++)
                {
                    uint off = mobi_header.frag_index + main_indx.header.any_count + 1 + i;
                    CTOC_Section ctoc = new CTOC_Section(GetSectionData(off));
                    sections[off] = ctoc;
                    foreach (int key in ctoc.ctoc_data.Keys)
                    {
                        ctoc_dict[key + ctoc_off] = ctoc.ctoc_data[key];
                    }
                    ctoc_off += 0x10000;
                }
                for (uint i = 0; i < main_indx.header.any_count; i++)
                {
                    INDX_Section_Extra ext_indx =
                    new INDX_Section_Extra(GetSectionData(mobi_header.frag_index + i + 1), main_indx);
                    sections[mobi_header.frag_index + i + 1] = ext_indx;
                    ext_indx.ReadTagMap();
                    for (int j = 0; j < ext_indx.texts.Length; j++)
                    {
                        List<int> values = (List<int>)(ext_indx.tagmaps[j][2]);//有点毛病……结果OK
                        frag_table.Add(new Fragment_item(ext_indx.texts[j], (string)ctoc_dict[values[0]], values[1], values[2], values[3], values[4]));
                    }
                }
            }
            if (mobi_header.guide_index != 0xffffffff)
            {
                Hashtable ctoc_dict = new Hashtable();
                guide_table = new List<Guide_item>();
                INDX_Section_Main main_indx = new INDX_Section_Main(GetSectionData(mobi_header.guide_index), "INDX(Guide)");
                sections[mobi_header.guide_index] = main_indx;
                main_indx.ReadTag();
                int ctoc_off = 0;
                for (uint i = 0; i < main_indx.header.ctoc_count; i++)
                {
                    uint off = mobi_header.guide_index + main_indx.header.any_count + 1 + i;
                    CTOC_Section ctoc = new CTOC_Section(GetSectionData(off));
                    sections[off] = ctoc;
                    foreach (int key in ctoc.ctoc_data.Keys)
                    {
                        ctoc_dict[key + ctoc_off] = ctoc.ctoc_data[key];
                    }
                    ctoc_off += 0x10000;
                }
                for (uint i = 0; i < main_indx.header.any_count; i++)
                {
                    INDX_Section_Extra ext_indx =
                    new INDX_Section_Extra(GetSectionData(mobi_header.guide_index + i + 1), main_indx);
                    sections[mobi_header.guide_index + i + 1] = ext_indx;
                    ext_indx.ReadTagMap();
                    for (int j = 0; j < ext_indx.texts.Length; j++)
                    {
                        List<int> values = (List<int>)(ext_indx.tagmaps[j][1]);//有点毛病……结果OK
                        guide_table.Add(new Guide_item(ext_indx.texts[j], (string)ctoc_dict[values[0]], values[1]));
                    }
                }
            }
            if (mobi_header.ncx_index != 0xffffffff)
            {
                index_info_table = new List<IndexInfo_item>();
                Hashtable ctoc_dict = new Hashtable();
                INDX_Section_Main main_indx = new INDX_Section_Main(GetSectionData(mobi_header.ncx_index), "INDX(NCX)");
                sections[mobi_header.ncx_index] = main_indx;
                main_indx.ReadTag();
                int ctoc_off = 0;
                for (uint i = 0; i < main_indx.header.ctoc_count; i++)
                {
                    uint off = mobi_header.ncx_index + main_indx.header.any_count + 1 + i;
                    CTOC_Section ctoc = new CTOC_Section(GetSectionData(off));
                    sections[off] = ctoc;
                    foreach (int key in ctoc.ctoc_data.Keys)
                    {
                        ctoc_dict[key + ctoc_off] = ctoc.ctoc_data[key];
                    }
                    ctoc_off += 0x10000;
                }
                for (uint i = 0; i < main_indx.header.any_count; i++)
                {
                    INDX_Section_Extra ext_indx =
                    new INDX_Section_Extra(GetSectionData(mobi_header.ncx_index + i + 1), main_indx);
                    sections[mobi_header.ncx_index + i + 1] = ext_indx;
                    ext_indx.ReadTagMap();
                    for (int j = 0; j < ext_indx.tagmaps.Length; j++)
                    {
                        IndexInfo_item item = new IndexInfo_item();
                        item.name = ext_indx.texts[j];
                        foreach (var k in ext_indx.tagmaps[j])
                        {
                            List<int> a = (List<int>)((DictionaryEntry)k).Value;
                            item.position = a[0];
                            item.length = a[1];
                            item.title = (string)ctoc_dict[a[2]];
                            item.level = a[3];
                            if (item.level > 0)
                            {
                                switch (a.Count)
                                {
                                    case 7:
                                        item.parent = a[4];
                                        item.fid = a[5];
                                        item.off = a[6];
                                        break;
                                    case 9:
                                        item.parent = a[4];
                                        item.children_start = a[5];
                                        item.children_end = a[6];
                                        item.fid = a[7];
                                        item.off = a[8];
                                        break;
                                }

                            }
                            else
                            {
                                switch (a.Count)
                                {
                                    case 6:
                                        item.fid = a[4];
                                        item.off = a[5];
                                        break;
                                    case 7:
                                        item.fid = a[5];
                                        item.off = a[6];
                                        break;
                                    case 8:
                                        item.children_start = a[4];
                                        item.children_end = a[5];
                                        item.fid = a[6];
                                        item.off = a[7];
                                        break;
                                    default: throw new Exception("Unhandled Error at INDX");
                                }
                            }
                            break;
                        }
                        index_info_table.Add(item);
                        //Console.WriteLine($"name={item.name} fid={item.fid} off={item.off} parent={item.parent} {item.title}");
                    }
                }
            }
        }

        void BuildParts()
        {
            uint[] lens;
            byte[] texts;
            if (fdst == null)
            {
                lens = new uint[1];
                texts = rawML;
                Log.log("[Warn]Cannot find FDST Section.");
            }
            else
            {
                lens = new uint[fdst.table.Length];
                for (int i = 0; i < lens.Length - 1; i++) lens[i] = fdst.table[i + 1] - fdst.table[i];
                lens[fdst.table.Length - 1] = (uint)rawML.Length - fdst.table[fdst.table.Length - 1];
                texts = Util.SubArray(rawML, fdst.table[0], lens[0]);
            }


            int frag_index = 0;
            foreach (var ske in skeleton_table)
            {
                int pos = ske.start_pos + ske.length;
                byte[] part = Util.SubArray(texts, ske.start_pos, ske.length);
                for (int i = 0; i < ske.record_count; i++)
                {
                    byte[] middle = Util.SubArray(texts, pos, frag_table[frag_index].length);
                    frag_table[frag_index].pos_in_raw = pos;//for reverse search
                    frag_table[frag_index].xhtml = xhtmls.Count;//for reverse search
                    pos += frag_table[frag_index].length;
                    List<byte> temp = new List<byte>(part);
                    temp.InsertRange(
                        frag_table[frag_index].file_postion - ske.start_pos
                        , middle);
                    part = temp.ToArray();
                    frag_index++;
                }
                string s = Encoding.UTF8.GetString(part);
                xhtmls.Add(s);
            }
            for (int i = 1; i < lens.Length; i++)
            {
                flows.Add(Encoding.UTF8.GetString(Util.SubArray(rawML, fdst.table[i], lens[i])));
            }
        }

    }


}