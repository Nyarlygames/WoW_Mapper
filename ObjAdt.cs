using System;
using System.Collections.Generic;
using System.Text;
using CASCLib;
using System.IO;

namespace CascStuff
{
    class ObjAdt
    {
        //M2
        //Wmo
        public CASCHandler cs;
        public bool obj_claims_tile = false;
        public List<Toolbox.IngameObject> objects = new List<Toolbox.IngameObject>();
        public int x = 0;
        public int y = 0;
        public Dictionary<uint, string> listfile;
        public Dictionary<string, uint> filelist;
        List<string> filenameswmo = new List<string>();
        List<string> filenamesm2 = new List<string>();
        List<uint> mmid = new List<uint>();
        List<uint> mwid = new List<uint>();
        /*Toolbox.plane plane_min = new Toolbox.plane();
        Toolbox.plane plane_max = new Toolbox.plane();*/

        public ObjAdt(CASCHandler cascH, int adt_x, int adt_y, Dictionary<uint, string> lf, Dictionary<string, uint> fl)
        {
            cs = cascH;
            x = adt_x;
            y = adt_y;
            listfile = lf;
            filelist = fl;
        }

        public void Parse(int adt_name, Dictionary<uint, List<Toolbox.IngameObject>> IG_Obj)
        {
            if ((adt_name > 0) && cs.FileExists(adt_name))
            {
                using (Stream stream = cs.OpenFile(adt_name))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        while (reader.BaseStream.Position != reader.BaseStream.Length)
                        {
                            var magic = reader.ReadUInt32();
                            var size = reader.ReadUInt32();
                            var pos = reader.BaseStream.Position;

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
                                    objects.Add(Toolbox.MakeObject(reader, 1, listfile, x, y, mmid, filenamesm2, filelist, IG_Obj));
                                }
                            }

                            reader.BaseStream.Position = pos + size;
                        }
                    }
                }
                obj_claims_tile = true;
            }
        }
    }
}
