using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;

namespace UnpackKindleS
{


    public class Azw6File : AzwFile
    {

        public string title
        {
            get { return header.title; }
        }
        public Azw6Header header;
        public HREF_Section hrefs_sec;
        public List<string> hrefs { get { return hrefs_sec.hrefs; } }

        public List<int> image_sections = new List<int>();
        public Azw6File(string path) : base(path)
        {
            if (section_count > 0)
            {
                sections = new Section[section_count];
                if (ident == "RBINCONT")
                {
                    header = new Azw6Header(GetSectionData(0));
                    sections[0] = header;
                    ProcessRes();
                    if(image_sections.Count!=hrefs.Count){throw new UnpackKindleSException("HD Container herf and section 数量不一致");}
                }
            }
        }


        void ProcessRes()
        {
            for (uint i = 0; i < section_count; i++)
            {
                sections[i] = new Section(GetSectionData(i));
                switch (sections[i].type)
                {
                    case "kind":
                        hrefs_sec = new HREF_Section(sections[i].raw);
                        sections[i] = hrefs_sec;
                        break;
                    case "CRES":
                            sections[i] = new CRES_Section(sections[i]);
                            image_sections.Add((int)i);
                        break;
                    default:
                    if(Util.GetUInt32(sections[i].raw,0)==0xa0a0a0a0)
                        sections[i]=new PlaceHolder_Section(sections[i]);
                        break;
                }

            }

        }

    }
    public class HREF_Section : Section
    {
        public List<string> hrefs;
        public HREF_Section(byte[] raw) : base("href of Images", raw)
        {
            string[] _hrefs = Encoding.UTF8.GetString(raw).Split('|');
            List<string> ist = new List<string>();
            Regex regex = new Regex("kindle:embed:(.*?)\\?mime=image/(.*)");
            foreach (string s in _hrefs) { Match m=regex.Match(s);if (m.Success) ist.Add(m.Groups[0].Value); }
            hrefs = ist;
        }
    }
    public class CRES_Section : Section
    {
        public string ext;
        public byte[] img;
        public CRES_Section(Section s) : base(s)
        {
            img = Util.SubArray(raw, 12, (ulong)raw.Length - 12);
            ext = Util.GuessImageType(img);
        }
    }
    public class PlaceHolder_Section :Section
    {
        public PlaceHolder_Section(Section s):base(s){}
    }

}