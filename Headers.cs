using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnpackKindleS
{

    public class ExtMeta
    {
        public Dictionary<UInt32, UInt32> id_value;
        public Dictionary<UInt32, string> id_string;
        public Dictionary<UInt32, string> id_hex;

        public ExtMeta(byte[] ext)
        {
            id_value = new Dictionary<uint, uint>();
            id_string = new Dictionary<uint, string>();
            id_hex = new Dictionary<uint, string>();

            UInt32 len = Util.GetUInt32(ext, 4);
            UInt32 num_items = Util.GetUInt32(ext, 8);
            uint pos = 12;
            for (int i = 0; i < num_items; i++)
            {
                UInt32 id = Util.GetUInt32(ext, pos);
                UInt32 size = Util.GetUInt32(ext, pos + 4);
                if (IdMapping.id_map_strings.ContainsKey(id))
                {
                    string a = Encoding.UTF8.GetString(Util.SubArray(ext, pos + 8, size - 8));
                    //Log.log(" " + IdMapping.id_map_strings[id] + ":" + a);
                    id_string.Add(id, a);
                }
                else
                if (IdMapping.id_map_values.ContainsKey(id))
                {
                    UInt32 a = 0;
                    switch (size)
                    {
                        case 9: a = Util.GetUInt8(ext, pos + 8); break;
                        case 10: a = Util.GetUInt16(ext, pos + 8); break;
                        case 12: a = Util.GetUInt32(ext, pos + 8); break;
                        default: Log.log("unexpected size:" + size); break;
                    }
                   // Log.log(" " + IdMapping.id_map_values[id] + ":" + a);
                    id_value.Add(id, a);
                }
                else
                if (IdMapping.id_map_hex.ContainsKey(id))
                {
                    string a = Util.ToHexString(ext, pos + 8, size - 8);
                   // Log.log(" " + IdMapping.id_map_hex[id] + ":" + a);
                    id_hex.Add(id, a);
                }
                else
                {
                    string a = Util.ToHexString(ext, pos + 8, size - 8);
                    Log.log(" unknown id " + id + ":" + a);
                }

                pos += size;
            }
        }
    }
    public class MobiHeader : Section
    {
        public string title;
        public UInt16 records, compression, crypto_type, mobi_flags;
        public UInt32 length, mobi_type, codepage, unique_id, version, exth_flag, first_res_index, first_nontext_index,
        ncx_index, frag_index, skel_index, guide_index, fdst_start_index, fdst_count,
        mobi_version, mobi_length, huffman_start_index, huffman_count;

        public ExtMeta extMeta;

        public MobiHeader(byte[] header) : base("Mobi Header", header)
        {
            string mobi = Encoding.ASCII.GetString(header, 16, 4);
            records = Util.GetUInt16(header, 8);
            compression = Util.GetUInt16(header, 0);
            if (compression == 0x4448)
            {
                huffman_start_index = Util.GetUInt32(header, 0x70);
                huffman_count = Util.GetUInt32(header, 0x74);
            }
            length = Util.GetUInt32(header, 20);
            mobi_type = Util.GetUInt32(header, 24);
            codepage = Util.GetUInt32(header, 28);
            unique_id = Util.GetUInt32(header, 32);
            version = Util.GetUInt32(header, 36);
            title = Encoding.UTF8.GetString(
                header,
                (int)Util.GetUInt32(header, 0x54),
                (int)Util.GetUInt32(header, 0x58)
                );
            exth_flag = Util.GetUInt32(header, 0x80);
            if ((exth_flag & 0x40) > 0)
            {
                byte[] exth = Util.SubArray(header, length + 16,
                    Util.GetUInt32(header, length + 20));
                extMeta = new ExtMeta(exth);

            }
            crypto_type = Util.GetUInt16(header, 0xc); if (crypto_type != 0) { throw new UnpackKindleSException("Unable to handle an encrypted file. Crypto Type:" + crypto_type); }
            first_res_index = Util.GetUInt32(header, 0x6c);
            first_nontext_index = Util.GetUInt32(header, 0x50);
            ncx_index = Util.GetUInt32(header, 0xf4);
            skel_index = Util.GetUInt32(header, 0xfc);
            frag_index = Util.GetUInt32(header, 0xf8);
            guide_index = Util.GetUInt32(header, 0x104);
            fdst_start_index = Util.GetUInt32(header, 0xc0);
            fdst_count = Util.GetUInt32(header, 0xc4);

            mobi_length = Util.GetUInt32(header, 0x14);
            mobi_version = Util.GetUInt32(header, 0x68);
            mobi_flags = Util.GetUInt16(header, 0xf2);
        }
    }

    public class Azw6Header : Section
    {
        public Azw6HeaderInfo info;
        public ExtMeta meta;
        public string title;

        public Azw6Header(byte[] header_raw) : base("Azw6 Header",header_raw)
        {
            int header_size = Marshal.SizeOf(typeof(Azw6HeaderInfo));
            // Byte[] header_raw = Util.SubArray(azw6_data, section_info[0].start_addr, (ulong)header_size);
            info = Util.GetStructBE<Azw6HeaderInfo>(header_raw, 0);
            Array.Reverse(info.magic);
            if (info.codepage != 65001) return;
            Byte[] title_raw = Util.SubArray(header_raw, info.title_offset, info.title_length);
            title = Encoding.UTF8.GetString(title_raw);
            //Log.log("Azw6 File Title:" + title);
            Byte[] ext = Util.SubArray(header_raw,
            48,
            header_raw.Length - 48
            );
            meta = new ExtMeta(ext);
        }
    }
}