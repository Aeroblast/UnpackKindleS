using System;
using System.Text;
using System.Collections.Generic;

namespace UnpackKindleS
{

    public interface TextSectionDecoder
    {
        byte[] Decode(byte[] data);

    }


    public class PalmdocDecoder : TextSectionDecoder
    {
        public byte[] Decode(byte[] data)
        {
            List<byte> r = new List<byte>();
            int pos = 0;
            while (pos < data.Length)
            {
                byte c = data[pos];
                pos++;
                if (c >= 1 && c <= 8)
                {
                    r.AddRange(Util.SubArray(data, pos, c));
                    pos += c;
                }
                else if (c < 128)
                {
                    r.Add(c);
                }
                else if (c >= 192)
                {
                    r.Add(0x20);
                    r.Add((byte)( c ^ 128));
                }
                else
                {
                    if (pos < data.Length)
                    {
                        int cx = (c << 8) | data[pos];
                        pos++;
                        int m = (cx >> 3) & 0x07ff;
                        int n = (cx & 7) + 3;
                        if (m > n) { r.AddRange(Util.SubArray(r.ToArray(), r.Count - m, n)); }
                        else
                        {
                            for (int i = 0; i < n; i++)
                            {
                                if (m == 1) r.AddRange(Util.SubArray(r.ToArray(), r.Count - m, m));
                                else r.Add(r[r.Count - m]);
                            }
                        }
                    }
                }
            }

            return r.ToArray();
        }
    }
    public class HuffmanDecoder : TextSectionDecoder
    {
        ulong[] mincode = new ulong[33];
        ulong[] maxcode = new ulong[33];
        uint[] codelen = new uint[256];
        bool[] term = new bool[256];
        uint[] maxcode1 = new uint[256];
        HuffmanCDIC cdic;



        public HuffmanDecoder(byte[] seed_section)
        {
            string ident = Encoding.ASCII.GetString(seed_section, 0, 4);
            if (ident != "HUFF") { throw new UnpackKindleSException("Unexpect Section Header at Huff Decoder"); }
            UInt32 off1 = Util.GetUInt32(seed_section, 8);
            UInt32 off2 = Util.GetUInt32(seed_section, 12);

            for (uint i = 0; i < 256; i++)
            {
                UInt32 v = Util.GetUInt32(seed_section, off1 + i * 4);
                codelen[i] = v & 0x1f; term[i] = (v & 0x80) > 0; maxcode1[i] = v >> 8;
                if (codelen[i] == 0 || (codelen[i] <= 8 && !term[i])) { throw new UnpackKindleSException("Huff decode error."); }
                maxcode1[i] = ((maxcode1[i] + 1) << (int)(32 - codelen[i])) - 1;
            }
            mincode[0] = 0;
            maxcode[0] = (((ulong)1) << (int)(32)) - 1;
            for (uint i = 1; i < 33; i++)
            {
                mincode[i] = Util.GetUInt32(seed_section, off2 + (i - 1) * 4 * 2);
                maxcode[i] = Util.GetUInt32(seed_section, off2 + (i - 1) * 4 * 2 + 4);
                mincode[i] = (mincode[i] << (int)(32 - i));
                maxcode[i] = ((maxcode[i] + 1) << (int)(32 - i)) - 1;
            }
            cdic = new HuffmanCDIC();

        }
        public void AddCDIC(byte[] CDIC_section)
        {
            cdic.Add(CDIC_section);
        }
        public byte[] Decode(byte[] _data)
        {
            byte[] data = new byte[_data.Length + 8]; _data.CopyTo(data, 0);

            long bitsleft = _data.Length * 8;
            ulong pos = 0;
            ulong x = Util.GetUInt64(data, pos);
            int n = 32;
            List<byte> s = new List<byte>();
            while (true)
            {
                if (n <= 0)
                {
                    pos += 4;
                    x = Util.GetUInt64(data, pos);
                    n += 32;
                }
                ulong code = (x >> n) & (((ulong)1 << 32) - 1);
                ulong dict1_i = code >> 24;
                uint _codelen = codelen[dict1_i];
                ulong _maxcode = maxcode1[dict1_i];
                if (!term[dict1_i])
                {
                    while (code < mincode[_codelen]) _codelen++;

                    _maxcode = maxcode[_codelen];
                }
                n -= (int)_codelen;
                bitsleft -= _codelen;
                if (bitsleft < 0) break;

                ulong r = (_maxcode - code) >> (int)(32 - _codelen);
                byte[] slice = cdic.slice[(int)r];
                bool flag = cdic.slice_flag[(int)r];

                if (!flag)
                {
                    cdic.slice[(int)r] = new byte[0];
                    slice = Decode(slice);
                    cdic.slice[(int)r] = slice; cdic.slice_flag[(int)r] = true;//self.dictionary[r] = (slice, 1)
                }
                s.AddRange(slice);

            }
            return s.ToArray();

        }

    }

    public class HuffmanCDIC
    {
        public List<byte[]> slice = new List<byte[]>();
        public List<bool> slice_flag = new List<bool>();
        public void Add(byte[] raw)
        {
            string ident = Encoding.ASCII.GetString(raw, 0, 4);
            if (ident != "CDIC") { throw new UnpackKindleSException("Unexpect Section Header at CDIC"); }
            UInt32 phases = Util.GetUInt32(raw, 8);
            UInt32 bits = Util.GetUInt32(raw, 12);
            long n = Math.Min(1 << (int)bits, phases - slice.Count);

            for (int i = 0; i < n; i++)
            {
                UInt16 off = Util.GetUInt16(raw, (ulong)(16 + i * 2));
                UInt16 length = Util.GetUInt16(raw, (ulong)(16 + off));
                slice_flag.Add((length & 0x8000) > 0);
                slice.Add(Util.SubArray(raw, (ulong)(18 + off), (ulong)(length & 0x7fff)));
            }

        }


    }

    //方便在debug监视器看
    class HuffmanCDIC_Section : Section
    {
        public HuffmanCDIC_Section() : base("Hunffman CDIC", null) { }
    }
    class Huffman_Section : Section
    {
        public Huffman_Section() : base("Hunffman", null) { }
    }
}