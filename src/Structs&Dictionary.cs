using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
namespace UnpackKindleS
{


    public struct SectionInfo
    {
        public ulong start_addr;
        public ulong end_addr;
        public ulong length { get { return end_addr - start_addr; } }
    }
    public class AzwFile
    {
        byte[] raw_data;
        public ushort section_count;
        public SectionInfo[] section_info;
        public Section[] sections;
        public string ident;

        protected void GetSectionInfo()
        {
            ident = Encoding.ASCII.GetString(raw_data, 0x3c, 8);
            section_count = Util.GetUInt16(raw_data, 76);
            section_info = new SectionInfo[section_count];

            section_info[0].start_addr = Util.GetUInt32(raw_data, 78);
            for (uint i = 1; i < section_count; i++)
            {
                section_info[i].start_addr = Util.GetUInt32(raw_data, 78 + i * 8);
                //UInt32 tmp = Util.GetUInt32(raw_data, 78 + i * 8+4);if(i<20&&tmp!=i*2){error="Section Struct Not Match at"+i;return;}//这个字段是0 2 4 6 8...
                section_info[i - 1].end_addr = section_info[i].start_addr;
            }
            section_info[section_count - 1].end_addr = (ulong)raw_data.Length;
        }

        protected byte[] GetSectionData(uint i)
        {
            Byte[] d = Util.SubArray(raw_data, section_info[i].start_addr, section_info[i].length);
            return d;
        }
        protected AzwFile(string path)
        {
                        raw_data = File.ReadAllBytes(path);
            GetSectionInfo();

        }

    }

    [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct Azw6HeaderInfo
    {
        public UInt32 title_length;
        public UInt32 title_offset;
        public UInt32 unknown2;
        public UInt32 offset_to_hrefs;
        public UInt32 num_wo_placeholders;

        public UInt32 num_resc_recs;
        public UInt32 unknown1;
        public UInt32 unknown0;
        public UInt32 codepage;
        public UInt16 count;
        public UInt16 type;
        public UInt32 record_size;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] magic;
    }

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
    public struct INDX_Section_Header
    {
        public UInt32 ctoc_count, ligt_count, ligt, ordt, total, lng,
        codepage, any_count, index_offset, gen, type, nul1, tag_part_start;

    }
    public class INDX_Section_Tag
    {
        public int tag; public int count; public int value, tag_value;
        public INDX_Section_Tag(int _tag, int _count, int _value, int _tag_value) { tag = _tag; count = _count; value = _value; tag_value = _tag_value; }
    }
    public class Skeleton_item
    {
        public string name;
        public int record_count, start_pos, length;
        public Skeleton_item(string _name, int count, int pos, int len)
        { name = _name; record_count = count; start_pos = pos; length = len; }
    }
    public class Fragment_item
    {
        public string name;
        public int file_postion, file_num, squence_num, start_offset, length;

        public int pos_in_raw; public int xhtml;//for reverse search
        public Fragment_item(string file_pos, string _name, int _file_num, int sq_num, int off, int len)
        { file_postion = int.Parse(file_pos); name = _name; file_num = _file_num; squence_num = sq_num; start_offset = off; length = len; }
    }
    public class Guide_item
    {
        public string ref_type, ref_name;
        public int num;
        public Guide_item(string _ref_type, string _ref_name, int no) { ref_type = _ref_type; ref_name = _ref_name; num = no; }
    }
    public class NCX_item
    {
        public string title, name;
        public int fid, off, position, length;
    }
    public class IdMapping
    {
        public static Dictionary<uint, string> id_map_strings = new Dictionary<uint, string>
        {
           {1,"Drm Server Id (1)"},
           {2,"Drm Commerce Id (2)"},
           {3,"Drm Ebookbase Book Id(3)"},
           {100,"Creator_(100)"},
           {101,"Publisher_(101)"},
           {102,"Imprint_(102)"},
           {103,"Description_(103)"},
           {104,"ISBN_(104)"},
           {105,"Subject_(105)"},
           {106,"Published_(106)"},
           {107,"Review_(107)"},
           {108,"Contributor_(108)"},
           {109,"Rights_(109)"},
           {110,"SubjectCode_(110)"},
           {111,"Type_(111)"},
           {112,"Source_(112)"},
           {113,"ASIN_(113)"},
           {114,"versionNumber_(114)"},
           {117,"Adult_(117)"},
           {118,"Price_(118)"},
           {119,"Currency_(119)"},
           {122,"fixed-layout_(122)"},
           {123,"book-type_(123)"},
           {124,"orientation-lock_(124)"},
           {126,"original-resolution_(126)"},
           {127,"zero-gutter_(127)"},
           {128,"zero-margin_(128)"},
           {129,"K8_Masthead/Cover_Image_(129)"},
           {132,"RegionMagnification_(132)"},
           {200,"DictShortName_(200)"},
           {208,"Watermark_(208)"},
           {501,"cdeType_(501)"},
           {502,"last_update_time_(502)"},
           {503,"Updated_Title_(503)"},
           {504,"ASIN_(504)"},
           {508,"Title_Katagana_(508)"},
           {517,"Creator_Katagana_(517)"},
           {522,"Publisher_Katagana_(522)"},
           {524,"Language_(524)"},
           {525,"primary-writing-mode_(525)"},
           {526,"Unknown_(526)"},
           {527,"page-progression-direction_(527)"},
           {528,"override-kindle_fonts_(528)"},
           {529,"Unknown_(529)"},
           {534,"Input_Source_Type_(534)"},
           {535,"Kindlegen_BuildRev_Number_(535)"},
           {536,"Container_Info_(536)"}, // CONT_Header is 0, Ends with CONTAINER_BOUNDARY (or Asset_Type?)
           {538,"Container_Resolution_(538)"},
           {539,"Container_Mimetype_(539)"},
           {542,"Unknown_but_changes_with_filename_only_(542)"},
           {543,"Container_id_(543)"},  // FONT_CONTAINER, BW_CONTAINER, HD_CONTAINER
           {544,"Unknown_(544)"}
        };

        public static Dictionary<uint, string> id_map_values = new Dictionary<uint, string>()
        {
           {115,"sample_(115)"},
           {116,"StartOffset_(116)"},
           {121,"K8(121)_Boundary_Section_(121)"},
           {125,"K8_Count_of_Resources_Fonts_Images_(125)"},
           {131,"K8_Unidentified_Count_(131)"},
           {201,"CoverOffset_(201)"},
           {202,"ThumbOffset_(202)"},
           {203,"Fake_Cover_(203)"},
           {204,"Creator_Software_(204)"},
           {205,"Creator_Major_Version_(205)"},
           {206,"Creator_Minor_Version_(206)"},
           {207,"Creator_Build_Number_(207)"},
           {401,"Clipping_Limit_(401)"},
           {402,"Publisher_Limit_(402)"},
           {404,"Text_to_Speech_Disabled_(404)"},
           {406,"Rental_Indicator_(406)"}
        };

        public static Dictionary<uint, string> id_map_hex = new Dictionary<uint, string>()
        {
            {208,"Watermark(208 in hex)"},
            { 209 , "Tamper_Proof_Keys_(209_in_hex)"},
           {300 , "Font_Signature_(300_in_hex)"}
        };
    }


}