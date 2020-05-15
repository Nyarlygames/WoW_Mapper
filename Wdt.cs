using System;
using System.Collections.Generic;
using System.Text;
using CASCLib;
using System.IO;

namespace CascStuff
{
    class Wdt
    {
        public CASCHandler cs;
        public int wdt_id;
        public bool[,] wdt_claimed_tiles = new bool[64, 64];
        public bool[,] all_claimed_tiles = new bool[64, 64];
        public bool wmo_only = false;
        public Toolbox.IngameObject wmo = null;
        public Dictionary<uint, string> listfile;
        public Dictionary<string, uint> filelist;
        List<string> filenameswmo = new List<string>();
        public WDT_MAID maid_chunk = new WDT_MAID();
        public Toolbox.UnrefDef[,] unref = new Toolbox.UnrefDef[64,64];
        public bool found_unref =false;
        public int min_x = 64;
        public int min_y = 64;
        public int max_x = -1;
        public int max_y = -1;
        public int size_x = 1;
        public int size_y = 1;
        public int minNative = 10;


        public Wdt(int wdt_name, CASCHandler cascH, Dictionary<uint, string> lf,  Dictionary<string, uint> fl, Dictionary<uint, List<Toolbox.IngameObject>> IG_Obj)
        {
            wdt_id = wdt_name;
            cs = cascH;
            listfile = lf;
            filelist = fl;
            ParseWdt(IG_Obj);
        }

        public void LoadExtra()
        {
            Console.WriteLine("entering extra");
            string base_str = listfile[(uint)wdt_id].Substring(0, listfile[(uint)wdt_id] .Length-4);
            string mapname = listfile[Convert.ToUInt32(wdt_id)].Substring(listfile[Convert.ToUInt32(wdt_id)].LastIndexOf("/") + 1, listfile[Convert.ToUInt32(wdt_id)].Length - 4 - (listfile[Convert.ToUInt32(wdt_id)].LastIndexOf("/") + 1));
            
            for (int i = 0; i < 64; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    if (wdt_claimed_tiles[i,j] == false)
                    {
                        string adt = base_str + "_" + i + "_" + j + ".adt";
                        string obj0 = base_str + "_" + i + "_" + j + "_obj0.adt";
                        string obj1 = base_str + "_" + i + "_" + j + "_obj1.adt";
                        string minit = Path.Combine("world", "minimaps", mapname, String.Format("map{0:00}_{1:00}.blp", i, j));
                        
                        unref[i,j] = new Toolbox.UnrefDef();
                        unref[i,j].adtname = 0;
                        unref[i,j].obj0name = 0;
                        unref[i,j].obj1name = 0;
                        unref[i,j].minit = 0;
                        uint val = 0;
                        if (filelist.TryGetValue(adt, out val))
                            unref[i,j].adtname = val;
                        if (filelist.TryGetValue(obj0, out val))
                            unref[i,j].obj0name = val;
                        if (filelist.TryGetValue(obj1, out val))
                            unref[i,j].obj1name = val;
                        if (filelist.TryGetValue(minit, out val))
                            unref[i,j].minit = val;
                         //= Toolbox.GetKeysFromListfile(adt, obj0, obj1, listfile);
                        unref[i, j].x = i;
                        unref[i, j].y = j;
                        //Console.WriteLine(unref[i,j].adtname + " / " + unref[i,j].obj0name + " / " + unref[i,j].obj1name + " / " + unref[i,j].minit);
                        //unref[i, j].minit = Toolbox.GetKeyFromListfile(minit, listfile);
                        if ((unref[i, j].minit==0) && (unref[i, j].adtname==0) && (unref[i, j].obj0name==0) && (unref[i, j].obj1name) == 0){
                            unref[i,j] = null;
                        }
                        else {
                            //Console.WriteLine("adding adt " + unref[i,j].adtname + " at " + i + " / " + j + " | from wdt :  " + maid_chunk.root[i,j]);
                            //Console.WriteLine("adding mini " + unref[i,j].minit + " at " + i + " / " + j + " | from wdt :  " + maid_chunk.minit[i,j]);

                            if ((unref[i,j].adtname != 0) && (maid_chunk.root[j,i] ==0)) 
                            {
                                unref[i, j].exists_adtname = cs.FileExists(unref[i, j].adtname);
                                maid_chunk.root[j,i] = unref[i,j].adtname;
                                //Console.WriteLine("adding unrefroot : " +i + " / " +j);
                                all_claimed_tiles[i,j] = true;
                            }
                            if ((unref[i, j].obj0name != 0) && (maid_chunk.obj0[j,i] ==0)){
                                maid_chunk.obj0[j,i] = unref[i,j].obj0name;
                                unref[i, j].exists_obj0name = cs.FileExists((int) unref[i, j].obj0name);
                                //Console.WriteLine("adding unrefobj0 : " +i + " / " +j);
                                all_claimed_tiles[i,j] = true;
                            }
                            if ((unref[i, j].obj1name != 0) && (maid_chunk.obj1[j,i] ==0))  {
                                maid_chunk.obj1[j,i] = unref[i,j].obj1name;
                                unref[i, j].exists_obj1name = cs.FileExists((int) unref[i, j].obj1name);
                                //Console.WriteLine("adding unrefobj1 : " +i + " / " +j);
                                all_claimed_tiles[i,j] = true;
                            }
                            if ((unref[i, j].minit != 0) && (maid_chunk.minit[j,i] ==0)) {
                                unref[i, j].exists_minit = cs.FileExists((int) unref[i, j].minit);
                                //Console.WriteLine("adding unrefminit : " +i + " / " +j);
                                maid_chunk.minit[j,i] = unref[i,j].minit;
                                all_claimed_tiles[i,j] = true;
                            }
                            found_unref = true;
                        }
                    }
                }
            }
            Console.WriteLine("leaving extra");
        }

