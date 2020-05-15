using System;
using System.Collections.Generic;
using System.Text;
using NetVips;
using NetVips.Extensions;
using CASCLib;
using System.IO;
using SereniaBLPLib;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace CascStuff
{
    class Mapper
    {
        public static void Make_ZoomMap(Wdt map, string base_dir, CASCHandler cs)
        {
            string zoom_fullmap = base_dir + "/map_zoom.png";
            Console.WriteLine("Making zoom map:");
            for (var cur_x = 0; cur_x < 64; cur_x++)
            {
                for (var cur_y = 0; cur_y < 64; cur_y++)
                {
                    if (map.all_claimed_tiles[cur_x,cur_y] == true) {
                        //Console.WriteLine("exists " + cur_x + " /"  + cur_y);
                        if (cur_x < map.min_y) { map.min_y = cur_x;}
                        if (cur_y < map.min_x) { map.min_x = cur_y;}
                        if (cur_x > map.max_y) { map.max_y = cur_x;}
                        if (cur_y > map.max_x) { map.max_x = cur_y;}
                    }
                }
            }
            Console.WriteLine("minx " + map.min_x + " / miny " + map.min_y);
            Console.WriteLine("maxx " + map.max_x + " / maxy " + map.max_y);
            map.size_x = map.max_x - map.min_x +1;
            map.size_y = map.max_y - map.min_y +1;
            var canvas = NetVips.Image.Black(1, 1);
            for (var cur_x = 0; cur_x < 64; cur_x++)
            {
                for (var cur_y = 0; cur_y < 64; cur_y++)
                {
                    using (var stream = new MemoryStream())
                    {
                        var minit = map.maid_chunk.minit[cur_x, cur_y];
                        if ((minit != 0) && cs.FileExists((int) minit))
                        {
                            using (var stream2 = new MemoryStream())
                            {
                                new BlpFile(cs.OpenFile((int) minit)).GetBitmap(0).Save(stream2, System.Drawing.Imaging.ImageFormat.Png);
                                var image = NetVips.Image.NewFromBuffer(stream2.ToArray());

                                if (image.Width != Toolbox.tile_size)
                                {
                                    if (Toolbox.tile_size == 512 && image.Width == 256)
                                    {
                                        image = image.Resize(2, "VIPS_KERNEL_NEAREST");
                                    }
                                    else if (Toolbox.tile_size == 256 && image.Width == 512)
                                    {
                                        image = image.Resize(0.5, "VIPS_KERNEL_NEAREST");
                                    }
                                }
                                //Console.WriteLine("writing at : " + cur_y +" - " + map.min_y + " = " + (cur_y - map.min_y) + " | " +cur_x +" - " + map.min_x + " = " + (cur_x - map.min_x));
                                canvas = canvas.Insert(image,(cur_y - map.min_y) * Toolbox.tile_size,  (cur_x - map.min_x) * Toolbox.tile_size, true);
                            }
                        }
                    }
                }
            }
            Console.WriteLine("writing zoom map");
            canvas.WriteToFile(zoom_fullmap);
            CutZoomMap(map, zoom_fullmap,base_dir+"/zoom_tiles", 10);
        }

        
        /* Unref map*/
        static public void Make_UnrefMap(string base_dir,Toolbox.UnrefDef[,] unrefs, Wdt wdt)
        {
            Console.WriteLine("Making unref map:");
            string unrefjson = "[";
            for(int i = 0; i < 64; i++) {
                for(int j = 0; j < 64; j++) {
                    if (unrefs[i,j] != null) {
                        if ((unrefs[i,j].exists_adtname) || (unrefs[i,j].exists_obj0name) || (unrefs[i,j].exists_obj1name) || (unrefs[i,j].exists_minit) || (unrefs[i,j].adtname > 0) || (unrefs[i,j].obj0name > 0) || (unrefs[i,j].obj1name > 0) || (unrefs[i,j].minit > 0)) {
                            if (unrefjson != "[")
                                unrefjson += ",";
                            //Console.WriteLine(unrefs[i,j].x + " / " + unrefs[i,j].y + " | " + wdt.min_x + " / " + wdt.min_y);
                            unrefjson += "{\"x\":"+unrefs[i,j].x+",\"y\":"+unrefs[i,j].y+",\"exists_adtname\":\""+unrefs[i,j].exists_adtname+"\",\"exists_obj0name\":\""+unrefs[i,j].exists_obj0name+"\",\"exists_obj1name\":\""+unrefs[i,j].exists_obj1name+"\",\"exists_minit\":\""+unrefs[i,j].exists_minit+"\",\"adtname\":\""+unrefs[i,j].adtname+"\",\"obj0name\":\""+unrefs[i,j].obj0name+"\",\"obj1name\":\""+unrefs[i,j].obj1name+"\",\"minit\":\""+unrefs[i,j].minit+"\"}";
                        }
                    }
                }
            }
            unrefjson +="]";
            File.WriteAllText(base_dir + "/unrefs.json", unrefjson);
        }

        /* Area map*/
        static public void Make_AreaMap(string base_dir,Dictionary<int, List<Toolbox.Area>> Areas, Wdt wdt)
        {
            Console.WriteLine("Making area map:");
            string area_fullmap = base_dir + "/map_areas.png";
            var tilesize = 256;
            int size_per_mcnk = tilesize / 16;
            string areasjson = "[";
            List<System.Drawing.Color> colors = new List<System.Drawing.Color>();
            using (var areaall = new System.Drawing.Bitmap(wdt.size_y*tilesize,wdt.size_x*tilesize)) {
                using (var area_graphics = System.Drawing.Graphics.FromImage(areaall)) {
                    areaall.MakeTransparent();
                    area_graphics.DrawImage(areaall, 0, 0, areaall.Width, areaall.Height);
                    foreach (KeyValuePair<int, List<Toolbox.Area>> area in Areas){
                        Random rnd = new Random();
                        var AreaColor = System.Drawing.Color.FromArgb(255/2,rnd.Next(255), rnd.Next(255), rnd.Next(255));
                        while(colors.Contains(AreaColor)) {
                            AreaColor = System.Drawing.Color.FromArgb(255/2,rnd.Next(255), rnd.Next(255), rnd.Next(255));
                        }
                        colors.Add(AreaColor);
                        if (areasjson != "[")
                            areasjson += ",";
                        //Console.WriteLine(AreaColor.R + " / " + AreaColor.G + " /  " + AreaColor.B);
                        areasjson +="{\"r\":"+(Math.Round(AreaColor.R * (1.0f-(64.0f/127f))))+",\"g\":" + (Math.Round(AreaColor.G * (1.0f-(64.0f/127f)))) + ",\"b\":"+(Math.Round(AreaColor.B* (1.0f-(64.0f/127f))))+",\"ID\":\""+area.Value[0].ID+"\",\"ZoneName\":\""+area.Value[0].ZoneName+"\",\"ContinentID\":"+area.Value[0].ContinentID+",\"AreaName_lang\":\""+area.Value[0].AreaName_lang+"\"}";
                        using (System.Drawing.SolidBrush AreaBrush = new System.Drawing.SolidBrush(AreaColor)) {
                            foreach(Toolbox.Area a in area.Value) {
                                area_graphics.FillRectangle(AreaBrush, ((a.y - wdt.min_y) * tilesize) + (a.sub_x * size_per_mcnk), ((a.x - wdt.min_x) * tilesize) + (a.sub_y * size_per_mcnk), size_per_mcnk, size_per_mcnk);
                            }
                        }
                    }
                }
                Console.WriteLine("writing area map");
                NetVips.Image netimg = BitmapConverter.ToVips(areaall);
                netimg = netimg.Resize(2, "VIPS_KERNEL_NEAREST");
                netimg.WriteToFile(area_fullmap);
            }
            CutMap(area_fullmap,base_dir+"/area_tiles", 10);
            areasjson +="]";
            File.WriteAllText(base_dir + "/areas.json", areasjson);
        }

        /* Unknown map */
        static public void Make_UnknownMap(string base_dir,Adt[,] adts, Wdt wdt) {
            Console.WriteLine("Making unknown map:");
            string unknown_fullmap = base_dir + "/map_unknown.png";
            var tilesize = 256;
            int size_per_mcnk = tilesize / 16;
            System.Drawing.SolidBrush unknown_brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(170, 255, 255, 255));
            using (var areaall = new System.Drawing.Bitmap(wdt.size_y*tilesize,wdt.size_x*tilesize)) {
                using (var area_graphics = System.Drawing.Graphics.FromImage(areaall)) {
                    /*areaall.MakeTransparent();
                    area_graphics.DrawImage(areaall, 0, 0, areaall.Width, areaall.Height);*/
                    for (int x  = 0; x < 64; x++) {
                    for (int y = 0; y < 64; y++) {
                        for (int sub_x = 0; sub_x< 16; sub_x++) {
                        for (int sub_y = 0; sub_y< 16; sub_y++) {
                            if(adts[x,y].unknown[sub_x,sub_y] == 1) {
                                area_graphics.FillRectangle(unknown_brush, ((y - wdt.min_y) * tilesize) + (sub_x * size_per_mcnk), ((x - wdt.min_x) *tilesize) + (sub_y * size_per_mcnk), size_per_mcnk, size_per_mcnk);
                            }
                        }
                        }
                    }
                    }
                }
                unknown_brush.Dispose();
                Console.WriteLine("writing unknown map");
                NetVips.Image netimg = BitmapConverter.ToVips(areaall);
                netimg = netimg.Resize(2, "VIPS_KERNEL_NEAREST");
                netimg.WriteToFile(unknown_fullmap);
            }
            CutMap(unknown_fullmap,base_dir+"/unknown_tiles", 10);
        }

        /* Death borders */
        static public void Make_DeathMap(string output_dir, Adt[,] adts, Wdt wdt){
            JObject deathareas_json = new JObject();
            for (int x = 0; x < 64; x++){
                for (int y = 0; y < 64; y++){
                    if ( (adts[x,y].plane_min.a != null) && (adts[x,y].plane_min.b != null) && (adts[x,y].plane_min.c != null) && (adts[x,y].plane_max.a != null) &&  (adts[x,y].plane_max.b != null) &&  (adts[x,y].plane_max.c != null))
                        deathareas_json[((y - wdt.min_y)).ToString("00")+"_"+((x - wdt.min_x)).ToString("00")] = "Min : "+adts[x,y].plane_min.a[2]+"/"+adts[x,y].plane_min.b[2]+"/"+adts[x,y].plane_min.c[2]+"<br> Max : "+adts[x,y].plane_max.a[2]+"/"+adts[x,y].plane_max.b[2]+"/"+adts[x,y].plane_max.c[2];
                }
            }
            File.WriteAllText(output_dir + "/deathareas.json", deathareas_json.ToString());
        }

        static public void CreateModelsJson(Dictionary<uint, List<Toolbox.IngameObject>> IG_Obj,string map_dir,Wdt wdt, int x, int y){
           // Console.WriteLine("Making models json:");
            if (!Directory.Exists(map_dir+"/models"))
                Directory.CreateDirectory(map_dir+"/models");
            if ((wdt.wmo != null) && (wdt.found_unref == false)){
                string wmo_json = "[";
                wmo_json += "{\"name\":\"" + wdt.wmo.name + "\",\"coords\":[{\"posx\":" + wdt.wmo.posz + ",\"posy\":" + wdt.wmo.posx+"}],\"type\":" + wdt.wmo.type + ",\"id\":\""+wdt.wmo.filedataid+"\"}";
                wmo_json += "]";
                File.WriteAllText(map_dir + "/wdt_model.json", wmo_json);
            }
            else {
                string models = "[";
                foreach(KeyValuePair<uint, List<Toolbox.IngameObject>> obj in IG_Obj){
                    if (models != "[")
                        models +=",";
                    models += "{\"name\":\"" + obj.Value[0].name + "\",\"coords\":[";
                    var b = false;
                    foreach (Toolbox.IngameObject o in obj.Value){
                        if (b == true)
                            models += ",";
                        models += "{\"posx\":" + o.posz + ",\"posy\":" + o.posx+",\"posz\":"+o.posy+"}";
                        if (b == false)
                            b =true;
                    }
                    models += "],\"type\":" + obj.Value[0].type + ",\"id\":\""+obj.Key+"\"}";
                }
                models += "]";
                if (IG_Obj.Count > 0)
                    File.WriteAllText(map_dir + "/models/models"+x.ToString("00")+"_"+y.ToString("00")+".json", models);
                            
                if ((wdt.wmo != null) && (wdt.found_unref == false)){
                    string wmo_json = "[";
                    wmo_json += "{\"name\":\"" + wdt.wmo.name + "\",\"coords\":[{\"posx\":" + wdt.wmo.posz + ",\"posy\":" + wdt.wmo.posx+"}],\"type\":" + wdt.wmo.type + ",\"id\":\""+wdt.wmo.filedataid+"\"}";
                    wmo_json += "]";
                    File.WriteAllText(map_dir + "/wdt_model.json", wmo_json);
                }
            }
        }

        static public void Make_Models(string output_dir, Wdt wdt,Adt[,] adts, ObjAdt[,] obj0, ObjAdt[,] obj1){
            
            Console.WriteLine("Making models json:");
            Dictionary<uint, List<Toolbox.IngameObject>> listobj = new Dictionary<uint, List<Toolbox.IngameObject>>();
            if (!Directory.Exists(output_dir+"/models"))
                Directory.CreateDirectory(output_dir+"/models");
                
            if ((wdt.wmo != null) && (wdt.found_unref == false)){
                string wmo_json = "[";
                wmo_json += "{\"name\":\"" + wdt.wmo.name + "\",\"coords\":[{\"posx\":" + wdt.wmo.posz + ",\"posy\":" + wdt.wmo.posx+"}],\"type\":" + wdt.wmo.type + ",\"id\":\""+wdt.wmo.filedataid+"\"}";
                wmo_json += "]";
                File.WriteAllText(output_dir + "/wdt_model.json", wmo_json);
            }
            else {
                for (int x  = 0; x < 64; x++) {
                    for (int y = 0; y < 64; y++) {
                        listobj.Clear();
                        string models = "[";
                        foreach(Toolbox.IngameObject c in obj0[x,y].objects){
                            if (!listobj.ContainsKey(c.filedataid))
                                listobj.Add(c.filedataid, new List<Toolbox.IngameObject>());
                            listobj[c.filedataid].Add(c);
                            
                        }
                        foreach(Toolbox.IngameObject c in adts[x,y].objects){
                            if (!listobj.ContainsKey(c.filedataid))
                                listobj.Add(c.filedataid, new List<Toolbox.IngameObject>());
                            listobj[c.filedataid].Add(c);
                        }
                        foreach(Toolbox.IngameObject c in obj1[x,y].objects){
                            if (!listobj.ContainsKey(c.filedataid))
                                listobj.Add(c.filedataid, new List<Toolbox.IngameObject>());
                            listobj[c.filedataid].Add(c);
                        }
                        foreach(KeyValuePair<uint, List<Toolbox.IngameObject>> obj in listobj){
                            if (models != "[")
                                models +=",";
                            models += "{\"name\":\"" + obj.Value[0].name + "\",\"coords\":[";
                            var b = false;
                            foreach (Toolbox.IngameObject o in obj.Value){
                                if (b == true)
                                    models += ",";
                                models += "{\"posx\":" + o.posz + ",\"posy\":" + o.posx+",\"posz\":"+o.posy+"}";
                                if (b == false)
                                    b =true;
                            }
                            models += "],\"type\":" + obj.Value[0].type + ",\"id\":\""+obj.Key+"\"}";
                        }
                        models += "]";
                        if (listobj.Count > 0)
                            File.WriteAllText(output_dir + "/models/models"+x.ToString("00")+"_"+y.ToString("00")+".json", models);
                    }
                }
                if ((wdt.wmo != null) && (wdt.found_unref == false)){
                    string wmo_json = "[";
                    wmo_json += "{\"name\":\"" + wdt.wmo.name + "\",\"coords\":[{\"posx\":" + wdt.wmo.posz + ",\"posy\":" + wdt.wmo.posx+"}],\"type\":" + wdt.wmo.type + ",\"id\":\""+wdt.wmo.filedataid+"\"}";
                    wmo_json += "]";
                    File.WriteAllText(output_dir + "/wdt_model.json", wmo_json);
                }
            }
        }

        /* Wdt borders */
        static public void Make_WdtBorders(string output_dir, Wdt wdt){
            string border_string = "[";
            for (int x = 0; x < 64; x++){
                for (int y = 0; y < 64; y++){
                    if (wdt.wdt_claimed_tiles[x, y])
                    {
                        if (x == 0 || !wdt.wdt_claimed_tiles[x - 1, y])
                        {
                            if (String.Compare(border_string,"[")!=0) {
                                border_string += ",";
                            }
                            border_string += "{\"type\": \"Feature\",\"properties\": {\"name\": \"Border left" + x + "/"+ y + "\"},\"geometry\": {\"type\": \"LineString\",\"coordinates\": [["+((x - wdt.min_y)/2.0f)+","+((-(y - wdt.min_x))/2.0f)+"],["+((x - wdt.min_y)/2.0f)+","+((-(y - wdt.min_x)-1)/2.0f)+"]]}}"; 
                        }
                        if (x == 63 || !wdt.wdt_claimed_tiles[x + 1, y])
                        {
                            if (String.Compare(border_string,"[")!=0) {
                                border_string += ",";
                            }
                            border_string += "{\"type\": \"Feature\",\"properties\": {\"name\": \"Border right" + x + "/"+ y + "\"},\"geometry\": {\"type\": \"LineString\",\"coordinates\": [["+(((x - wdt.min_y)+1)/2.0f)+","+((-(y - wdt.min_x))/2.0f)+"],["+(((x - wdt.min_y)+1)/2.0f)+","+((-(y - wdt.min_x)-1)/2.0f)+"]]}}"; 
                        }
                        if (y == 0 || !wdt.wdt_claimed_tiles[x, y - 1])
                        {
                            if (String.Compare(border_string,"[")!=0) {
                                border_string += ",";
                            }
                            border_string += "{\"type\": \"Feature\",\"properties\": {\"name\": \"Border top " + x + "/"+ y + "\"},\"geometry\": {\"type\": \"LineString\",\"coordinates\": [["+((x - wdt.min_y)/2.0f)+","+((-(y - wdt.min_x))/2.0f)+"],["+(((x - wdt.min_y)+1)/2.0f)+","+((-(y - wdt.min_x))/2.0f)+"]]}}"; 
                        }
                        if (y == 63 || !wdt.wdt_claimed_tiles[x, y + 1])
                        {
                            if (String.Compare(border_string,"[")!=0) {
                                border_string += ",";
                            }
                            border_string += "{\"type\": \"Feature\",\"properties\": {\"name\": \"Border bot " + x + "/"+ y + "\"},\"geometry\": {\"type\": \"LineString\",\"coordinates\": [["+(((x - wdt.min_y))/2.0f)+","+((-(y - wdt.min_x)-1)/2.0f)+"],["+(((x - wdt.min_y)+1)/2.0f)+","+((-(y - wdt.min_x)-1)/2.0f)+"]]}}"; 
                        }
                    }
                }
            }
            border_string += "]";
            File.WriteAllText(output_dir + "/wdtborders.json", border_string);
        }

        /* Impass map */
        static public void Make_ImpassMap(string base_dir, Adt[,] adts, Wdt wdt)
        {
            Console.WriteLine("Making impass map:");
            string impass_fullmap = base_dir + "/map_impass.png";
            var tilesize = 256;
            int size_per_mcnk = tilesize / 16;
            Pen impass_pen = new Pen(Color.FromArgb(220, 255, 255, 0), 2.5f);
            Pen mid_impass_pen = new Pen(Color.FromArgb(220, 255, 0, 0), 2.5f);

            //Console.WriteLine(wdt.size_x + " / "+  wdt.size_y);

            using (var bitmap = new System.Drawing.Bitmap(wdt.size_y*tilesize,wdt.size_x*tilesize)) {
                using (var g_impass = System.Drawing.Graphics.FromImage(bitmap)) {
                    bitmap.MakeTransparent();
                    g_impass.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                    for (int a = 0; a < 64;a++)
                    {
                        for (int b = 0; b < 64; b++)
                        {
                            for (int i = 0; i < 16;i++)
                            {
                                for (int j = 0; j < 16; j++)
                                {
                                    Point topright = new Point((b-wdt.min_y)*tilesize + (int)size_per_mcnk * (int)i,(a-wdt.min_x)*tilesize+(int)size_per_mcnk * ((int)j + 1));
                                    Point topleft = new Point((b-wdt.min_y)*tilesize +(int)size_per_mcnk * (int)i, (a-wdt.min_x)*tilesize+(int)size_per_mcnk * (int)j);
                                    Point bottomright = new Point((b-wdt.min_y)*tilesize +(int)size_per_mcnk * ((int)i + 1), (a-wdt.min_x)*tilesize+(int)size_per_mcnk * ((int)j + 1));
                                    Point botttomleft = new Point((b-wdt.min_y)*tilesize +(int)size_per_mcnk * ((int)i + 1), (a-wdt.min_x)*tilesize+(int)size_per_mcnk * (int)j);

                                    int up = 0;
                                    int down = 0;
                                    int left = 0;
                                    int right = 0;
                                    int current = adts[a,b].impasses[i, j];

                                    if (j > 0)
                                        left = adts[a,b].impasses[i, j - 1];
                                    else if (a > 0)
                                        left = adts[a-1,b].impasses[i, 15];
                                    else
                                        left = -1; // wall


                                    if (j < 15)
                                        right = adts[a,b].impasses[i, j +1];
                                    else if (a < 63)
                                        right = adts[a+1,b].impasses[i, 0];
                                    else
                                        right = -1; // wall


                                    if (i > 0)
                                        up = adts[a,b].impasses[i - 1, j];
                                    else if (b > 0)
                                        up = adts[a,b-1].impasses[15, j];
                                    else
                                        up = -1; // wall

                                    if (i <15)
                                        down = adts[a,b].impasses[i + 1, j];
                                    else if (b <63)
                                        down = adts[a,b+1].impasses[0, j];
                                    else
                                        down = -1; // wall

                                    if (current == 1)
                                    { 
                                        if (left == 1)
                                            g_impass.DrawLine(mid_impass_pen, topleft, botttomleft);
                                        else if (left == -1)
                                            g_impass.DrawLine(impass_pen, topleft, botttomleft);
                                        if (right == 1)
                                            g_impass.DrawLine(mid_impass_pen, topright, bottomright);
                                        else if (right == -1)
                                            g_impass.DrawLine(impass_pen, topright, bottomright);
                                        if (up == 1)
                                            g_impass.DrawLine(mid_impass_pen, topleft, topright);
                                        else if (up == -1)
                                            g_impass.DrawLine(impass_pen, topleft, topright);
                                        if (down == 1)
                                            g_impass.DrawLine(mid_impass_pen, botttomleft, bottomright);
                                        else if (down == -1)
                                            g_impass.DrawLine(impass_pen, botttomleft, bottomright);
                                    }
                                    else if (current == 0) {
                                        if (left == 1)
                                            g_impass.DrawLine(mid_impass_pen, topleft, botttomleft);
                                        if (right == 1)
                                            g_impass.DrawLine(mid_impass_pen, topright, bottomright);
                                        if (up == 1)
                                            g_impass.DrawLine(mid_impass_pen, topleft, topright);
                                        if (down == 1)
                                            g_impass.DrawLine(mid_impass_pen, botttomleft, bottomright);
                                    }
                                }
                            }
                        }
                    }
                }
                impass_pen.Dispose();
                mid_impass_pen.Dispose();
                Console.WriteLine("writing impass map");
                NetVips.Image netimg = BitmapConverter.ToVips(bitmap);
                netimg = netimg.Resize(2, "VIPS_KERNEL_NEAREST");
                netimg.WriteToFile(impass_fullmap);
            }
            CutMap(impass_fullmap,base_dir+"/impass_tiles", 10);
        }
        
        public static void CutZoomMap(Wdt wdt, string inpng, string outdir, int maxzoom){
            var image = NetVips.Image.NewFromFile(inpng);
            for (var zoom = maxzoom; zoom > 1; zoom--)
            {

                var zoom_dir = outdir + "/"+zoom;
                Console.WriteLine(zoom);

                if (zoom != maxzoom)
                {
                    image = image.Resize(0.5, "VIPS_KERNEL_NEAREST");
                }

                var width = image.Width;
                var height = image.Height;

                // Always make sure that the image is dividable by 256
                if (width % 256 != 0)
                {
                    width = (width - (width % 256) + 256);
                }

                if (height % 256 != 0)
                {
                    height = (height - (height % 256) + 256);
                }

                if ((image.Width < 256) || (image.Height < 256)) {
                    if (zoom < 10)
                        wdt.minNative = zoom+1;
                    else 
                        wdt.minNative = 7;
                    Console.WriteLine("ending cut, too small");
                    break;
                }
                image = image.Gravity("VIPS_COMPASS_DIRECTION_NORTH_WEST", width, height);

                if (!Directory.Exists(zoom_dir))
                {
                    Directory.CreateDirectory(zoom_dir);
                }

                var w = 0;
                for (var x = 0; x < width; x += 256)
                {
                    var h = 0;
                    for (var y = 0; y < height; y += 256)
                    {
                        image.ExtractArea(x, y, 256, 256).WriteToFile(System.IO.Path.Combine(zoom_dir, w + "-" + h + ".png"));
                        h++;
                    }
                    w++;
                }
            }
            File.Delete(inpng);
        }
        
        public static void CutMap(string inpng, string outdir, int maxzoom){
            var image = NetVips.Image.NewFromFile(inpng);
            for (var zoom = maxzoom; zoom > 4; zoom--)
            {

                var zoom_dir = outdir + "/"+zoom;
                Console.WriteLine(zoom);

                if (zoom != maxzoom)
                {
                    image = image.Resize(0.5, "VIPS_KERNEL_NEAREST");
                }

                var width = image.Width;
                var height = image.Height;

                // Always make sure that the image is dividable by 256
                if (width % 256 != 0)
                {
                    width = (width - (width % 256) + 256);
                }

                if (height % 256 != 0)
                {
                    height = (height - (height % 256) + 256);
                }

                if ((image.Width < 256) || (image.Height < 256)) {
                    Console.WriteLine("ending cut, too small");
                    break;
                }
                image = image.Gravity("VIPS_COMPASS_DIRECTION_NORTH_WEST", width, height);

                if (!Directory.Exists(zoom_dir))
                {
                    Directory.CreateDirectory(zoom_dir);
                }

                var w = 0;
                for (var x = 0; x < width; x += 256)
                {
                    var h = 0;
                    for (var y = 0; y < height; y += 256)
                    {
                        image.ExtractArea(x, y, 256, 256).WriteToFile(System.IO.Path.Combine(zoom_dir, w + "-" + h + ".png"));
                        h++;
                    }
                    w++;
                }
            }
            File.Delete(inpng);
        }
    }

    
}
