using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CsvHelper;

namespace CascStuff
{
    class Toolbox
    {
        static public int tile_size = 512;

        public class Vector3
        {
            public int x { get; set; }
            public int y { get; set; }
            public int z { get; set; }
        }
        public class UnrefDef
        {
            public int x { get; set; }
            public int y { get; set; }
            public uint adtname { get; set; }
            public uint obj0name { get; set; }
            public uint obj1name { get; set; }
            public uint minit { get; set; }
            public bool exists_adtname { get; set; }
            public bool exists_obj0name { get; set; }
            public bool exists_obj1name { get; set; }
            public bool exists_minit { get; set; }
        }
        public class CsvMap
        {
            public int ID { get; set; }
            public string Directory { get; set; }
            public string MapName_lang { get; set; }
            public int WdtFileDataID { get; set; }
        }
        public class CsvArea
        {
            public int ID { get; set; }
            public string ZoneName { get; set; }
            public string AreaName_lang { get; set; }
            public int ContinentID { get; set; }
        }
        public class MapDef
        {
            public string product { get; set; }
            public int ID { get; set; }
            public string Directory { get; set; }
            public string MapName_lang { get; set; }
            public int WdtFileDataID { get; set; }
        }
        public class Area
        {
            public float x { get; set; }
            public float y { get; set; }
            public float sub_x { get; set; }
            public float sub_y { get; set; }
            public int ID { get; set; }
            public string ZoneName{ get; set; }
            public string AreaName_lang { get; set; }
            public int ContinentID { get; set; }
        }
        public class VersionDef
        {
            public MapDef map { get; set; }
            public string BuildId { get; set; }
        }
        public class IngameObject
        {
            public float posx { get; set; }
            public float posy { get; set; }
            public float posz { get; set; }
            public float x { get; set; }
            public float y { get; set; }
            public int type { get; set; }
            public string name { get; set; }
            public uint uniqueId { get; set; }
            public uint filedataid { get; set; }
        }

        public struct plane
        {
            public short[] a;
            public short[] b;
            public short[] c;
        }


        static public UnrefDef GetKeysFromListfile(string adt, string obj0, string ob1, Dictionary<uint, string> listfile)
        {
            UnrefDef res = new UnrefDef();
            res.adtname = 0;
            res.obj0name = 0;
            res.obj1name = 0;
            foreach (KeyValuePair<uint, string> kp in listfile)
            {
                if (adt == kp.Value)
                {
                    res.adtname = kp.Key;
                }
                else if (obj0 == kp.Value)
                {
                    res.obj0name = kp.Key;
                }
                else if (ob1 == kp.Value)
                {
                    res.obj1name = kp.Key;
                }
                if ((res.adtname != 0) && (res.obj0name != 0) && (res.obj1name != 0))
                    break;
            }
            return (res);
        }
        static public uint GetKeyFromListfile(string path, Dictionary<uint, string> listfile)
        {
            uint key = 0;
            foreach (KeyValuePair<uint, string> kp in listfile)
            {
                if (path == kp.Value)
                {
                    key = kp.Key;
                    break;
                }
            }
            return (key);
        }

        public static Dictionary<uint, string> Listfile()
        {
            Dictionary<uint, string> ret = new Dictionary<uint, string>();
            string[] lines = System.IO.File.ReadAllLines(@"listfile.csv");

            foreach (string line in lines)
            {
                ret.Add(Convert.ToUInt32(line.Substring(0, line.IndexOf(";"))), line.Substring(line.IndexOf(";") + 1));
            }
            return ret;
        }
        public static Dictionary<string, uint> Filelist(Dictionary<string, int> wdt_from_list)
        {
            Dictionary<string, uint> ret = new Dictionary<string, uint>();
            string[] lines = System.IO.File.ReadAllLines(@"listfile.csv");

            foreach (string line in lines)
            {
                var filename = line.Substring(line.IndexOf(";") + 1);
                if (ret.ContainsKey(filename))
                    filename += "_2";
                if (filename.Contains(".wdt") && !filename.Contains("_occ.wdt") && !filename.Contains("_lgt.wdt") && !filename.Contains("_fogs.wdt") && !filename.Contains("_mpv.wdt"))
                    wdt_from_list.Add(filename, Convert.ToInt32(line.Substring(0, line.IndexOf(";"))));
                ret.Add(filename,Convert.ToUInt32(line.Substring(0, line.IndexOf(";"))));
            }
            return ret;
        }

        public static List<CsvArea> Listareas()
        {
            List<CsvArea> CsvMaps = new List<CsvArea>();
            using (var reader = new StreamReader(@"areatable.csv"))
            using (var csv = new CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture))
            {
                csv.Configuration.PrepareHeaderForMatch = (string header, int index) => header.ToLower();
                CsvMaps = csv.GetRecords<CsvArea>().ToList();
            }
            return CsvMaps;
        }

