// YmapDedupe - reads every .ymap in one or two map folders, finds overlapping/duplicate props,
// reports fully duplicate ymaps and writes cleaned .ymap files with the same names and folder structure.
// Only touches .ymap files. Never modifies the originals.
// Dil / Language: Türkçe + English (selectable in the window).
//
// Build with build.bat (uses the C# compiler that ships with Windows, C# 5 compatible).
// CodeWalker.Core.dll is NOT needed at build time; it is loaded from the CodeWalker folder at run time.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace YmapDedupe
{
    // ===================== LOCALIZATION (TR / EN) =====================

    static class Loc
    {
        public static bool En;   // false = Türkçe, true = English
        static readonly Dictionary<string, string[]> S = new Dictionary<string, string[]>();

        static void A(string key, string tr, string en) { S[key] = new string[] { tr, en }; }

        public static string T(string key)
        {
            string[] v;
            if (!S.TryGetValue(key, out v)) return key;
            return En ? v[1] : v[0];
        }

        public static string F(string key, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, T(key), args);
        }

        static Loc()
        {
            // --- window / controls ---
            A("title", "Ymap Çakışma Temizleyici", "Ymap Overlap Cleaner");
            A("row_a", "1. map klasörü:", "Map folder 1:");
            A("row_b", "2. map klasörü (isteğe bağlı):", "Map folder 2 (optional):");
            A("row_out", "Çıktı klasörü:", "Output folder:");
            A("row_cw", "CodeWalker klasörü:", "CodeWalker folder:");
            A("browse", "Seç...", "Browse...");
            A("eps_label", "Konum toleransı (metre):", "Position tolerance (meters):");
            A("cb_drop", "Tamamen kopya olan ymap'leri map klasörlerine koyma, '_silinebilir' klasörüne ayır",
                         "Do not put fully duplicate ymaps in the map folders; move them to '_deletable'");
            A("drop_dir", "_silinebilir", "_deletable");
            A("btn_scan", "1) Tara ve Çakışmaları Bul", "1) Scan and Find Overlaps");
            A("btn_save", "2) Temizlenmiş Kopyaları Kaydet", "2) Save Cleaned Copies");
            A("btn_csv", "Raporu CSV Kaydet", "Save Report as CSV");
            A("btn_stop", "Durdur", "Stop");
            A("ready", "Hazır. Çıktı klasörünün içinde, her map için kendi adıyla yeni bir klasör açılır (alt klasörler ve ymap isimleri aynen korunur).",
                       "Ready. A new folder named after each map is created inside the output folder (subfolders and ymap names are preserved).");
            A("col_file", "Dosya", "File");
            A("col_props", "Prop", "Props");
            A("col_matched", "Çakışan", "Overlapping");
            A("col_remove", "Silinecek", "To remove");
            A("col_status", "Durum", "Status");
            A("col_cov", "En çok çakıştığı dosyalar", "Most overlapping files");
            A("pick_a", "1. map klasörünü seçin (içinde .ymap dosyaları olan klasör)", "Select map folder 1 (the folder containing .ymap files)");
            A("pick_b", "2. map klasörünü seçin (isteğe bağlı)", "Select map folder 2 (optional)");
            A("pick_out", "Temizlenmiş dosyaların yazılacağı klasörü seçin", "Select the folder where the cleaned files will be written");
            A("pick_cw", "CodeWalker.exe'nin bulunduğu klasörü seçin", "Select the folder containing CodeWalker.exe");
            A("pick_csv", "Raporların (CSV) kaydedileceği klasörü seçin", "Select the folder where the reports (CSV) will be saved");
            A("stop_req", "Durdurma istendi...", "Stop requested...");

            // --- statuses ---
            A("k_full", "TAMAMEN KOPYA", "FULL DUPLICATE");
            A("k_full_other", "TAMAMEN KOPYA (başka içerik var!)", "FULL DUPLICATE (has other content!)");
            A("k_partial", "Kısmi çakışma", "Partial overlap");
            A("k_unreadable", "OKUNAMADI", "UNREADABLE");
            A("k_empty", "Prop yok", "No props");
            A("k_clean", "Temiz", "Clean");
            A("self_cov", "(kendi içinde)", "(within itself)");

            // --- dialog titles ---
            A("t_error", "Hata", "Error");
            A("t_missing", "Eksik bilgi", "Missing information");
            A("t_info", "Bilgi", "Information");
            A("t_nested", "Klasörler iç içe", "Nested folders");
            A("t_cwreq", "CodeWalker gerekli", "CodeWalker required");
            A("t_outbad", "Çıktı klasörü uygun değil", "Invalid output folder");
            A("t_save", "Kaydet", "Save");
            A("t_done", "Tamamlandı", "Completed");
            A("t_ok", "Tamam", "OK");

            // --- scan ---
            A("msg_nofolder", "Klasör bulunamadı:\r\n{0}", "Folder not found:\r\n{0}");
            A("msg_need_folder", "En az bir map klasörü seçin.", "Select at least one map folder.");
            A("log_same", "Uyarı: İki klasör aynı, yalnızca biri taranacak.", "Warning: Both folders are the same; only one will be scanned.");
            A("msg_nested", "Seçtiğiniz klasörlerden biri diğerinin içinde. Aynı dosyalar iki kez okunurdu.\r\nLütfen birbirinden ayrı iki klasör seçin.",
                            "One of the selected folders is inside the other, so the same files would be read twice.\r\nPlease select two separate folders.");
            A("msg_cwreq", "CodeWalker klasörü seçilmedi veya içinde CodeWalker.Core.dll yok.\r\n\r\n\"CodeWalker klasörü\" satırından CodeWalker.exe'nin bulunduğu klasörü seçin.",
                           "No CodeWalker folder was selected, or it does not contain CodeWalker.Core.dll.\r\n\r\nUse the \"CodeWalker folder\" row to select the folder containing CodeWalker.exe.");
            A("st_loading_cw", "CodeWalker yükleniyor...", "Loading CodeWalker...");
            A("log_found", "{0}: {1} ymap bulundu", "{0}: {1} ymap files found");
            A("log_none", "Hiç .ymap dosyası bulunamadı.", "No .ymap files found.");
            A("log_scan_stop", "Tarama durduruldu.", "Scan stopped.");
            A("st_reading", "Okunuyor ({0}/{1}): {2}", "Reading ({0}/{1}): {2}");
            A("log_unreadable", "  ! OKUNAMADI: {0}  -> {1}", "  ! UNREADABLE: {0}  -> {1}");
            A("log_total", "Toplam {0} ymap, {1} prop okundu.", "Total: {0} ymap files, {1} props read.");
            A("log_total_unread", "  ({0} dosya okunamadı)", "  ({0} files could not be read)");
            A("log_allfail1", "HİÇBİR dosya okunamadı. Bu genelde CodeWalker sürümünün bu programla uyumsuz olduğunu gösterir.",
                              "NO files could be read. This usually means the CodeWalker version is not compatible with this program.");
            A("log_allfail2", "Yukarıdaki hata mesajlarını geliştiriciye iletin.", "Please pass the error messages above on to the developer.");
            A("st_read_fail", "Okuma başarısız.", "Reading failed.");
            A("st_searching", "Çakışmalar aranıyor...", "Searching for overlaps...");
            A("st_scan_done", "Tarama bitti. Sonuçları inceleyip \"{0}\" düğmesine basabilirsiniz.",
                              "Scan finished. Review the results, then click \"{0}\".");
            A("log_error", "HATA: {0}", "ERROR: {0}");

            // --- summary ---
            A("sum_overlap", "Çakışan prop: {0}  (silinecek: {1}, LOD bağlantılı olduğu için atlanan: {2})",
                             "Overlapping props: {0}  (to remove: {1}, skipped because LOD-linked: {2})");
            A("sum_full_hdr", "=== TAMAMEN KOPYA OLAN YMAP'LER (tüm propları başka ymap'lerde zaten var) ===",
                              "=== FULLY DUPLICATE YMAPS (every prop already exists in other ymaps) ===");
            A("sum_other", "      ! Prop dışı içerik de var (car generator/occluder/grass/MLO). Silmeden önce kontrol edin.",
                           "      ! Also contains non-prop content (car generators/occluders/grass/MLO). Check before deleting.");
            A("sum_drop_note", "Kaydederken (ilgili kutu işaretliyse) bu dosyalar '{0}' klasörüne ayrılır; silip silmemek size kalır.",
                               "When saving (if the related box is checked) these files are moved to the '{0}' folder; whether to delete them is up to you.");
            A("sum_none", "Tamamen kopya olan ymap yok.", "There are no fully duplicate ymaps.");

            // --- save ---
            A("msg_scan_first", "Önce tarama yapın.", "Please scan first.");
            A("msg_out_first", "Çıktı klasörünü seçin.", "Select an output folder.");
            A("msg_outbad", "Çıktı klasörü map klasörlerinden birinin içinde veya onu kapsıyor.\r\nOrijinallerin üzerine yazılmaması için map klasörlerinden bağımsız bir çıktı klasörü seçin.",
                            "The output folder is inside one of the map folders or contains one.\r\nSelect an output folder that is independent of the map folders so the originals are not overwritten.");
            A("conf_head", "Şu klasörler oluşturulacak:\r\n", "The following folders will be created:\r\n");
            A("conf_clean", "{0} ymap temizlenip yazılacak,\r\n", "{0} ymap files will be cleaned and written,\r\n");
            A("conf_drop", "{0} tamamen kopya ymap '{1}' içine ayrılacak,\r\n", "{0} fully duplicate ymap files will be moved into '{1}',\r\n");
            A("conf_copy", "{0} ymap olduğu gibi kopyalanacak.\r\n\r\n", "{0} ymap files will be copied as they are.\r\n\r\n");
            A("conf_struct", "Her map klasörünün alt klasör yapısı ve ymap isimleri aynen korunur.\r\n",
                             "Each map folder's subfolder structure and ymap names are preserved exactly.\r\n");
            A("conf_exists", "\r\nDİKKAT: Bu klasörlerden bazıları zaten var; aynı isimli dosyaların üzerine yazılacak.\r\n",
                             "\r\nWARNING: Some of these folders already exist; files with the same names will be overwritten.\r\n");
            A("conf_tail", "\r\nOrijinal dosyalara dokunulmaz. Devam edilsin mi?", "\r\nThe original files are not touched. Continue?");
            A("log_saving", "=== KAYDEDİLİYOR: {0} ===", "=== SAVING: {0} ===");
            A("log_save_stop", "Durduruldu. Şu ana kadar yazılanlar çıktı klasöründe kaldı.", "Stopped. Files written so far remain in the output folder.");
            A("st_writing", "Yazılıyor ({0}/{1}): {2}", "Writing ({0}/{1}): {2}");
            A("err_changed", "dosya taramadan sonra değişmiş, yeniden tarayın", "the file changed after scanning; please scan again");
            A("err_verify", "doğrulama başarısız: beklenen {0} prop, okunan {1}", "verification failed: expected {0} props, read {1}");
            A("log_cleaned", "  temizlendi: {0}  (-{1} prop)", "  cleaned: {0}  (-{1} props)");
            A("log_savefail", "  ! KAYDEDİLEMEDİ: {0} -> {1}  (orijinal dosya aynen kopyalandı)", "  ! COULD NOT SAVE: {0} -> {1}  (the original file was copied unchanged)");
            A("log_done", "BİTTİ. Temizlenen: {0}, olduğu gibi kopyalanan: {1}, '{2}' klasörüne ayrılan: {3}, hata: {4}",
                          "DONE. Cleaned: {0}, copied as is: {1}, moved to '{2}': {3}, errors: {4}");
            A("log_reports", "Raporlar çıktı klasörüne yazıldı: {0}, {1}", "Reports were written to the output folder: {0}, {1}");
            A("st_done", "Bitti. Çıktı: {0}", "Done. Output: {0}");
            A("done_box", "Bitti.\r\n\r\nTemizlenen: {0}\r\nOlduğu gibi kopyalanan: {1}\r\n'{2}' klasörüne ayrılan: {3}\r\nHata (orijinali kopyalandı): {4}\r\n\r\nÇıktı: {5}",
                          "Done.\r\n\r\nCleaned: {0}\r\nCopied as is: {1}\r\nMoved to '{2}': {3}\r\nErrors (original copied): {4}\r\n\r\nOutput: {5}");
            A("log_csv_saved", "Raporlar kaydedildi: {0}", "Reports saved: {0}");
            A("msg_csv_saved", "{0} ve {1} kaydedildi.", "{0} and {1} were saved.");

            // --- reports (CSV) ---
            A("csv_sum_name", "ymap_ozet.csv", "ymap_summary.csv");
            A("csv_det_name", "ymap_cakisma_detay.csv", "ymap_overlap_details.csv");
            A("hdr_sum", "dosya,prop_sayisi,eslesen,silinecek,yuzde,durum,en_cok_cakistigi_dosyalar",
                         "file,prop_count,matched,to_remove,percent,status,most_overlapping_files");
            A("hdr_det", "dosya,sira,tutulan_dosya,tutulan_sira,arketip,mesafe_m,islem",
                         "file,index,kept_file,kept_index,archetype,distance_m,action");
            A("act_remove", "silinecek", "remove");
            A("act_lod", "LOD_baglantili_atlandi", "skipped_lod_linked");

            // --- CodeWalker bridge errors ---
            A("e_cw_change", "CodeWalker klasörünü değiştirmek için programı kapatıp yeniden açın.", "To change the CodeWalker folder, close and reopen the program.");
            A("e_cw_dll", "Bu klasörde CodeWalker.Core.dll yok:\r\n{0}\r\n\r\nCodeWalker.exe'nin bulunduğu klasörü seçin.",
                          "CodeWalker.Core.dll was not found in this folder:\r\n{0}\r\n\r\nSelect the folder containing CodeWalker.exe.");
            A("e_cw_bad", "CodeWalker.Core.dll bu programla uyumlu değil (.NET Framework sürümü gerekiyor).",
                          "CodeWalker.Core.dll is not compatible with this program (a .NET Framework build is required).");
            A("e_cw_types", "CodeWalker.Core.dll içinde YmapFile/RpfFile bulunamadı. CodeWalker sürümü farklı olabilir.",
                            "YmapFile/RpfFile was not found in CodeWalker.Core.dll. The CodeWalker version may be different.");
            A("e_member", "{0} bulunamadı (CodeWalker sürümü farklı olabilir)", "{0} was not found (the CodeWalker version may be different)");
            A("e_savenull", "Save() veri döndürmedi", "Save() returned no data");
        }
    }

    // ===================== DATA MODEL =====================

    class Ent
    {
        public int FileId;
        public int Index;
        public string Name;
        public uint NameHash;
        public float X, Y, Z, QX, QY, QZ, QW, SXY, SZ;
        public bool LodLinked;       // part of a LOD hierarchy (parent or child)
        public object Handle;        // only used while saving
        public Ent MatchedWith;
        public bool Removed;
    }

    class YmapItem
    {
        public int Id;
        public string Root;
        public string RootLabel;
        public string FullPath;
        public string RelPath;
        public bool Readable;
        public bool HasOtherContent; // car generator, occluder, grass, MLO etc.
        public List<Ent> Ents = new List<Ent>();
        public int Matched, Removed;
        public Dictionary<int, int> CoveredBy = new Dictionary<int, int>();
        public bool DropCandidate;   // every prop already exists in other ymaps
    }

    enum Kind { Full, FullOther, Partial, Unreadable, Clean, Empty }

    struct Cell : IEquatable<Cell>
    {
        public readonly uint H; public readonly int X, Y, Z;
        public Cell(uint h, int x, int y, int z) { H = h; X = x; Y = y; Z = z; }
        public bool Equals(Cell o) { return H == o.H && X == o.X && Y == o.Y && Z == o.Z; }
        public override bool Equals(object obj) { return obj is Cell && Equals((Cell)obj); }
        public override int GetHashCode()
        {
            unchecked { int h = (int)H; h = h * 397 ^ X; h = h * 397 ^ Y; h = h * 397 ^ Z; return h; }
        }
    }

    // ===================== SETTINGS (ymapdedupe.ini next to the exe) =====================

    static class Settings
    {
        static string IniPath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ymapdedupe.ini"); } }

        static Dictionary<string, string> ReadAll()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            try
            {
                if (File.Exists(IniPath))
                {
                    string[] lines = File.ReadAllLines(IniPath, Encoding.UTF8);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string l = lines[i].Trim();
                        if (l.Length == 0) continue;
                        if (l.StartsWith("cw=")) d["cw"] = l.Substring(3);
                        else if (l.StartsWith("lang=")) d["lang"] = l.Substring(5);
                        else if (i == 0) d["cw"] = l;   // old format: just the CodeWalker folder path
                    }
                }
            }
            catch { }
            return d;
        }

        public static string Get(string key)
        {
            Dictionary<string, string> d = ReadAll();
            string v;
            return d.TryGetValue(key, out v) ? v : "";
        }

        public static void Set(string key, string value)
        {
            try
            {
                Dictionary<string, string> d = ReadAll();
                d[key] = value;
                List<string> lines = new List<string>();
                foreach (KeyValuePair<string, string> kv in d) lines.Add(kv.Key + "=" + kv.Value);
                File.WriteAllLines(IniPath, lines.ToArray(), Encoding.UTF8);
            }
            catch { }
        }
    }

    // ===================== CodeWalker.Core BRIDGE (reflection; no build-time dependency) =====================

    static class CW
    {
        public static bool Initialized;
        public static string Folder;
        static Type tYmap;
        static MethodInfo loadRes;
        static MethodInfo removeM, saveM, extentsM;
        static readonly Dictionary<string, MemberInfo> cache = new Dictionary<string, MemberInfo>();

        static readonly string[] OtherContentNames =
        {
            "CarGenerators", "GrassInstanceBatches", "BoxOccluders", "OccludeModels",
            "TimeCycleModifiers", "LodLights", "DistantLODLights"
        };

        public static void Init(string folder)
        {
            if (Initialized)
            {
                if (!string.Equals(folder, Folder, StringComparison.OrdinalIgnoreCase))
                    throw new Exception(Loc.T("e_cw_change"));
                return;
            }
            string dll = Path.Combine(folder, "CodeWalker.Core.dll");
            if (!File.Exists(dll)) throw new Exception(Loc.F("e_cw_dll", folder));
            Folder = folder;
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;

            Assembly core;
            try { core = Assembly.LoadFrom(dll); }
            catch (BadImageFormatException) { throw new Exception(Loc.T("e_cw_bad")); }

            tYmap = core.GetType("CodeWalker.GameFiles.YmapFile", false);
            Type tRpf = core.GetType("CodeWalker.GameFiles.RpfFile", false);
            if (tYmap == null || tRpf == null) throw new Exception(Loc.T("e_cw_types"));

            foreach (MethodInfo m in tRpf.GetMethods(BindingFlags.Public | BindingFlags.Static))
                if (m.Name == "LoadResourceFile" && m.IsGenericMethodDefinition && m.GetParameters().Length == 3)
                    loadRes = m;
            if (loadRes == null) throw new Exception(Loc.F("e_member", "RpfFile.LoadResourceFile"));
            loadRes = loadRes.MakeGenericMethod(tYmap);
            Initialized = true;
        }

        static Assembly Resolve(object sender, ResolveEventArgs args)
        {
            try
            {
                string name = new AssemblyName(args.Name).Name + ".dll";
                string p = Path.Combine(Folder, name);
                if (File.Exists(p)) return Assembly.LoadFrom(p);
            }
            catch { }
            return null;
        }

        // ---- reflection helpers ----

        static MemberInfo Find(Type t, string name)
        {
            string key = t.FullName + "|" + name;
            MemberInfo m;
            lock (cache) { if (cache.TryGetValue(key, out m)) return m; }
            BindingFlags f = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo p = null;
            try { p = t.GetProperty(name, f); } catch (AmbiguousMatchException) { }
            m = p != null ? (MemberInfo)p : (MemberInfo)t.GetField(name, f);
            lock (cache) { cache[key] = m; }
            return m;
        }

        static bool TryGet(object o, string name, out object val)
        {
            val = null;
            if (o == null) return false;
            MemberInfo m = Find(o.GetType(), name);
            if (m == null) return false;
            PropertyInfo p = m as PropertyInfo;
            val = p != null ? p.GetValue(o, null) : ((FieldInfo)m).GetValue(o);
            return true;
        }

        static object Get(object o, string name)
        {
            object v;
            if (!TryGet(o, name, out v))
                throw new MissingMemberException(Loc.F("e_member", (o == null ? "null" : o.GetType().Name) + "." + name));
            return v;
        }

        static MethodInfo FindMethod(Type t, string name, int paramCount)
        {
            foreach (MethodInfo m in t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                if (m.Name == name && m.GetParameters().Length == paramCount) return m;
            return null;
        }

        static object Invoke(MethodInfo m, object target, params object[] args)
        {
            try { return m.Invoke(target, args); }
            catch (TargetInvocationException ex)
            {
                Exception inner = ex.InnerException ?? ex;
                throw new Exception(inner.GetType().Name + ": " + inner.Message, inner);
            }
        }

        static float F(object v) { return Convert.ToSingle(v, CultureInfo.InvariantCulture); }
        static long L(object v) { return Convert.ToInt64(v, CultureInfo.InvariantCulture); }

        // ---- ymap operations ----

        public static object Load(byte[] data)
        {
            object y = Activator.CreateInstance(tYmap);
            ParameterInfo[] ps = loadRes.GetParameters();
            object ver = Convert.ChangeType(2, ps[2].ParameterType, CultureInfo.InvariantCulture);
            Invoke(loadRes, null, y, data, ver);
            return y;
        }

        // Converts the normal props (CEntityDef) to the plain model. MLO instances are left alone.
        public static List<Ent> Entities(object y, int fileId, out bool hadMlo)
        {
            hadMlo = false;
            List<Ent> list = new List<Ent>();
            IList all = Get(y, "AllEntities") as IList;
            if (all == null) return list;
            for (int i = 0; i < all.Count; i++)
            {
                object e = all[i];
                if (e == null) continue;
                object mlo;
                if (TryGet(e, "MloInstance", out mlo) && mlo != null) { hadMlo = true; continue; }
                object d = Get(e, "_CEntityDef");
                object pos = Get(d, "position");
                object rot = Get(d, "rotation");
                object nm = Get(d, "archetypeName");
                Ent n = new Ent();
                n.FileId = fileId;
                n.Index = i;
                n.Handle = e;
                n.NameHash = Convert.ToUInt32(Get(nm, "Hash"), CultureInfo.InvariantCulture);
                n.Name = Convert.ToString(nm, CultureInfo.InvariantCulture);
                n.X = F(Get(pos, "X")); n.Y = F(Get(pos, "Y")); n.Z = F(Get(pos, "Z"));
                n.QX = F(Get(rot, "X")); n.QY = F(Get(rot, "Y")); n.QZ = F(Get(rot, "Z")); n.QW = F(Get(rot, "W"));
                n.SXY = F(Get(d, "scaleXY")); n.SZ = F(Get(d, "scaleZ"));
                n.LodLinked = L(Get(d, "parentIndex")) >= 0 || L(Get(d, "numChildren")) > 0;
                list.Add(n);
            }
            return list;
        }

        // Any content besides props? (if a name does not match, it is simply treated as "none")
        public static bool HasOtherContent(object y)
        {
            foreach (string n in OtherContentNames)
            {
                object v;
                if (TryGet(y, n, out v))
                {
                    ICollection c = v as ICollection;
                    if (c != null && c.Count > 0) return true;
                }
            }
            return false;
        }

        public static void Remove(object y, List<Ent> toRemove)
        {
            if (removeM == null)
            {
                removeM = FindMethod(y.GetType(), "RemoveEntity", 1);
                if (removeM == null) throw new MissingMethodException(Loc.F("e_member", "YmapFile.RemoveEntity"));
            }
            foreach (Ent r in toRemove) Invoke(removeM, y, r.Handle);
            if (extentsM == null) extentsM = FindMethod(y.GetType(), "CalcExtents", 0);
            if (extentsM != null) Invoke(extentsM, y);
        }

        public static byte[] Save(object y)
        {
            if (saveM == null)
            {
                saveM = FindMethod(y.GetType(), "Save", 0);
                if (saveM == null) throw new MissingMethodException(Loc.F("e_member", "YmapFile.Save"));
            }
            byte[] data = Invoke(saveM, y) as byte[];
            if (data == null) throw new Exception(Loc.T("e_savenull"));
            return data;
        }
    }

    // ===================== OVERLAP ENGINE =====================

    static class Engine
    {
        public static void Dedupe(List<Ent> ents, double posEps, double rotEps, double scaleEps, List<YmapItem> items)
        {
            double cell = Math.Max(posEps, 1e-3);
            Dictionary<Cell, List<Ent>> grid = new Dictionary<Cell, List<Ent>>();
            foreach (Ent e in ents)
            {
                int cx = (int)Math.Floor(e.X / cell), cy = (int)Math.Floor(e.Y / cell), cz = (int)Math.Floor(e.Z / cell);
                Ent m = FindMatch(grid, e, cx, cy, cz, posEps, rotEps, scaleEps);
                if (m == null)
                {
                    Cell key = new Cell(e.NameHash, cx, cy, cz);
                    List<Ent> l;
                    if (!grid.TryGetValue(key, out l)) { l = new List<Ent>(1); grid[key] = l; }
                    l.Add(e);
                    continue;
                }
                // A matched prop is never added to the grid: the "kept" one is always one that really stays in the output.
                e.MatchedWith = m;
                YmapItem it = items[e.FileId];
                it.Matched++;
                int c; it.CoveredBy.TryGetValue(m.FileId, out c); it.CoveredBy[m.FileId] = c + 1;
                if (!e.LodLinked) { e.Removed = true; it.Removed++; }
            }
        }

        static Ent FindMatch(Dictionary<Cell, List<Ent>> grid, Ent e, int cx, int cy, int cz, double pe, double re, double se)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        List<Ent> l;
                        if (!grid.TryGetValue(new Cell(e.NameHash, cx + dx, cy + dy, cz + dz), out l)) continue;
                        foreach (Ent k in l) if (Same(e, k, pe, re, se)) return k;
                    }
            return null;
        }

        static bool Same(Ent a, Ent b, double pe, double re, double se)
        {
            double dx = a.X - b.X, dy = a.Y - b.Y, dz = a.Z - b.Z;
            if (dx * dx + dy * dy + dz * dz > pe * pe) return false;
            double dot = Math.Abs((double)a.QX * b.QX + (double)a.QY * b.QY + (double)a.QZ * b.QZ + (double)a.QW * b.QW);
            if (dot < 1.0 - re) return false;   // q and -q are the same rotation
            return Math.Abs(a.SXY - b.SXY) <= se && Math.Abs(a.SZ - b.SZ) <= se;
        }
    }

    // ===================== WINDOW =====================

    class ScanArgs
    {
        public List<string> Roots;
        public string CwFolder;
        public double Eps;
    }

    class MainForm : Form
    {
        TextBox tbA, tbB, tbOut, tbCw, log;
        Button btnA, btnB, btnOut, btnCw, btnScan, btnSave, btnStop, btnCsv;
        CheckBox cbDrop;
        ComboBox cbLang;
        NumericUpDown nudEps;
        ListView lv;
        ProgressBar pb;
        Label lblStatus;
        SplitContainer split;

        // controls whose Text follows the selected language
        List<KeyValuePair<Control, string>> loc = new List<KeyValuePair<Control, string>>();

        List<YmapItem> items = new List<YmapItem>();
        volatile bool cancel;
        volatile bool busy;

        public MainForm()
        {
            // language: saved choice, otherwise the Windows UI language
            string savedLang = Settings.Get("lang");
            if (savedLang.Length > 0) Loc.En = savedLang == "en";
            else Loc.En = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "tr";

            ClientSize = new Size(1040, 770);
            MinimumSize = new Size(900, 620);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9f);
            Reg(this, "title");

            int y = 12;

            Label ll = new Label();
            ll.Text = "Dil / Language:"; ll.Left = 12; ll.Top = y + 4; ll.Width = 190;
            Controls.Add(ll);
            cbLang = new ComboBox();
            cbLang.DropDownStyle = ComboBoxStyle.DropDownList;
            cbLang.Left = 205; cbLang.Top = y; cbLang.Width = 150;
            cbLang.Items.Add("Türkçe");
            cbLang.Items.Add("English");
            cbLang.SelectedIndex = Loc.En ? 1 : 0;
            Controls.Add(cbLang);
            y += 34;

            tbA = Row("row_a", y, out btnA); y += 32;
            tbB = Row("row_b", y, out btnB); y += 32;
            tbOut = Row("row_out", y, out btnOut); y += 32;
            tbCw = Row("row_cw", y, out btnCw); y += 38;

            Label le = new Label();
            le.Left = 12; le.Top = y + 4; le.Width = 190;
            Reg(le, "eps_label");
            Controls.Add(le);
            nudEps = new NumericUpDown();
            nudEps.Left = 205; nudEps.Top = y; nudEps.Width = 70;
            nudEps.DecimalPlaces = 3; nudEps.Minimum = 0.001M; nudEps.Maximum = 5M; nudEps.Increment = 0.01M; nudEps.Value = 0.02M;
            Controls.Add(nudEps);
            cbDrop = new CheckBox();
            cbDrop.Left = 300; cbDrop.Top = y + 1; cbDrop.Width = 720; cbDrop.Checked = true;
            Reg(cbDrop, "cb_drop");
            Controls.Add(cbDrop);
            y += 36;

            btnScan = MakeButton("btn_scan", 12, y, 220);
            btnSave = MakeButton("btn_save", 240, y, 260);
            btnCsv = MakeButton("btn_csv", 508, y, 170);
            btnStop = MakeButton("btn_stop", 686, y, 100);
            btnSave.Enabled = false; btnCsv.Enabled = false; btnStop.Enabled = false;
            y += 40;

            pb = new ProgressBar();
            pb.Left = 12; pb.Top = y; pb.Width = ClientSize.Width - 24; pb.Height = 16;
            pb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(pb);
            y += 20;
            lblStatus = new Label();
            lblStatus.Left = 12; lblStatus.Top = y; lblStatus.Width = ClientSize.Width - 24; lblStatus.Height = 18;
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.Text = Loc.T("ready");
            Controls.Add(lblStatus);
            y += 24;

            split = new SplitContainer();
            split.Orientation = Orientation.Horizontal;
            split.Left = 12; split.Top = y; split.Width = ClientSize.Width - 24; split.Height = ClientSize.Height - y - 12;
            split.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(split);

            lv = new ListView();
            lv.Dock = DockStyle.Fill; lv.View = View.Details; lv.FullRowSelect = true; lv.GridLines = true;
            lv.Columns.Add(Loc.T("col_file"), 380);
            lv.Columns.Add(Loc.T("col_props"), 65);
            lv.Columns.Add(Loc.T("col_matched"), 85);
            lv.Columns.Add(Loc.T("col_remove"), 80);
            lv.Columns.Add(Loc.T("col_status"), 210);
            lv.Columns.Add(Loc.T("col_cov"), 360);
            split.Panel1.Controls.Add(lv);

            log = new TextBox();
            log.Dock = DockStyle.Fill; log.Multiline = true; log.ReadOnly = true; log.ScrollBars = ScrollBars.Vertical;
            log.Font = new Font("Consolas", 9f);
            split.Panel2.Controls.Add(log);

            // defaults
            string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            string saved = Settings.Get("cw");
            if (File.Exists(Path.Combine(baseDir, "CodeWalker.Core.dll"))) tbCw.Text = baseDir;
            else if (saved.Length > 0 && File.Exists(Path.Combine(saved, "CodeWalker.Core.dll"))) tbCw.Text = saved;
            tbOut.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ymap_temiz");

            btnA.Click += delegate { PickFolder(tbA, "pick_a"); };
            btnB.Click += delegate { PickFolder(tbB, "pick_b"); };
            btnOut.Click += delegate { PickFolder(tbOut, "pick_out"); };
            btnCw.Click += delegate { PickFolder(tbCw, "pick_cw"); };
            btnScan.Click += delegate { OnScan(); };
            btnSave.Click += delegate { OnSave(); };
            btnStop.Click += delegate { cancel = true; Log(Loc.T("stop_req")); };
            btnCsv.Click += delegate { OnCsv(); };

            cbLang.SelectedIndexChanged += delegate
            {
                Loc.En = cbLang.SelectedIndex == 1;
                Settings.Set("lang", Loc.En ? "en" : "tr");
                ApplyLanguage();
            };

            Shown += delegate
            {
                try { split.SplitterDistance = Math.Max(120, split.Height * 62 / 100); } catch { }
            };
            FormClosing += delegate { cancel = true; };
        }

        // ---------- UI helpers ----------

        void Reg(Control c, string key)
        {
            loc.Add(new KeyValuePair<Control, string>(c, key));
            c.Text = Loc.T(key);
        }

        void ApplyLanguage()
        {
            foreach (KeyValuePair<Control, string> kv in loc) kv.Key.Text = Loc.T(kv.Value);
            lv.Columns[0].Text = Loc.T("col_file");
            lv.Columns[1].Text = Loc.T("col_props");
            lv.Columns[2].Text = Loc.T("col_matched");
            lv.Columns[3].Text = Loc.T("col_remove");
            lv.Columns[4].Text = Loc.T("col_status");
            lv.Columns[5].Text = Loc.T("col_cov");
            if (!busy) lblStatus.Text = items.Count == 0 ? Loc.T("ready") : Loc.F("st_scan_done", Loc.T("btn_save"));
            FillList();
        }

        TextBox Row(string key, int y, out Button btn)
        {
            Label l = new Label();
            l.Left = 12; l.Top = y + 4; l.Width = 190;
            Reg(l, key);
            TextBox t = new TextBox();
            t.Left = 205; t.Top = y; t.Width = ClientSize.Width - 205 - 120;
            t.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Button b = new Button();
            b.Left = ClientSize.Width - 105; b.Top = y - 1; b.Width = 93; b.Height = 25;
            b.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Reg(b, "browse");
            Controls.Add(l); Controls.Add(t); Controls.Add(b);
            btn = b;
            return t;
        }

        Button MakeButton(string key, int x, int y, int w)
        {
            Button b = new Button();
            b.Left = x; b.Top = y; b.Width = w; b.Height = 32;
            Reg(b, key);
            Controls.Add(b);
            return b;
        }

        void PickFolder(TextBox tb, string descKey)
        {
            using (FolderBrowserDialog d = new FolderBrowserDialog())
            {
                d.Description = Loc.T(descKey);
                if (tb.Text.Trim().Length > 0 && Directory.Exists(tb.Text.Trim())) d.SelectedPath = tb.Text.Trim();
                if (d.ShowDialog(this) == DialogResult.OK) tb.Text = d.SelectedPath;
            }
        }

        void UI(Action a)
        {
            if (IsDisposed) return;
            try
            {
                if (InvokeRequired) BeginInvoke(a); else a();
            }
            catch (InvalidOperationException) { }
        }

        void Log(string s) { UI(delegate { log.AppendText(s + "\r\n"); }); }

        void Progress(int v, int max, string text)
        {
            UI(delegate
            {
                pb.Maximum = Math.Max(1, max);
                pb.Value = Math.Min(Math.Max(0, v), pb.Maximum);
                lblStatus.Text = text;
            });
        }

        void SetBusy(bool b)
        {
            UI(delegate
            {
                busy = b;
                btnScan.Enabled = !b;
                btnSave.Enabled = !b && items.Count > 0;
                btnCsv.Enabled = !b && items.Count > 0;
                btnStop.Enabled = b;
            });
        }

        void Msg(string text, string titleKey, MessageBoxIcon icon)
        {
            MessageBox.Show(this, text, Loc.T(titleKey), MessageBoxButtons.OK, icon);
        }

        static bool IsInside(string parent, string child)
        {
            return child.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        static string Lbl(YmapItem it) { return Path.Combine(it.RootLabel, it.RelPath); }

        static string Q(string s) { return "\"" + (s ?? "").Replace("\"", "\"\"") + "\""; }

        static Kind KindOf(YmapItem it)
        {
            if (!it.Readable) return Kind.Unreadable;
            if (it.Ents.Count == 0) return Kind.Empty;
            if (it.Matched == it.Ents.Count) return it.HasOtherContent ? Kind.FullOther : Kind.Full;
            return it.Matched > 0 ? Kind.Partial : Kind.Clean;
        }

        static string KindText(Kind k)
        {
            switch (k)
            {
                case Kind.Full: return Loc.T("k_full");
                case Kind.FullOther: return Loc.T("k_full_other");
                case Kind.Partial: return Loc.T("k_partial");
                case Kind.Unreadable: return Loc.T("k_unreadable");
                case Kind.Empty: return Loc.T("k_empty");
                default: return Loc.T("k_clean");
            }
        }

        string CoveredText(YmapItem it, int take)
        {
            return string.Join(" | ", it.CoveredBy.OrderByDescending(kv => kv.Value).Take(take)
                .Select(kv => (kv.Key == it.Id ? Loc.T("self_cov") : Lbl(items[kv.Key])) + " x" + kv.Value).ToArray());
        }

        // ---------- SCAN ----------

        void OnScan()
        {
            if (busy) return;
            List<string> roots = new List<string>();
            foreach (TextBox tb in new TextBox[] { tbA, tbB })
            {
                string t = tb.Text.Trim().Trim('"');
                if (t.Length == 0) continue;
                if (!Directory.Exists(t)) { Msg(Loc.F("msg_nofolder", t), "t_error", MessageBoxIcon.Warning); return; }
                roots.Add(Path.GetFullPath(t).TrimEnd(Path.DirectorySeparatorChar));
            }
            if (roots.Count == 0) { Msg(Loc.T("msg_need_folder"), "t_missing", MessageBoxIcon.Information); return; }
            if (roots.Count == 2)
            {
                if (string.Equals(roots[0], roots[1], StringComparison.OrdinalIgnoreCase))
                {
                    roots.RemoveAt(1);
                    Log(Loc.T("log_same"));
                }
                else if (IsInside(roots[0], roots[1]) || IsInside(roots[1], roots[0]))
                {
                    Msg(Loc.T("msg_nested"), "t_nested", MessageBoxIcon.Warning);
                    return;
                }
            }
            string cw = tbCw.Text.Trim().Trim('"');
            if (cw.Length == 0 || !File.Exists(Path.Combine(cw, "CodeWalker.Core.dll")))
            {
                Msg(Loc.T("msg_cwreq"), "t_cwreq", MessageBoxIcon.Information);
                return;
            }

            ScanArgs a = new ScanArgs();
            a.Roots = roots; a.CwFolder = cw; a.Eps = (double)nudEps.Value;
            cancel = false;
            items = new List<YmapItem>();
            lv.Items.Clear();
            log.Clear();
            SetBusy(true);
            Thread th = new Thread(delegate() { DoScan(a); });
            th.IsBackground = true;
            th.Start();
        }

        void DoScan(ScanArgs a)
        {
            try
            {
                Progress(0, 1, Loc.T("st_loading_cw"));
                CW.Init(a.CwFolder);
                Settings.Set("cw", a.CwFolder);

                // map labels (to tell apart two folders that have the same name)
                List<string> labels = new List<string>();
                for (int i = 0; i < a.Roots.Count; i++)
                {
                    string nm = new DirectoryInfo(a.Roots[i]).Name;
                    if (labels.Contains(nm, StringComparer.OrdinalIgnoreCase)) nm = nm + "_" + (i + 1);
                    labels.Add(nm);
                }

                List<KeyValuePair<int, string>> files = new List<KeyValuePair<int, string>>();
                for (int i = 0; i < a.Roots.Count; i++)
                {
                    List<string> fs = Directory.GetFiles(a.Roots[i], "*.ymap", SearchOption.AllDirectories)
                        .Where(f => f.EndsWith(".ymap", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
                    Log(Loc.F("log_found", labels[i], fs.Count));
                    foreach (string f in fs) files.Add(new KeyValuePair<int, string>(i, f));
                }
                if (files.Count == 0) { Log(Loc.T("log_none")); return; }

                List<YmapItem> loaded = new List<YmapItem>();
                int unreadable = 0;
                for (int n = 0; n < files.Count; n++)
                {
                    if (cancel) { Log(Loc.T("log_scan_stop")); return; }
                    int ri = files[n].Key;
                    string path = files[n].Value;
                    YmapItem it = new YmapItem();
                    it.Id = loaded.Count; it.Root = a.Roots[ri]; it.RootLabel = labels[ri]; it.FullPath = path;
                    it.RelPath = path.Substring(a.Roots[ri].Length).TrimStart(Path.DirectorySeparatorChar);
                    Progress(n, files.Count, Loc.F("st_reading", n + 1, files.Count, Lbl(it)));
                    try
                    {
                        object y = CW.Load(File.ReadAllBytes(path));
                        bool mlo;
                        it.Ents = CW.Entities(y, it.Id, out mlo);
                        foreach (Ent e in it.Ents) e.Handle = null;   // free memory
                        it.HasOtherContent = mlo || CW.HasOtherContent(y);
                        it.Readable = true;
                    }
                    catch (Exception ex)
                    {
                        unreadable++;
                        Log(Loc.F("log_unreadable", Lbl(it), ex.Message));
                    }
                    loaded.Add(it);
                }

                int totalEnts = loaded.Sum(i => i.Ents.Count);
                Log("");
                Log(Loc.F("log_total", loaded.Count, totalEnts) + (unreadable > 0 ? Loc.F("log_total_unread", unreadable) : ""));
                if (unreadable == loaded.Count)
                {
                    Log("");
                    Log(Loc.T("log_allfail1"));
                    Log(Loc.T("log_allfail2"));
                    items = loaded;
                    UI(delegate { FillList(); lblStatus.Text = Loc.T("st_read_fail"); });
                    return;
                }

                Progress(files.Count, files.Count, Loc.T("st_searching"));
                List<Ent> all = loaded.SelectMany(i => i.Ents).ToList();
                Engine.Dedupe(all, a.Eps, 1e-4, 0.01, loaded);
                foreach (YmapItem it in loaded)
                    it.DropCandidate = it.Readable && it.Ents.Count > 0 && it.Matched == it.Ents.Count && !it.HasOtherContent;

                items = loaded;
                UI(delegate { FillList(); });
                LogSummary();
                Progress(files.Count, files.Count, Loc.F("st_scan_done", Loc.T("btn_save")));
            }
            catch (Exception ex)
            {
                Log(Loc.F("log_error", ex.Message));
                UI(delegate { Msg(ex.Message, "t_error", MessageBoxIcon.Error); });
            }
            finally { SetBusy(false); }
        }

        void FillList()
        {
            lv.BeginUpdate();
            lv.Items.Clear();
            IEnumerable<YmapItem> ordered = items.OrderBy(i => (int)KindOf(i)).ThenByDescending(i => i.Removed)
                .ThenBy(i => Lbl(i), StringComparer.OrdinalIgnoreCase);
            foreach (YmapItem it in ordered)
            {
                Kind k = KindOf(it);
                ListViewItem li = new ListViewItem(new string[]
                {
                    Lbl(it), it.Ents.Count.ToString(), it.Matched.ToString(), it.Removed.ToString(),
                    KindText(k), CoveredText(it, 3)
                });
                if (k == Kind.Full) li.BackColor = Color.FromArgb(255, 205, 205);
                else if (k == Kind.FullOther) li.BackColor = Color.FromArgb(255, 225, 190);
                else if (k == Kind.Partial) li.BackColor = Color.FromArgb(255, 250, 205);
                else if (k == Kind.Unreadable) li.BackColor = Color.LightGray;
                lv.Items.Add(li);
            }
            lv.EndUpdate();
        }

        void LogSummary()
        {
            int matched = items.Sum(i => i.Matched), removed = items.Sum(i => i.Removed);
            Log(Loc.F("sum_overlap", matched, removed, matched - removed));

            List<YmapItem> full = items.Where(i => KindOf(i) == Kind.Full || KindOf(i) == Kind.FullOther).ToList();
            if (full.Count > 0)
            {
                Log("");
                Log(Loc.T("sum_full_hdr"));
                foreach (YmapItem it in full)
                {
                    Log("  " + Lbl(it));
                    Log("      -> " + CoveredText(it, 3));
                    if (it.HasOtherContent) Log(Loc.T("sum_other"));
                }
                Log(Loc.F("sum_drop_note", Loc.T("drop_dir")));
            }
            else Log(Loc.T("sum_none"));
        }

        // ---------- SAVE ----------

        void OnSave()
        {
            if (busy) return;
            if (items.Count == 0) { Msg(Loc.T("msg_scan_first"), "t_info", MessageBoxIcon.Information); return; }
            string outDir = tbOut.Text.Trim().Trim('"');
            if (outDir.Length == 0) { Msg(Loc.T("msg_out_first"), "t_missing", MessageBoxIcon.Information); return; }
            string outRoot = Path.GetFullPath(outDir).TrimEnd(Path.DirectorySeparatorChar);
            foreach (string root in items.Select(i => i.Root).Distinct())
            {
                if (string.Equals(root, outRoot, StringComparison.OrdinalIgnoreCase) || IsInside(root, outRoot) || IsInside(outRoot, root))
                {
                    Msg(Loc.T("msg_outbad"), "t_outbad", MessageBoxIcon.Warning);
                    return;
                }
            }

            bool separate = cbDrop.Checked;
            int toClean = items.Count(i => i.Readable && !i.DropCandidate && i.Removed > 0);
            int toDrop = separate ? items.Count(i => i.DropCandidate) : 0;
            int toCopy = items.Count - toClean - toDrop;
            StringBuilder sb = new StringBuilder();
            sb.Append(Loc.T("conf_head"));
            bool exists = false;
            foreach (string mapLabel in items.Select(i => i.RootLabel).Distinct())
            {
                string target = Path.Combine(outRoot, mapLabel);
                sb.Append("   " + target + "\r\n");
                if (Directory.Exists(target)) exists = true;
            }
            sb.Append("\r\n");
            sb.Append(Loc.F("conf_clean", toClean));
            if (separate) sb.Append(Loc.F("conf_drop", toDrop, Path.Combine(outRoot, Loc.T("drop_dir"))));
            sb.Append(Loc.F("conf_copy", toCopy));
            sb.Append(Loc.T("conf_struct"));
            if (exists) sb.Append(Loc.T("conf_exists"));
            sb.Append(Loc.T("conf_tail"));
            if (MessageBox.Show(this, sb.ToString(), Loc.T("t_save"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            cancel = false;
            SetBusy(true);
            Thread th = new Thread(delegate() { DoSave(outRoot, separate); });
            th.IsBackground = true;
            th.Start();
        }

        // Destination: <output>\<map folder name>\<original subfolders>\<same ymap name>
        string MakeDest(YmapItem it, string outRoot, bool separateDrops)
        {
            string baseDir = (it.DropCandidate && separateDrops) ? Path.Combine(outRoot, Loc.T("drop_dir")) : outRoot;
            return Path.Combine(baseDir, Path.Combine(it.RootLabel, it.RelPath));
        }

        void DoSave(string outRoot, bool separateDrops)
        {
            try
            {
                Log("");
                Log(Loc.F("log_saving", outRoot));
                Dictionary<int, string> dest = new Dictionary<int, string>();
                foreach (YmapItem it in items) dest[it.Id] = MakeDest(it, outRoot, separateDrops);

                int cleaned = 0, copied = 0, failed = 0, dropped = 0, n = 0;
                foreach (YmapItem it in items)
                {
                    if (cancel) { Log(Loc.T("log_save_stop")); break; }
                    n++;
                    Progress(n, items.Count, Loc.F("st_writing", n, items.Count, Lbl(it)));
                    string d = dest[it.Id];
                    Directory.CreateDirectory(Path.GetDirectoryName(d));

                    // Files that will not change are copied byte for byte (no needless re-saving)
                    if (it.DropCandidate) { File.Copy(it.FullPath, d, true); if (separateDrops) dropped++; else copied++; continue; }
                    if (!it.Readable || it.Removed == 0) { File.Copy(it.FullPath, d, true); copied++; continue; }

                    try
                    {
                        // Read the file fresh (so saving twice never removes twice)
                        object y = CW.Load(File.ReadAllBytes(it.FullPath));
                        bool mlo;
                        List<Ent> fresh = CW.Entities(y, it.Id, out mlo);
                        if (fresh.Count != it.Ents.Count) throw new Exception(Loc.T("err_changed"));
                        List<Ent> rem = new List<Ent>();
                        for (int i = 0; i < it.Ents.Count; i++) if (it.Ents[i].Removed) rem.Add(fresh[i]);
                        CW.Remove(y, rem);
                        byte[] data = CW.Save(y);

                        // Verification: re-read what was saved; does the prop count match?
                        int expected = it.Ents.Count - it.Removed;
                        int got = CW.Entities(CW.Load(data), it.Id, out mlo).Count;
                        if (got != expected) throw new Exception(Loc.F("err_verify", expected, got));

                        File.WriteAllBytes(d, data);
                        cleaned++;
                        Log(Loc.F("log_cleaned", Lbl(it), it.Removed));
                    }
                    catch (Exception ex)
                    {
                        File.Copy(it.FullPath, d, true);   // keep the original
                        failed++;
                        Log(Loc.F("log_savefail", Lbl(it), ex.Message));
                    }
                }

                WriteReports(outRoot);
                Log("");
                Log(Loc.F("log_done", cleaned, copied, Loc.T("drop_dir"), dropped, failed));
                Log(Loc.F("log_reports", Loc.T("csv_sum_name"), Loc.T("csv_det_name")));
                UI(delegate
                {
                    lblStatus.Text = Loc.F("st_done", outRoot);
                    MessageBox.Show(this, Loc.F("done_box", cleaned, copied, Loc.T("drop_dir"), dropped, failed, outRoot),
                        Loc.T("t_done"), MessageBoxButtons.OK, failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                });
            }
            catch (Exception ex)
            {
                Log(Loc.F("log_error", ex.Message));
                UI(delegate { Msg(ex.Message, "t_error", MessageBoxIcon.Error); });
            }
            finally { SetBusy(false); }
        }

        // ---------- REPORTS ----------

        void OnCsv()
        {
            if (busy || items.Count == 0) return;
            using (FolderBrowserDialog d = new FolderBrowserDialog())
            {
                d.Description = Loc.T("pick_csv");
                if (d.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    WriteReports(d.SelectedPath);
                    Log(Loc.F("log_csv_saved", d.SelectedPath));
                    Msg(Loc.F("msg_csv_saved", Loc.T("csv_sum_name"), Loc.T("csv_det_name")), "t_ok", MessageBoxIcon.Information);
                }
                catch (Exception ex) { Msg(ex.Message, "t_error", MessageBoxIcon.Error); }
            }
        }

        void WriteReports(string folder)
        {
            Directory.CreateDirectory(folder);
            UTF8Encoding utf8 = new UTF8Encoding(true);
            using (StreamWriter w = new StreamWriter(Path.Combine(folder, Loc.T("csv_sum_name")), false, utf8))
            {
                w.WriteLine(Loc.T("hdr_sum"));
                foreach (YmapItem it in items)
                {
                    double pct = it.Ents.Count == 0 ? 0 : 100.0 * it.Matched / it.Ents.Count;
                    w.WriteLine(string.Join(",", new string[] {
                        Q(Lbl(it)), it.Ents.Count.ToString(), it.Matched.ToString(), it.Removed.ToString(),
                        pct.ToString("0.0", CultureInfo.InvariantCulture), Q(KindText(KindOf(it))), Q(CoveredText(it, 3)) }));
                }
            }
            using (StreamWriter w = new StreamWriter(Path.Combine(folder, Loc.T("csv_det_name")), false, utf8))
            {
                w.WriteLine(Loc.T("hdr_det"));
                foreach (YmapItem it in items)
                    foreach (Ent e in it.Ents)
                    {
                        if (e.MatchedWith == null) continue;
                        Ent k = e.MatchedWith;
                        double dist = Math.Sqrt(Math.Pow(e.X - k.X, 2) + Math.Pow(e.Y - k.Y, 2) + Math.Pow(e.Z - k.Z, 2));
                        w.WriteLine(string.Join(",", new string[] {
                            Q(Lbl(it)), e.Index.ToString(), Q(Lbl(items[k.FileId])), k.Index.ToString(),
                            Q(e.Name), dist.ToString("0.0000", CultureInfo.InvariantCulture),
                            e.Removed ? Loc.T("act_remove") : Loc.T("act_lod") }));
                    }
            }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
