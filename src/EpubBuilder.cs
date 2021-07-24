using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml;
using System.Collections.Generic;
using System.IO.Compression;
namespace UnpackKindleS
{

    public class Epub
    {
        Azw3File azw3;
        Azw6File azw6;

        public bool renameXhtmlWithId = true;


        List<XmlDocument> xhtmls = new List<XmlDocument>();
        List<string> xhtml_names = new List<string>();

        List<string> csss = new List<string>();
        List<string> css_names = new List<string>();
        List<byte[]> imgs = new List<byte[]>();
        List<string> img_names = new List<string>();
        List<string> font_names = new List<string>();
        List<byte[]> fonts = new List<byte[]>();


        string cover_name;

        string opf;
        string ncx;
        string nav;
        public Epub(Azw3File azw3, Azw6File azw6, bool rename_xhtml_with_id)
        {
            this.azw3 = azw3;
            this.azw6 = azw6;
            renameXhtmlWithId = rename_xhtml_with_id;
            azw3.flowProcessLog = new string[azw3.flows.Count];
            for (int i = 0; i < azw3.xhtmls.Count; i++)
                xhtml_names.Add("part" + Util.Number(i) + ".xhtml");
            if (renameXhtmlWithId)
            {
                Log.log("[Info]Rename xhtmls with id.");
                int i = 0, offset = 0;
                if (NeedCreateCoverDocument())
                {
                    i = 1; offset = -1;
                }
                // var itemrefs = azw3.resc.spine.FirstChild.ChildNodes;
                var itemrefs = azw3.resc.spine.GetElementsByTagName("itemref");
                for (; (i + offset < xhtml_names.Count) && (i < itemrefs.Count); i++)
                {
                    var id = itemrefs[i].Attributes.GetNamedItem("idref").Value;
                    xhtml_names[i + offset] = id + ".xhtml";
                }

            }
            foreach (string xhtml in azw3.xhtmls)
            {
                var doc = LoadXhtml(xhtml);
                xhtmls.Add(doc);
                ProcNodes(doc.DocumentElement);
            }
            SetCover();

#if DEBUG
            CreateIndexDoc();
#else
            try
            {
                CreateIndexDoc();
            }
            catch (Exception e)
            {
                Log.log("[Error]Cannot Create NCX or NAV.");
                Log.log("[Error]" + e.ToString());
            }
#endif

            UInt64 thumb_offset = 0;
            if (azw3.mobi_header.extMeta.id_value.TryGetValue(202, out thumb_offset))
            {
                azw3.sections[azw3.mobi_header.first_res_index + thumb_offset].comment = "Thumb Cover, Ignored";
            }

            foreach (var a in azw3.sections)
            {

                if (a.type == "Image" && string.IsNullOrEmpty(a.comment))
                {
                    int i = (int)Array.IndexOf(azw3.sections, a) - (int)azw3.mobi_header.first_res_index;
                    string name = AddImage(i, "Not referred");
                    Log.log($"[Warn] {name} is not referred.");
                }

            }

            CreateOPF();

        }
        const string container = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\">    <rootfiles><rootfile full-path=\"OEBPS/content.opf\" media-type=\"application/oebps-package+xml\"/>    </rootfiles></container>";
        public void Save(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create))
            using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                {
                    var entry = zip.CreateEntry("mimetype", CompressionLevel.NoCompression);
                    using (StreamWriter writer = new StreamWriter(entry.Open()))
                        writer.Write("application/epub+zip");
                }
                ZipWriteAllText(zip, "META-INF/container.xml", container);
                for (int i = 0; i < xhtml_names.Count; i++)
                {
                    string p = "OEBPS/Text/" + xhtml_names[i];
                    string ss;
                    var xmlSettings = new XmlWriterSettings { Indent = true, NewLineChars = "\n" };
                    using (var stringWriter = new StringWriter())
                    using (var xmlTextWriter = XmlWriter.Create(stringWriter, xmlSettings))
                    {
                        xhtmls[i].WriteTo(xmlTextWriter);
                        xmlTextWriter.Flush();
                        ss = stringWriter.GetStringBuilder().ToString();
                    }
                    ss = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE html>\n"
                    + ss.Substring(ss.IndexOf("<html"));
                    ZipWriteAllText(zip, p, ss);
                }
                for (int i = 0; i < css_names.Count; i++)
                {
                    ZipWriteAllText(zip, "OEBPS/Styles/" + css_names[i], csss[i]);
                }
                for (int i = 0; i < img_names.Count; i++)
                {
                    ZipWriteAllBytes(zip, "OEBPS/Images/" + img_names[i], imgs[i]);
                }
                for (int i = 0; i < font_names.Count; i++)
                {
                    ZipWriteAllBytes(zip, "OEBPS/Fonts/" + font_names[i], fonts[i]);
                }
                ZipWriteAllText(zip, "OEBPS/toc.ncx", ncx);
                ZipWriteAllText(zip, "OEBPS/nav.xhtml", nav);
                ZipWriteAllText(zip, "OEBPS/content.opf", opf);
            }
            Log.log("Saved: " + path);
        }
        void ZipWriteAllText(ZipArchive zip, string path, string text)
        {
            var entry = zip.CreateEntry(path);
            using (StreamWriter writer = new StreamWriter(entry.Open()))
            {
                writer.Write(text);
            }
        }
        void ZipWriteAllBytes(ZipArchive zip, string path, byte[] data)
        {
            var entry = zip.CreateEntry(path);
            using (Stream stream = entry.Open())
            {
                stream.Write(data, 0, data.Length);
            }
        }

        void ProcNodes(XmlNode node)
        {
            if (node.Attributes != null)
            {
                node.Attributes.RemoveNamedItem("aid");
                node.Attributes.RemoveNamedItem("amzn-src-id");
                foreach (XmlAttribute attr in node.Attributes)
                {
                    if (attr.Value.IndexOf("kindle:") == 0)
                    {
                        string h = attr.Value.Substring("kindle:".Length, 4);
                        switch (h)
                        {
                            case "pos:": ProcLink(attr); break;
                            case "flow": ProcTextRef(attr); break;
                            case "embe": attr.Value = ProcEmbed(attr.Value); break;
                        }
                    }
                    if (attr.Name == "style")
                    {
                        attr.Value = ProcCSS(attr.Value);
                    }
                }
            }
            if (node.NodeType == XmlNodeType.Element && node.Name == "style")
            {
                node.InnerXml = ProcCSS(node.InnerXml);
            }

            if (node.ChildNodes != null)
                foreach (XmlNode child in node.ChildNodes)
                {
                    ProcNodes(child);
                }
        }
        void ProcTextRef(XmlAttribute attr)
        {
            Regex reg_link = new Regex("kindle:flow:([0-9|A-V]+)\\?mime=.*?/(.*)");
            Match m = reg_link.Match(attr.Value);
            if (!m.Success)
            { Log.log("[Error]link unsolved"); return; }
            int flowid = (int)Util.DecodeBase32(m.Groups[1].Value);
            string mime = m.Groups[2].Value;
            switch (mime)
            {
                case "css":
                    string name = "flow" + Util.Number(flowid) + ".css";
                    if (css_names.Find(s => s == name) == null)
                    {
                        string csstext = azw3.flows[flowid - 1];
                        csstext = ProcCSS(csstext);
                        csss.Add(csstext);
                        css_names.Add(name);
                        azw3.flowProcessLog[flowid - 1] = name;
                    }
                    attr.Value = "../Styles/" + name;
                    break;
                case "svg+xml":
                    {
                        string text = azw3.flows[flowid - 1];
                        XmlElement svg = attr.OwnerDocument.CreateElement("temp");
                        svg.InnerXml = text;
                        foreach (XmlNode n in svg.ChildNodes)
                        {
                            if (n.Name == "svg")
                            {
                                attr.OwnerElement.ParentNode.InsertBefore(n, attr.OwnerElement);
                                attr.OwnerElement.ParentNode.RemoveChild(attr.OwnerElement);
                                ProcNodes(n);
                            }
                            if (n.Name == "xml-stylesheet")
                            {
                                Regex reg_link2 = new Regex("kindle:flow:([0-9|A-V]+)\\?mime=.*?/(.*?)\"");
                                Match ml = reg_link2.Match(n.OuterXml);
                                if (ml.Success && ml.Groups[2].Value == "css")
                                {
                                    int flowid_ = (int)Util.DecodeBase32(ml.Groups[1].Value);
                                    string name_ = "flow" + Util.Number(flowid_) + ".css";
                                    bool alreadyHave = false;
                                    foreach (XmlElement linktag in n.OwnerDocument.GetElementsByTagName("link"))
                                    {
                                        if (linktag.GetAttribute("href") == "../Styles/" + name_)
                                        { alreadyHave = true; break; }
                                    }
                                    if (!alreadyHave)
                                    {
                                        //rel="stylesheet" type="text/css"
                                        XmlElement l = n.OwnerDocument.CreateElement("link");
                                        l.SetAttribute("href", "../Styles/" + name_);
                                        l.SetAttribute("type", "text/css");
                                        l.SetAttribute("rel", "stylesheet");
                                        n.OwnerDocument.GetElementsByTagName("head")[0].AppendChild(l);
                                        if (css_names.Find(s => s == name_) == null)
                                        {
                                            string csstext = azw3.flows[flowid_ - 1];
                                            csstext = ProcCSS(csstext);
                                            csss.Add(csstext);
                                            css_names.Add(name_);
                                            azw3.flowProcessLog[flowid_ - 1] = name_;
                                        }

                                    }
                                }
                                else
                                { Log.log("cannot find css link in xml-stylesheet"); }

                            }
                        }
                        azw3.flowProcessLog[flowid - 1] = "Flow" + Util.Number(flowid) + " svg has been put into xhtmls";
                    }
                    break;
            }

        }
        void ProcLink(XmlAttribute attr)
        {
            Regex reg_link = new Regex("kindle:pos:fid:([0-9|A-V]+):off:([0-9|A-V]+)");

            Match link = reg_link.Match(attr.Value);
            if (!link.Success) { Log.log("[Error]link unsolved"); return; }
            int fid = (int)Util.DecodeBase32(link.Groups[1].Value);
            int off = (int)Util.DecodeBase32(link.Groups[2].Value);

            attr.Value = KindlePosToUri(fid, off);
        }
        string KindlePosToUri(int fid, int off)
        {
            int extraCoverOffset = extraCoverDocAdded ? 1 : 0;
            Regex reg_html_id = new Regex("^<.*? id=\"(.*?)\".*?>");
            Fragment_item frag = azw3.frag_table[fid];
            byte[] t = Util.SubArray(azw3.rawML, frag.pos_in_raw + off, frag.length - off);
            string s = Encoding.UTF8.GetString(t);
            Match m = reg_html_id.Match(s);
            if (m.Success)
                return xhtml_names[frag.xhtml + extraCoverOffset] + "#" + m.Groups[1].Value;
            else return xhtml_names[frag.xhtml + extraCoverOffset];
        }
        string ProcEmbed(string uri)
        {
            string link;
            Regex reg_path = new Regex("kindle:embed:([0-9|A-V]+)");
            Regex reg_query = new Regex("mime=(.*)/(.*)");
            Match m = reg_path.Match(uri);
            if (!m.Success) { Log.log("[Error]link unsolved: " + uri); return ""; }
            int resid = (int)Util.DecodeBase32(m.Groups[1].Value) - 1;
            if (uri.IndexOf('?') > 1)
            {
                m = reg_query.Match(uri);
                if (m.Groups[1].Value == "image")
                {
                    string name = AddImage(resid);
                    link = "../Images/" + name;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                Section section = azw3.sections[azw3.mobi_header.first_res_index + resid];
                switch (section.type)
                {
                    case "FONT":
                        {
                            Font_Section font_Section = (Font_Section)section;
                            string name = "embed" + Util.Number(resid) + font_Section.ext;
                            link = "../Fonts/" + name;
                            font_Section.comment = name;
                            fonts.Add(font_Section.data);
                            font_names.Add(name);
                        }
                        break;
                    default:
                        throw new NotImplementedException();

                }
            }
            return link;
        }

        //Process CSS file: Resolve links in css file
        string ProcCSS(string text)
        {
            string r = text;
            Regex reg_link = new Regex("url\\(kindle:flow:([0-9|A-V]+)\\?mime=text/css\\)");
            Regex reg_link2 = new Regex("url\\((kindle:embed:.+?)\\)");
            foreach (Match m in reg_link.Matches(text))
            {
                int flowid = (int)Util.DecodeBase32(m.Groups[1].Value);
                string name = "flow" + Util.Number(flowid) + ".css";
                if (css_names.Find(s => s == name) == null)
                {
                    string csstext = azw3.flows[flowid - 1];
                    csstext = ProcCSS(csstext);
                    csss.Add(csstext);
                    css_names.Add(name);
                    azw3.flowProcessLog[flowid - 1] = name;
                }
                r = r.Replace(m.Groups[0].Value, "url(" + name + ")");
            }
            foreach (Match m in reg_link2.Matches(text))
            {
                r = r.Replace(m.Groups[1].Value, ProcEmbed(m.Groups[1].Value));
            }
            return r;
        }

        private class IndexNode
        {
            public string href, title;
            public List<IndexNode> children;
            public IndexNode parent;
            public IndexNode(string href, string title)
            {
                this.href = href;
                this.title = title;
            }

        }
        int playOrder = 1;
        void CreateIndexDoc_Helper(IndexNode node, StringBuilder temp_epub3, StringBuilder temp_epub2, int level = 0)
        {
            string tabs = new String('\t', level);

            temp_epub3.Append("\n" + tabs + $"<ol>\n");
            foreach (var n in node.children)
            {

                temp_epub3.Append(tabs + $"<li><a href=\"{n.href}\">{n.title}</a>");

                temp_epub2.Append(tabs + $"<navPoint id=\"navPoint-{playOrder}\" playOrder=\"{playOrder}\">\n");
                temp_epub2.Append(tabs + $"\t<navLabel><text>{n.title}</text></navLabel>\n");
                temp_epub2.Append(tabs + $"\t<content src=\"{n.href}\" />\n");

                playOrder++;

                if (n.children != null)
                {
                    CreateIndexDoc_Helper(n, temp_epub3, temp_epub2, level + 1);
                }
                temp_epub3.Append("</li>\n");
                temp_epub2.Append(tabs + "</navPoint>\n");
            }
            temp_epub3.Append(tabs + $"</ol>\n");
        }
        void CreateIndexDoc()
        {
            List<IndexNode> allEntries = new List<IndexNode>();
            IndexNode root = new IndexNode("", "");
            root.children = new List<IndexNode>();
            int maxLevel = 0;
            if (azw3.index_info_table != null)
                for (int i = 0; i < azw3.index_info_table.Count; i++)
                {
                    IndexInfo_item info = azw3.index_info_table[i];
                    var entry = new IndexNode("Text/" + KindlePosToUri(info.fid, info.off), info.title);
                    allEntries.Add(entry);
                    if (info.children_start != -1) { entry.children = new List<IndexNode>(); }
                    if (info.level > 0)
                    {
                        entry.parent = allEntries[info.parent];
                        entry.parent.children.Add(entry);

                        //assert
                        var _item = azw3.index_info_table[info.parent];
                        if (_item.children_start > i || _item.children_end < i) { throw new Exception("Index Error"); }
                    }
                    else
                    {
                        root.children.Add(entry);
                    }
                    if (info.level > maxLevel) maxLevel = info.level;
                }
            StringBuilder temp_epub3 = new StringBuilder(), temp_epub2 = new StringBuilder();
            CreateIndexDoc_Helper(root, temp_epub3, temp_epub2);
            //Create NAV
            {
                string t = File.ReadAllText("template\\template_nav.txt");
                t = t.Replace("{❕toc}", temp_epub3.ToString());
                string guide = "";
                if (azw3.guide_table != null)
                    foreach (Guide_item g in azw3.guide_table)
                    {
                        try
                        {
                            int i = azw3.frag_table[g.num].file_num + 1;
                            guide += string.Format("    <li><a epub:type=\"{2}\" href=\"{1}\">{0}</a></li>\n", g.ref_name, Path.Combine("Text/", xhtml_names[i]), g.ref_type);
                        }
                        catch (Exception e)
                        {
                            Log.log("Error at Gen guide.");
                            Log.log(e.ToString());
                        }
                    }

                t = t.Replace("{❕guide}", guide);
                t = t.Replace("{❕cover}", "Text/" + xhtml_names[0]);
                nav = t;
            }
            {
                string t = File.ReadAllText("template\\template_ncx.txt");

                t = t.Replace("{❕navMap}", temp_epub2.ToString());
                t = t.Replace("{❕Title}", azw3.title);
                string z = azw3.mobi_header.extMeta.id_string[504];//ASIN
                t = t.Replace("{❕uid}", z);
                t = t.Replace("{❕depth}", maxLevel + 1 + "");
                ncx = t;
            }
        }
        bool extraCoverDocAdded = false;
        void SetCover()//等KindlePosToUri不再使用后 关系到xhtml相关变量
        {
            if (azw3.mobi_header.extMeta.id_value.ContainsKey(201))
            {
                string cover;
                int off = (int)azw3.mobi_header.extMeta.id_value[201];//CoverOffset
                if (azw3.mobi_header.first_res_index + off < azw3.section_count)
                    if (azw3.sections[azw3.mobi_header.first_res_index + off].type == "Image")
                    {
                        cover_name = AddImage(off, "Cover");

                        if (NeedCreateCoverDocument())
                        {
                            Log.log("[Info]Adding a cover document.");

                            string t = File.ReadAllText("template\\template_cover.txt");
                            var (w, h) = Util.GetImageSize(imgs[img_names.IndexOf(cover_name)]);
                            cover = t.Replace("{❕image}", cover_name).Replace("{❕w}", w.ToString()).Replace("{❕h}", h.ToString());

                            string coverDocumentName = "cover.xhtml";
                            if (renameXhtmlWithId)
                            {
                                var id = azw3.resc.spine.GetElementsByTagName("itemref")[0].Attributes.GetNamedItem("idref").Value;
                                coverDocumentName = id + ".xhtml";
                            }
                            xhtml_names.Insert(0, coverDocumentName);
                            XmlDocument cover_ = new XmlDocument();
                            cover_.LoadXml(cover);
                            xhtmls.Insert(0, cover_);
                            extraCoverDocAdded = true;
                        }

                    }
                return;
            }
            //if (azw3.mobi_header.extMeta.id_string.ContainsKey(129)){}
            Log.log("[Warn]No Cover!");
        }

        bool NeedCreateCoverDocument()
        {
            if (azw3.resc.spine.GetElementsByTagName("itemref").Count == xhtml_names.Count) return false;
            return true;
        }

        void CreateOPF()
        {
            if (azw3.resc != null)
            {
                string t = File.ReadAllText("template\\template_opf.txt");
                XmlDocument manifest = new XmlDocument();
                XmlElement mani_root = manifest.CreateElement("manifest");
                manifest.AppendChild(mani_root);

                int i = 0;

                foreach (XmlNode itemref in azw3.resc.spine.FirstChild.ChildNodes)
                {
                    if (itemref.NodeType != XmlNodeType.Element) continue;
                    itemref.Attributes.RemoveNamedItem("skelid");
                    string idref = itemref.Attributes.GetNamedItem("idref").Value;
                    XmlElement item = manifest.CreateElement("item");
                    item.SetAttribute("href", "Text/" + xhtml_names[i]);
                    item.SetAttribute("id", idref);
                    item.SetAttribute("media-type", "application/xhtml+xml");
                    if (ContainsSVG(xhtmls[i]))
                    {
                        item.SetAttribute("properties", "svg");
                    }
                    mani_root.AppendChild(item);
                    i++;
                }
                if (i > xhtmls.Count) Log.log("[Warn] Missing Parts. Ignore if this is a book sample.");
                if (i < xhtmls.Count)
                {
                    Log.log("[Warn]Not all xhtmls are refered in spine.");
                    for (; i < xhtmls.Count; i++)
                    {

                        XmlElement item = manifest.CreateElement("item");
                        item.SetAttribute("href", "Text/" + xhtml_names[i]);
                        item.SetAttribute("id", xhtml_names[i]);
                        item.SetAttribute("media-type", "application/xhtml+xml");
                        if (ContainsSVG(xhtmls[i]))
                        {
                            item.SetAttribute("properties", "svg");
                        }
                        mani_root.AppendChild(item);

                        XmlElement itemref = azw3.resc.spine.CreateElement("itemref");
                        itemref.SetAttribute("idref", xhtml_names[i]);
                        itemref.SetAttribute("linear", "yes");
                        azw3.resc.spine.FirstChild.AppendChild(itemref);
                        Log.log("[Warn]Added " + xhtml_names[i] + " to spine and item");

                    }
                }

                foreach (string imgname in img_names)
                {

                    string ext = Path.GetExtension(imgname).ToLower().Substring(1);
                    if (ext == "jpg") ext = "jpeg";
                    XmlElement item = manifest.CreateElement("item");
                    item.SetAttribute("href", "Images/" + imgname);
                    if (imgname == cover_name) { item.SetAttribute("properties", "cover-image"); }
                    item.SetAttribute("id", Path.GetFileNameWithoutExtension(imgname));
                    item.SetAttribute("media-type", "image/" + ext);
                    mani_root.AppendChild(item);
                }
                foreach (string cssname in css_names)
                {
                    XmlElement item = manifest.CreateElement("item");
                    item.SetAttribute("href", "Styles/" + cssname);
                    item.SetAttribute("id", Path.GetFileNameWithoutExtension(cssname));
                    item.SetAttribute("media-type", "text/css");
                    mani_root.AppendChild(item);
                }
                foreach (string fontname in font_names)
                {
                    XmlElement item = manifest.CreateElement("item");
                    item.SetAttribute("href", "Fonts/" + fontname);
                    item.SetAttribute("id", Path.GetFileNameWithoutExtension(fontname));
                    string mediaType = "";
                    switch (Path.GetExtension(fontname))
                    {
                        case ".ttf": mediaType = "application/font-sfnt"; break;
                        case ".otf": mediaType = "application/font-sfnt"; break;
                    }
                    item.SetAttribute("media-type", mediaType);
                    mani_root.AppendChild(item);
                }
                {
                    XmlElement item = manifest.CreateElement("item");
                    item.SetAttribute("href", "toc.ncx");
                    item.SetAttribute("id", "ncxuks");
                    item.SetAttribute("media-type", "application/x-dtbncx+xml");
                    mani_root.AppendChild(item);
                }
                {
                    XmlElement item = manifest.CreateElement("item");
                    item.SetAttribute("href", "nav.xhtml");
                    item.SetAttribute("id", "navuks");
                    item.SetAttribute("media-type", "application/xhtml+xml");
                    item.SetAttribute("properties", "nav");
                    mani_root.AppendChild(item);
                }
                t = t.Replace("{❕manifest}", manifest.OuterXml.Replace("><", ">\r\n<"));

                XmlDocument meta = new XmlDocument();
                meta.AppendChild(meta.CreateElement("metadata"));
                ((XmlElement)meta.FirstChild).SetAttribute("xmlns:dc", "http://purl");

                {
                    XmlElement x = meta.CreateElement("dc:title");
                    x.InnerText = azw3.title;
                    x.SetAttribute("id", "title");
                    meta.FirstChild.AppendChild(x);
                    if (azw3.mobi_header.extMeta.id_string.ContainsKey(508))
                    {
                        string z = azw3.mobi_header.extMeta.id_string[508];
                        XmlElement x2 = meta.CreateElement("meta");
                        x2.InnerText = z;
                        x2.SetAttribute("refines", "#title");
                        x2.SetAttribute("property", "file-as");
                        meta.FirstChild.AppendChild(x2);
                    }

                }

                {
                    string lang = azw3.mobi_header.extMeta.id_string[524];
                    XmlElement x = meta.CreateElement("dc:language");
                    x.InnerXml = lang;
                    meta.FirstChild.AppendChild(x);
                }
                {
                    XmlElement x = meta.CreateElement("dc:identifier");
                    x.SetAttribute("id", "ASIN");
                    //x.SetAttribute("opf:scheme", "ASIN");
                    string z = azw3.mobi_header.extMeta.id_string[504];
                    x.InnerXml = z;
                    meta.FirstChild.AppendChild(x);
                    XmlElement xd = meta.CreateElement("meta");
                    xd.SetAttribute("property", "dcterms:modified");
                    xd.InnerText = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssZ");
                    meta.FirstChild.AppendChild(xd);
                }
                if (azw3.mobi_header.extMeta.id_string.ContainsKey(100))
                {
                    string[] creatername = azw3.mobi_header.extMeta.id_string[100].Split('&');
                    string[] sortname = new string[0];
                    if (azw3.mobi_header.extMeta.id_string.ContainsKey(517))
                        sortname = azw3.mobi_header.extMeta.id_string[517].Split('&');
                    for (int l = 0; l < creatername.Length; l++)
                    {
                        XmlElement x = meta.CreateElement("dc:creator");
                        x.InnerText = creatername[l];
                        x.SetAttribute("id", "creator" + l);
                        meta.FirstChild.AppendChild(x);

                        if (l < sortname.Length)
                        {
                            XmlElement x2 = meta.CreateElement("meta");
                            x2.InnerText = sortname[l];
                            x2.SetAttribute("refines", "#creator" + l);
                            x2.SetAttribute("property", "file-as");
                            meta.FirstChild.AppendChild(x2);
                        }

                    }

                }
                if (azw3.mobi_header.extMeta.id_string.ContainsKey(101))
                {
                    XmlElement x = meta.CreateElement("dc:publisher");
                    x.SetAttribute("id", "publisher");
                    x.InnerText = azw3.mobi_header.extMeta.id_string[101];
                    meta.FirstChild.AppendChild(x);
                    if (azw3.mobi_header.extMeta.id_string.ContainsKey(522))
                    {
                        string fileas = azw3.mobi_header.extMeta.id_string[522];
                        XmlElement x2 = meta.CreateElement("meta");
                        x2.InnerText = fileas;
                        x2.SetAttribute("refines", "#publisher");
                        x2.SetAttribute("property", "file-as");
                        meta.FirstChild.AppendChild(x2);
                    }
                }
                if (azw3.mobi_header.extMeta.id_string.ContainsKey(106))
                {
                    XmlElement x = meta.CreateElement("dc:date");
                    string date = azw3.mobi_header.extMeta.id_string[106];
                    //x.SetAttribute("opf:event", "publication");
                    x.InnerText = date;
                    meta.FirstChild.AppendChild(x);
                }


                //fixed layout
                if (azw3.mobi_header.extMeta.id_string.ContainsKey(122))
                {
                    string v = azw3.mobi_header.extMeta.id_string[122];
                    if (v == "true")
                    {
                        //<meta property="rendition:layout">pre-paginated</meta>
                        //<meta property="rendition:orientation">auto</meta>
                        //<meta property="rendition:spread">landscape</meta>
                        XmlElement x;
                        x = meta.CreateElement("meta");
                        x.SetAttribute("property", "rendition:layout");
                        x.InnerText = "pre-paginated";
                        meta.FirstChild.AppendChild(x);
                        x = meta.CreateElement("meta");
                        x.SetAttribute("property", "rendition:orientation");
                        x.InnerText = "auto";
                        meta.FirstChild.AppendChild(x);
                        x = meta.CreateElement("meta");
                        x.SetAttribute("property", "rendition:spread");
                        x.InnerText = "landscape";
                        meta.FirstChild.AppendChild(x);

                    }
                }

                //normal meta
                var normalMetas = new (uint, string)[] {
                     ( 525, "primary-writing-mode" ),
                     ( 123, "book-type" ),
                     ( 124, "orientation-lock"),
                     ( 126, "original-resolution" )
                  };
                foreach (var nm in normalMetas)
                {
                    if (azw3.mobi_header.extMeta.id_string.ContainsKey(nm.Item1))
                    {
                        string v = azw3.mobi_header.extMeta.id_string[nm.Item1];
                        {
                            XmlElement x = meta.CreateElement("meta");
                            x.SetAttribute("name", nm.Item2);
                            x.SetAttribute("content", v);
                            meta.FirstChild.AppendChild(x);
                        }
                    }
                }
                if (fonts.Count > 0)
                {
                    XmlElement x = meta.CreateElement("meta");
                    x.SetAttribute("name", "ibooks:specified-fonts");
                    x.SetAttribute("content", "true");
                    meta.FirstChild.AppendChild(x);
                }

                {
                    string metaTemplate = "<meta name=\"{0}\" content=\"{1}\" />\n";
                    string tempstr = "";
                    if (azw3.mobi_header.extMeta.id_string.ContainsKey(503))
                    { tempstr += string.Format(metaTemplate, IdMapping.id_map_strings[503], azw3.mobi_header.extMeta.id_string[503]); }
                    t = t.Replace("{❕othermeta}", tempstr);
                }

                t = t.Replace("{❕meta}", Util.GetInnerXML((XmlElement)meta.FirstChild));
                //string metas = azw3.resc.metadata.OuterXml;
                ((XmlElement)(azw3.resc.spine.FirstChild)).SetAttribute("toc", "ncxuks"); ;
                string spine = azw3.resc.spine.OuterXml;
                t = t.Replace("{❕spine}", spine.Replace("><", ">\n<"));
                t = t.Replace("{❕version}", Version.version);

                opf = t;
            }
            else
            {
                throw new UnpackKindleSException("no resc info!");
            }
        }

        bool ContainsSVG(XmlDocument d)
        {
            var es = d.GetElementsByTagName("svg");
            if (es.Count == 0) return false;
            return true;
        }


        string ImageName(int resid, Image_Section section)
        {
            return "embed" + Util.Number(resid) + section.ext;
        }
        public static string ImageNameHD(int resid, CRES_Section section)
        {
            return "embed" + Util.Number(resid) + "_HD" + section.ext;
        }
        string AddImage(int id, string message = "")
        {
            string name = null; byte[] data = null;
            Section toComment = null;
            if (azw6 != null)
            {
                int r = azw6.image_sections.Find(s => s == (id + 1));
                if (r != 0)
                {
                    CRES_Section sec = (CRES_Section)azw6.sections[r];
                    name = ImageNameHD(id, sec);
                    data = sec.img;
                    sec.comment = name;
                    toComment = azw3.sections[azw3.mobi_header.first_res_index + id];

                }
            }
            if (name == null)
            {
                Image_Section section = (Image_Section)azw3.sections[azw3.mobi_header.first_res_index + id];
                name = ImageName(id, section);
                data = section.raw;
                toComment = section;
            }

            if (img_names.Find(s => s == name) != null) return name;
            imgs.Add(data);
            img_names.Add(name);
            toComment.comment = name + " | " + message;
            return name;
        }
        const string xhtml11doctype = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.1//EN\" \"http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd\">\n";
        XmlDocument LoadXhtml(string xhtml)
        {
            int htmlstart = xhtml.IndexOf("<html", 0, StringComparison.OrdinalIgnoreCase);
            xhtml = xhtml11doctype + xhtml.Substring(htmlstart);
            XmlDocument d = new XmlDocument();
            using (var rdr = new XmlTextReader(new StringReader(xhtml)))
            {
                d.PreserveWhitespace = true;
                rdr.DtdProcessing = DtdProcessing.Parse;
                rdr.XmlResolver = new XhtmlEntityResolver();
                d.Load(rdr);
            }
            return d;
        }

    }

    class XhtmlEntityResolver : XmlResolver
    {
        static byte[] dtd = File.ReadAllBytes("Xhtml-Entity-Set.dtd");
        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            return new MemoryStream(dtd);
        }
    }
}