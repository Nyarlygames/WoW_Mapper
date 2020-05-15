using System;
using CASCLib;
using System.Collections.Generic;
using System.Linq;
using SereniaBLPLib;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CsvHelper;

namespace CascStuff
{
    class Program
    {
        static public string version_build ="";
        static Dictionary<uint, string> listfile = new Dictionary<uint, string>();
        static Dictionary<string, uint> filelist = new Dictionary<string, uint>();
        static Dictionary<string, int> wdt_from_list = new Dictionary<string, int>();
        static List<Toolbox.CsvMap> listmap = new List<Toolbox.CsvMap>();
        static List<Toolbox.CsvArea> listarea = new List<Toolbox.CsvArea>();
        static Dictionary<uint, List<Toolbox.IngameObject>> IG_Obj = new Dictionary<uint, List<Toolbox.IngameObject>>();

        static void Main(string[] args)
        {
            string product = args[0];
            var list = args.ToList();
            list.RemoveAt(0);
            args = list.ToArray();
            listfile = Toolbox.Listfile();
            filelist = Toolbox.Filelist(wdt_from_list);
            listmap = Toolbox.Listmaps();
            listarea = Toolbox.Listareas();

            int[] arg_arr = listmap.Where(x => x.WdtFileDataID != 0).Select(x => x.WdtFileDataID).ToList().Union(wdt_from_list.Select(x => x.Value).ToList()).ToArray();
           //int[] arg_arr = new int[1];
           //arg_arr[0] = Convert.ToInt32(args[0]);
            Console.WriteLine("map count : " + arg_arr.Length);
            CASCHandler cs = CascConnect("offline", product);
            string output_dir = "../";
            List<string> Maps = new List<string>();
            var versionjson = output_dir+ @"versions.json";

            List<Toolbox.VersionDef> versions = new List<Toolbox.VersionDef>();
            List<Toolbox.VersionDef> newVersions = GetNewVersions(cs, out versions, output_dir, product, arg_arr);
        
            /* DEBUG */
            /*Toolbox.VersionDef Debug = new Toolbox.VersionDef();
            Debug.map = new Toolbox.MapDef();
            Debug.map.WdtFileDataID = Convert.ToInt32(args[0]);
            Debug.map.MapName_lang = "debug";
            newVersions.Add(Debug);*/
            
            /*foreach(int i in arg_arr){
                Toolbox.VersionDef Debug = new Toolbox.VersionDef();
                Debug.map = new Toolbox.MapDef();
                Debug.map.WdtFileDataID = i;
                Debug.map.MapName_lang = ""+i;
                newVersions.Add(Debug);
            }*/

            foreach(Toolbox.VersionDef ver in newVersions){
                if (cs.FileExists(ver.map.WdtFileDataID)) {
                    IG_Obj.Clear();
                    Wdt wdt = new Wdt(ver.map.WdtFileDataID, cs, listfile, filelist, IG_Obj);
                    Console.WriteLine("---- parsing " + ver.map.MapName_lang);
                    string map_dir = output_dir + wdt.wdt_id + "/" + version_build;
                   /* if (Directory.Exists(map_dir))
                        Directory.Delete(map_dir, true);*/
                    Directory.CreateDirectory(map_dir);
                    Adt[,] adts = new Adt[64, 64];
                    ObjAdt[,] obj0 = new ObjAdt[64, 64];
                    ObjAdt[,] obj1 = new ObjAdt[64, 64];
                    Dictionary<int, List<Toolbox.Area>> Areas = new Dictionary<int, List<Toolbox.Area>>();
                    wdt.LoadExtra();
                    
                    if ((wdt.wmo_only == true) && (wdt.found_unref == false)) {
                        Mapper.Make_Models(map_dir, wdt, adts, obj0, obj1);
                    }
                    else {
                        for (int i = 0; i < 64; i++)
                        {
                            for (int j = 0; j < 64; j++)
                            {

                                adts[i, j] = new Adt(cs, i, j, listfile, listarea, filelist);
                                adts[i, j].Parse((int)wdt.maid_chunk.root[i, j], IG_Obj);
                                foreach (Toolbox.Area area in adts[i,j].areas) {
                                    if (!Areas.ContainsKey(area.ID))
                                        Areas.Add(area.ID, new List<Toolbox.Area>());
                                    Areas[area.ID].Add(area);
                                }
                                obj0[i, j] = new ObjAdt(cs, i, j, listfile, filelist);
                                obj0[i, j].Parse((int)wdt.maid_chunk.obj0[i, j], IG_Obj);

                                obj1[i, j] = new ObjAdt(cs, i, j, listfile, filelist);
                                obj1[i, j].Parse((int)wdt.maid_chunk.obj1[i, j], IG_Obj);
                                Mapper.CreateModelsJson(IG_Obj,map_dir,wdt, i, j);
                                IG_Obj.Clear();
                            }
                        }
                        Mapper.Make_ZoomMap(wdt, map_dir, cs);
                        if ((wdt.size_x >= 0) && (wdt.size_y >= 0)) {
                            Console.WriteLine(wdt.size_x + " / "+  wdt.size_y + " / native " + wdt.minNative);
                            Mapper.Make_UnrefMap(map_dir, wdt.unref, wdt);
                            Mapper.Make_ImpassMap(map_dir, adts, wdt);
                            Mapper.Make_UnknownMap(map_dir, adts, wdt);
                            Mapper.Make_AreaMap(map_dir, Areas, wdt);
                            Mapper.Make_WdtBorders(map_dir, wdt);
                            Mapper.Make_DeathMap(map_dir, adts, wdt);
                            
                        }
                        string wdt_info = "{\"min_x\":\"" + wdt.min_x + "\",\"min_y\":\"" + wdt.min_y + "\",\"size_x\":\"" + wdt.size_x + "\",\"size_y\":\"" + wdt.size_y + "\",\"NativeZoom\":\"" + wdt.minNative + "\"}";
                        File.WriteAllText(map_dir + "/wdt_info.json", wdt_info);
                    }
                    if ((wdt.size_x < 0) && (wdt.size_y < 0) && (wdt.wmo_only == false)) {
                        Console.WriteLine("bad wdt removing " + ver.map.WdtFileDataID);
                        versions.Remove(ver);
                    }
                    else {
                        Console.WriteLine("Added " + ver.map.WdtFileDataID + " to versions.json");
                        List<Toolbox.VersionDef> during = new List<Toolbox.VersionDef>();
                        var duringfile = output_dir+ @"versions.json";
                        using (StreamReader r = new StreamReader(duringfile))
                        {
                            string jsonduring = r.ReadToEnd();
                            during = JsonConvert.DeserializeObject<List<Toolbox.VersionDef>>(jsonduring);
                        }
                        during.Add(ver);
                        string jsonduringout = JsonConvert.SerializeObject(during.ToArray());
                        System.IO.File.WriteAllText(duringfile, jsonduringout);
                    }
                }
                else {
                    Console.WriteLine("removing " + ver.map.WdtFileDataID);
                    versions.Remove(ver);
                }
            }
           /* string jsonverout = JsonConvert.SerializeObject(versions.ToArray());
            System.IO.File.WriteAllText(versionjson, jsonverout);*/
        }