        public void ParseWdt(Dictionary<uint, List<Toolbox.IngameObject>> IG_Obj)
        {
            //Console.WriteLine("Parsing wdt " + wdt_id);
            if (cs.FileExists(wdt_id))
            {
                using (Stream stream = cs.OpenFile(wdt_id))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        while (reader.BaseStream.Position != reader.BaseStream.Length)
                        {
                            var magic = reader.ReadUInt32();
                            var size = reader.ReadUInt32();
                            var pos = reader.BaseStream.Position;

                            if (magic == Toolbox.mk("MPHD"))
                            {
                                var flags = reader.ReadUInt32();
                                if ((flags & 1) == 1)
                                    wmo_only = true;
                            }
                            if (magic == Toolbox.mk("MAIN"))
                            {
                                for (int x = 0; x < 64; ++x)
                                {
                                    for (int y = 0; y < 64; ++y)
                                    {
                                        wdt_claimed_tiles[y, x] = (reader.ReadUInt32() & 1) == 1;
                                        all_claimed_tiles[y, x] =  wdt_claimed_tiles[y, x];
                                        var asyncid = reader.ReadUInt32();
                                    }
                                }
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
                            
                            if (magic == Toolbox.mk("MODF"))
                            {
                                wmo = Toolbox.MakeObject(reader, 2, listfile, 1, 1, null, filenameswmo, filelist, IG_Obj);
                            }

                            if (magic == Toolbox.mk("MAID"))
                            {
                                for (int i = 0; i < 64; i++)
                                {
                                    for (int j = 0; j < 64; j++)
                                    {
                                        maid_chunk.root[i, j] = reader.ReadUInt32();
                                        maid_chunk.obj0[i, j] = reader.ReadUInt32();
                                        maid_chunk.obj1[i, j] = reader.ReadUInt32();
                                        maid_chunk.tex0[i, j] = reader.ReadUInt32();
                                        maid_chunk.loadadt[i, j] = reader.ReadUInt32();
                                        maid_chunk.mapt[i, j] = reader.ReadUInt32();
                                        maid_chunk.maptn[i, j] = reader.ReadUInt32();
                                        maid_chunk.minit[i, j] = reader.ReadUInt32();
                                    }
                                }
                            }
                            reader.BaseStream.Position = pos + size;
                        }
                    }
                }
            }
            else
            {
                //Console.WriteLine("Wdt not found : " + wdt_id);
            }
        }

        public class WDT_MAID
        {
            public uint[,] root { get; set; }
            public uint[,] obj0 { get; set; }
            public uint[,] obj1 { get; set; }
            public uint[,] tex0 { get; set; }
            public uint[,] loadadt { get; set; }
            public uint[,] mapt { get; set; }
            public uint[,] maptn { get; set; }
            public uint[,] minit { get; set; }

            public WDT_MAID()
            {
                root = new uint[64, 64];
                obj0 = new uint[64, 64];
                obj1 = new uint[64, 64];
                tex0 = new uint[64, 64];
                loadadt = new uint[64, 64];
                mapt = new uint[64, 64];
                maptn = new uint[64, 64];
                minit = new uint[64, 64];
            }
        }
    }
}
