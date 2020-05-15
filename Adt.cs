using System;
using System.Collections.Generic;
using System.Text;
using CASCLib;
using System.IO;

namespace CascStuff
{
    class Adt
    {
        public CASCHandler cs;
        public bool adt_claims_tile = false;
        public List<Toolbox.IngameObject> objects = new List<Toolbox.IngameObject>();
        public int x = 0;
        public int y = 0;
        public Dictionary<uint, string> listfile;
        public Dictionary<string, uint> filelist;
        public int[,] impasses = new int[16, 16];
        public int[,] unknown = new int[16, 16];
        public List<Toolbox.Area> areas = new List<Toolbox.Area>();
        public List<Toolbox.CsvArea> CsvAreas = new List<Toolbox.CsvArea>();
        public Toolbox.plane plane_min = new Toolbox.plane();
        public Toolbox.plane plane_max = new Toolbox.plane();
        List<string> filenameswmo = new List<string>();
        List<string> filenamesm2 = new List<string>();
        List<uint> mmid = new List<uint>();
        List<uint> mwid = new List<uint>();

        public Adt(CASCHandler cascH, int adt_x, int adt_y, Dictionary<uint, string> lf, List<Toolbox.CsvArea> listareas,Dictionary<string, uint> fl)
        {
            cs = cascH;
            x = adt_x;
            y = adt_y;
            listfile = lf;
            filelist = fl;
            CsvAreas= listareas;
        }

        public void Parse(int adt_name, Dictionary<uint, List<Toolbox.IngameObject>> IG_Obj)
        {
            if ((adt_name > 0) && cs.FileExists(adt_name))
            {
                //Console.WriteLine("parsing " + adt_name + " : " +x + " / " + y);
                using (Stream stream = cs.OpenFile(adt_name))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        while (reader.BaseStream.Position != reader.BaseStream.Length)
                        {
                            var magic = reader.ReadUInt32();
                            var size = reader.ReadUInt32();
                            var pos = reader.BaseStream.Position;

                            if (magic == Toolbox.mk("MFBO"))
                            {
                                plane_max.a = new short[3];
                                plane_max.a[0] = reader.ReadInt16();
                                plane_max.a[1] = reader.ReadInt16();
                                plane_max.a[2] = reader.ReadInt16();
                                plane_max.b = new short[3];
                                plane_max.b[0] = reader.ReadInt16();
                                plane_max.b[1] = reader.ReadInt16();
                                plane_max.b[2] = reader.ReadInt16();
                                plane_max.c = new short[3];
                                plane_max.c[0] = reader.ReadInt16();
                                plane_max.c[1] = reader.ReadInt16();
                                plane_max.c[2] = reader.ReadInt16();

                                plane_min.a = new short[3];
                                plane_min.a[0] = reader.ReadInt16();
                                plane_min.a[1] = reader.ReadInt16();
                                plane_min.a[2] = reader.ReadInt16();
                                plane_min.b = new short[3];
                                plane_min.b[0] = reader.ReadInt16();
                                plane_min.b[1] = reader.ReadInt16();
                                plane_min.b[2] = reader.ReadInt16();
                                plane_min.c = new short[3];
                                plane_min.c[0] = reader.ReadInt16();
                                plane_min.c[1] = reader.ReadInt16();
                                plane_min.c[2] = reader.ReadInt16();
                            }
                            if (magic == Toolbox.mk("MCNK"))
                            {
                                var flags = reader.ReadUInt32();
                                var sub_x = reader.ReadUInt32();
                                var sub_y = reader.ReadUInt32();
                                var nLayers = reader.ReadUInt32();
                                var nDoodadRefs = reader.ReadUInt32();
                                var holes_high_res = reader.ReadUInt64();
                                var ofsLayer = reader.ReadUInt32();
                                var ofsRefs = reader.ReadUInt32();
                                var ofsAlpha = reader.ReadUInt32();
                                var sizeAlpha = reader.ReadUInt32();
                                var ofsShadow = reader.ReadUInt32();
                                var sizeShadow = reader.ReadUInt32();
                                var areaid = reader.ReadUInt32();
                                impasses[sub_x, sub_y] = -1;
                                if (areaid == 0) {
                                    unknown[sub_x, sub_y] = 1;
                                }
                                else {
                                    Toolbox.CsvArea csvdefault = CsvAreas.Find(e => e.ID == (int)areaid);
                                    Toolbox.Area area = new Toolbox.Area();
                                    area.x = x;
                                    area.y = y;
                                    area.sub_x = sub_x;
                                    area.sub_y = sub_y;
                                    area.ID = (int) areaid;
                                    if (csvdefault != null) {
                                        area.ZoneName = csvdefault.ZoneName;
                                        area.AreaName_lang = csvdefault.AreaName_lang;
                                        area.ContinentID = csvdefault.ContinentID;
                                    }
                                    areas.Add(area);
                                }
                                if ((flags & 2) == 2)
                                    impasses[sub_x, sub_y] = 1;
                            }

                            if (magic == Toolbox.mk("MWMO")) // wmo filenames
                            {
                                char c;
                                int i = (int)size;
                                while (i > 0)
                                {
                                    StringBuilder sb = new StringBuilder();
                                    while ((c = Convert.ToChar(reader.ReadByte())) != '\0')
                                    {
                                        sb.Append(c);
                                        i--;
                                    }
                                    i--;
                                    if (!filenameswmo.Contains(sb.ToString()))
                                    {
                                        filenameswmo.Add(sb.ToString());
                                    }
                                }
                            }
                            if (magic == Toolbox.mk("MMDX")) // m2 filenames
                            {
                                char c;
                                int i = (int)size;
                                while (i > 0)
                                {
                                    StringBuilder sb = new StringBuilder();
                                    while ((c = Convert.ToChar(reader.ReadByte())) != '\0')
                                    {
                                        sb.Append(c);
                                        i--;
                                    }
                                    i--;
                                    if (!filenamesm2.Contains(sb.ToString()))
                                    {
                                        filenamesm2.Add(sb.ToString());
                                    }
                                }
                            }

                            if (magic == Toolbox.mk("MMID")) // m2 offset names
                            {
                                while (reader.BaseStream.Position < pos + size)
                                {
                                    mmid.Add(reader.ReadUInt32());
                                }
                            }
                            if (magic == Toolbox.mk("MWID")) // wmo offset names
                            {
                                while (reader.BaseStream.Position < pos + size)
                                {
                                    mwid.Add(reader.ReadUInt32());
                                }
                            }

                            if (magic == Toolbox.mk("MODF")) // placement WMO
                            {
                                while (reader.BaseStream.Position < pos + size)
                                {
                                    objects.Add(Toolbox.MakeObject(reader, 0, listfile, x, y, mwid, filenameswmo, filelist, IG_Obj));
                                }
                            }
                            if (magic == Toolbox.mk("MDDF")) // placement m2
                            {
                                while (reader.BaseStream.Position < pos + size)
                                {
                                    objects.Add(Toolbox.MakeObject(reader,1, listfile, x, y, mmid, filenamesm2, filelist, IG_Obj));
                                }
                            }

                            reader.BaseStream.Position = pos + size;
                        }
                    }
                }
                adt_claims_tile = true;
            }
        }
    }
}