        /*static void CheckForObj()
        {
            var test = fullobj.FindAll(e => e.filedataid == 201309);
            if (test.Count> 0)
                Console.WriteLine("Found " + test[0].name + " in " + csvmap.Directory +" count : " + test.Count);
        }*/

        static List<Toolbox.VersionDef> GetNewVersions(CASCHandler cs, out List<Toolbox.VersionDef> versions, string output_dir, string product, int[] args) {
            List<Toolbox.VersionDef> newVersions = new List<Toolbox.VersionDef>();
            var versionjson = output_dir+ @"versions.json";
            using (StreamReader r = new StreamReader(versionjson))
            {
                string json = r.ReadToEnd();
                versions = JsonConvert.DeserializeObject<List<Toolbox.VersionDef>>(json);
            }
            foreach(int wdtname in args){
                Toolbox.CsvMap csvdefault = listmap.Find(e => e.WdtFileDataID == wdtname);
                Toolbox.MapDef knownmap = new Toolbox.MapDef();
                if (csvdefault == null) {
                    knownmap.product = product;
                    knownmap.ID = -1;
                    knownmap.Directory = "UnknownDir";
                    knownmap.MapName_lang = "UnknownName";
                    knownmap.WdtFileDataID = wdtname;
                }
                else {
                    knownmap.product = product;
                    knownmap.ID = csvdefault.ID;
                    knownmap.Directory = csvdefault.Directory;
                    knownmap.MapName_lang = csvdefault.MapName_lang;
                    knownmap.WdtFileDataID = csvdefault.WdtFileDataID;
                }
                bool contains = versions.Any(e => e.map.WdtFileDataID == wdtname);
                if ((wdtname > 0) && (!contains)) {
                    Toolbox.VersionDef newVer = new Toolbox.VersionDef();
                    newVer.map = new Toolbox.MapDef();
                    newVer.map.ID = knownmap.ID;
                    newVer.map.Directory = knownmap.Directory;
                    newVer.map.MapName_lang = knownmap.MapName_lang;
                    newVer.map.WdtFileDataID = wdtname;
                    newVer.map.product = product;
                    newVer.BuildId = cs.Config.BuildName;
                    versions.Add(newVer);
                    newVersions.Add(newVer);
                    Console.WriteLine("adding new map : " + newVer.map.WdtFileDataID);
                }
                else if (wdtname > 0){
                    bool containsbuild = versions.Any(e => e.map.WdtFileDataID == wdtname && e.BuildId == cs.Config.BuildName);
                    if (!containsbuild){
                        Toolbox.VersionDef newVer = new Toolbox.VersionDef();
                        newVer.map = new Toolbox.MapDef();
                        newVer.map.ID = knownmap.ID;
                        newVer.map.Directory = knownmap.Directory;
                        newVer.map.MapName_lang = knownmap.MapName_lang;
                        newVer.map.WdtFileDataID = wdtname;
                        newVer.map.product = product;
                        newVer.BuildId = cs.Config.BuildName;
                        versions.Add(newVer);
                        newVersions.Add(newVer);
                        Console.WriteLine("adding new build : " + newVer.BuildId +  " / " + newVer.map.WdtFileDataID);
                    }
                }
            }
            return newVersions;
        }

        static CASCHandler CascConnect(string method, string product)
        {
            CASCHandler cascHandler = null;
            if (method == "online")
            {
                cascHandler = CASCHandler.OpenOnlineStorage(product, "us");
                cascHandler.Root.SetFlags(LocaleFlags.All_WoW, true, true);
                version_build = cascHandler.Config.BuildName;
                Console.WriteLine(cascHandler.Config.BuildName);
            }
            else if (method == "offline")
            {
                Console.WriteLine("Connecting to casc");
                if (product == "wow")
                    cascHandler = CASCHandler.OpenLocalStorage(@"/home/nyarly/WoW/");
                else if (product == "wow_beta")
                    cascHandler = CASCHandler.OpenLocalStorage(@"/home/nyarly/WoW_beta/");
                else if (product == "wow_classic")
                    cascHandler = CASCHandler.OpenLocalStorage(@"/home/nyarly/WoW_classic/");
                cascHandler.Root.SetFlags(LocaleFlags.All_WoW, true, true);
                version_build = cascHandler.Config.BuildName;
                Console.WriteLine("Wow build : " + cascHandler.Config.BuildName);
            }
            else
            {
                Console.WriteLine("failed casc method");
            }
            return cascHandler;
        }
    }
}