        public static List<CsvMap> Listmaps()
        {
            List<CsvMap> CsvMaps = new List<CsvMap>();
            using (var reader = new StreamReader(@"map.csv"))
            using (var csv = new CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture))
            {
                csv.Configuration.PrepareHeaderForMatch = (string header, int index) => header.ToLower();
                CsvMaps = csv.GetRecords<CsvMap>().ToList();
            }
            return CsvMaps;
        }

        public static IngameObject MakeObject(BinaryReader reader, int type, Dictionary<uint, string> listfile, int x, int y, List<uint> offsets, List<string> filenames, Dictionary<string, uint> filelist, Dictionary<uint, List<Toolbox.IngameObject>> IG_Obj)
        {
            // 0 = wmo; 1 = m2; 2 = wmo map
            IngameObject n = new IngameObject();
            float posx =0;
            float posy=0;
            float posz=0;
            uint filedataid=0;
            uint uniqueId =0;
            uint flags =0;
            if (type == 1) {
                filedataid = reader.ReadUInt32();
                uniqueId = reader.ReadUInt32();
                posx = reader.ReadSingle();
                posy = reader.ReadSingle();
                posz = reader.ReadSingle();
                float[] rotation = new float[3];
                rotation[0] = reader.ReadSingle();
                rotation[1] = reader.ReadSingle();
                rotation[2] = reader.ReadSingle();
                ushort scale = reader.ReadUInt16();
                flags = reader.ReadUInt16();
            }
            else {
                filedataid = reader.ReadUInt32();
                uniqueId = reader.ReadUInt32();
                posx = reader.ReadSingle();
                posy = reader.ReadSingle();
                posz = reader.ReadSingle();
                
                float[] rotation = new float[3];
                rotation[0] = reader.ReadSingle();
                rotation[1] = reader.ReadSingle();
                rotation[2] = reader.ReadSingle();
                float[] min = new float[3];
                min[0] = reader.ReadSingle();
                min[1] = reader.ReadSingle();
                min[2] = reader.ReadSingle();
                float[] max = new float[3];
                max[0] = reader.ReadSingle();
                max[1] = reader.ReadSingle();
                max[2] = reader.ReadSingle();
                flags = reader.ReadUInt16();
                ushort doodadSet = reader.ReadUInt16();
                ushort nameSet = reader.ReadUInt16();
                ushort scale = reader.ReadUInt16();
            }
            n.filedataid = filedataid;
            n.uniqueId = uniqueId;
            n.x = x;
            n.y = y;
            n.type = type;
            n.posx = posx / 533 * 256;
            n.posy = posy;
            n.posz = posz / 533 * 256;

            if (type == 1) { // m2
                if ((flags & 64)  == 64){
                    string name = "";
                    if (listfile.TryGetValue(n.filedataid, out name))
                        n.name = name;
                    else
                        n.name = n.filedataid.ToString();
                }
                else {
                    n.name = filenames[(int)filedataid];
                    n.name = n.name.ToLower();
                    n.name = n.name.Replace("\\","/");
                    n.filedataid = filelist[n.name];
                }
            }
            else if (type == 0) { // wmo
                if ((flags & 8)  == 8){
                    string name = "";
                    if (listfile.TryGetValue(n.filedataid, out name))
                        n.name = name;
                    else
                        n.name = n.filedataid.ToString();
                }
                else {
                    n.name = filenames[(int)filedataid];
                    n.name = n.name.ToLower();
                    n.name = n.name.Replace("\\","/");
                    n.filedataid = filelist[n.name];
                }
            }
            else if (type == 2){ //wmo map
                if ((flags & 8) == 8){
                    string name = "";
                    if (listfile.TryGetValue(n.filedataid, out name))
                        n.name = name;
                    else
                        n.name = n.filedataid.ToString();
                }
                else {
                    n.name = filenames[(int)filedataid];
                    n.name = n.name.ToLower();
                    n.name = n.name.Replace("\\","/");
                    n.filedataid = filelist[n.name];
                }
            }
            if (!IG_Obj.ContainsKey(n.filedataid))
                IG_Obj.Add(n.filedataid, new List<Toolbox.IngameObject>());
            IG_Obj[n.filedataid].Add(n);
            
            return (n);
        }

        public static uint mk(string str)
        {
            if (str.Length != 4) throw new Exception("non 4-character magic???");
            return (uint)(str[3]) << 0 | (uint)(str[2]) << 8
                 | (uint)(str[1]) << 16 | (uint)(str[0]) << 24;
        }

    }
}
