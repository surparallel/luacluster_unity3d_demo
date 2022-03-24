using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace grpania_unity3d_demo
{
    public class Tool
    {

        public static void Clear(UnityEngine.Object o, float time = 0)
        {
            if (o != null)
                GameObject.Destroy(o, time);
        }

        // public static string GetEncryptstr(string msg)
        // {
        //     return HttpEncrypt.GetEncryptstr(msg);
        // }

        public static Color GetColorByString(string color)
        {
            byte br = byte.Parse(color.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte bg = byte.Parse(color.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte bb = byte.Parse(color.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            byte cc = byte.Parse(color.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
            float r = br / 255f;
            float g = bg / 255f;
            float b = bb / 255f;
            float a = cc / 255f;
            return new Color(r, g, b, a);
        }

        public static string GetStringByColor(Color color)
        {
            int r = Mathf.RoundToInt(color.r * 255.0f);
            int g = Mathf.RoundToInt(color.g * 255.0f);
            int b = Mathf.RoundToInt(color.b * 255.0f);
            int a = Mathf.RoundToInt(color.a * 255.0f);
            string hex = string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", r, g, b, a);
            return hex;
        }

        public static Sprite CreateSprite(Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        public static string GetHashCode(string url)
        {
            return url.GetHashCode().ToString();
        }

        public static void OpenDir(string path)
        {
            path = path.Replace("/", @"\");
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "explorer.exe";
            p.StartInfo.Arguments = path;
            p.Start();
        }

        public static byte[] Bytes(byte[] buffer)
        {
            int len = buffer.Length;
            byte[] dst_buffer = new byte[len];
            for (int i = 0; i < len; i++)
            {
                byte t = buffer[i];
                dst_buffer[i] = (byte)(t ^ 0x99);
            }
            return dst_buffer;
        }

        public static byte[] Utf8ToByte(object utf8)
        {
            return System.Text.Encoding.UTF8.GetBytes((string)utf8);
        }

        public static string ByteToUtf8(byte[] bytes)
        {
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        public static string BytesToString(byte[] bytes)
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            return encoding.GetString(bytes);
        }

        public static byte[] StringToBytes(string str)
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            return encoding.GetBytes(str);
        }

        public static void ArrayCopy(byte[] srcdatas, long srcLen, KBEngine.MemoryStream ms, long dstLen, long len)
        {
            Array.Copy(srcdatas, srcLen, ms.data(), dstLen, len);
        }

        public static void ArrayAppend(KBEngine.MemoryStream ms, byte[] datas, UInt32 offset, UInt32 size)
        {
            ms.append(datas, offset, size);
        }

        //kbe--------------------------------------------------------------------
        public static void BundleBytes(KBEngine.MemoryStream ms, int length)
        {
            byte[] bs = ms.data();
            bs[2] = (Byte)(length & 0xff);
            bs[3] = (Byte)(length >> 8 & 0xff);
            ms.setData(bs);
        }

        //kbe--------------------------------------------------------------------

        //public static void WriteBlob(KBEngine.MemoryStream ms,string v)
        //{
        //    ms.writeBlob(Tool.Utf8ToByte(v));
        //}

        public static string Md5(string file)
        {
            try
            {
                FileStream fs = new FileStream(file, FileMode.Open);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(fs);
                fs.Close();

                string sb = "";
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb += retVal[i].ToString("x2");
                }
                return sb;
            }
            catch (Exception ex)
            {
                throw new Exception("md5 fail error=" + ex.Message);
            }
        }

        public static string Md5String(string value)
        {
            byte[] input = System.Text.Encoding.UTF8.GetBytes(value);
            byte[] hash = System.Security.Cryptography.MD5.Create().ComputeHash(input);
            string sb = "";
            int length = hash.Length;
            for (int i = 0; i < length; ++i)
            {
                sb += hash[i].ToString("x2");
            }
            return sb;
        }

        public static byte[] EncryptAES(string content)
        {
            byte[] data = Encoding.UTF8.GetBytes(content);
            byte[] key = Encoding.UTF8.GetBytes(Config.appName);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= key[i % key.Length];
            }
            return data;
        }

        public static string DecryptAES(byte[] data)
        {
            byte[] key = Encoding.UTF8.GetBytes(Config.appName);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= key[i % key.Length];
            }
            return Encoding.UTF8.GetString(data, 0, data.Length);
        }

        public static void Encode(string path)
        {
            if (!File.Exists(path))
                return;
            string msg = File.ReadAllText(path);
            File.WriteAllBytes(path, Tool.EncryptAES(msg));
        }

        public static void Decode(string path)
        {
            if (!File.Exists(path))
                return;
            byte[] msg = File.ReadAllBytes(path);
            File.WriteAllText(path, Tool.DecryptAES(msg));
        }

        public static string ReadFile(string path)
        {
            if (!File.Exists(path))
                return "";
            string msg = File.ReadAllText(path);
            return msg;
        }

        public static void WriteFile(string text, string path)
        {
            File.WriteAllText(path, text);
        }

        public static string[] GetExistsStreamAssetGame()
        {
            string[] games = null;
            if (Directory.Exists(Config.resPath))
            {
                games = Directory.GetDirectories(Config.resPath);
            }
            return games;
        }

        public static string GetGamePath(string path)
        {
            int index = path.LastIndexOf("ab/");
            //Debug.Log("i=" + index + "\n" + path);
            if (index > -1)
            {
                return path.Substring(index + 3);
            }
            return path;
        }

        public static int GetRandom(int min = 0, int max = 99999)
        {
            return UnityEngine.Random.Range(min, max);
        }

        public static int GetByteLength(byte[] by)
        {
            return by.Length;
        }

        // public static int GetTotalMilliseconds(DateTime dt)
        // {
        //     return (int)((DateTime.UtcNow - dt).TotalMilliseconds);
        // }

        public static int GetTime()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt32(ts.TotalSeconds);
        }

        public static string GetTimeString()
        {
            // return DateTime.Now.ToString("HH:mm:ss:ffff");
            return DateTime.Now.ToString("HH:mm:ss");
        }

        public static string ReplaceSizeTag(string msg)
        {
            Regex reg = new Regex("</?size=?\\d*>");
            return reg.Replace(msg, "");
        }

        public static void Log(object log)
        {
            if (Config.showLog)
            {
                string time = "[" + GetTimeString() + "]";
                string g = time + log.ToString();
                Debug.Log(g);
            }
        }
        public static void War(object log, string color = "#ffff00")
        {
            if (Config.showLog)
            {
                string time = "[" + GetTimeString() + "]";
                string g = time + "<color=" + color + ">" + log.ToString() + "</color>"; ;
                Debug.Log(g);
            }
        }

        public static void Err(object log, string color = "#ff0000")
        {
            if (Config.showLog)
            {
                string time = "[" + GetTimeString() + "]";
                string logx = time + log.ToString();
                string[] msg = logx.Split('\n');
                string m = "";
                for (int i = 0; i < msg.Length; i++)
                {
                    m += "<color=" + color + ">" + Tool.ReplaceSizeTag(msg[i]) + "</color>\n";
                }
                Debug.Log(m);
            }
        }

        //public static void LogErrorStack(object log)
        //{
        //    if (Config.showXrr)
        //    {
        //        string time = "[" + GetTimeString() + "]";
        //        string g = time + log.ToString();
        //        Debug.LogError(g);
        //    }
        //}

        // public static void DropDownAddOptions(Dropdown dd,string[] msg,bool isClear = true)
        // {
        //     List<string> list = new List<string>();
        //     list.AddRange(msg);
        //     if (isClear)
        //         dd.ClearOptions();
        //     dd.AddOptions(list);
        // }

        public static void SavePicture(Texture2D tex, string path, string type = "png")
        {
            if (File.Exists(path))
                File.Delete(path);
            try
            {
                FileStream newFs = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite);
                byte[] bytes = null;
                if (type == "png")
                    bytes = tex.EncodeToPNG();
                else
                    bytes = tex.EncodeToJPG();
                newFs.Write(bytes, 0, bytes.Length);
                newFs.Close();
                newFs.Dispose();
                Tool.Err("SavePicture ok path=" + path);
            }
            catch (Exception ex)
            {
                Tool.Err("SavePicture err path=" + path + " error=" + ex.Message);
            }
        }

        public static Application.LogCallback AddLog(Action<string, string, LogType> fun)
        {
            Application.LogCallback ac = new Application.LogCallback(fun);
            Application.logMessageReceived += ac;
            return ac;
        }

        public static void RemoveLog(Application.LogCallback ac)
        {
            if (ac != null)
                Application.logMessageReceived -= ac;
        }

        //public static Spine.AnimationState AddSpineCurrentEvent(GameObject go, Action fun)
        //{
        //    SkeletonGraphic sg = go.GetComponent<SkeletonGraphic>();
        //    //sg.Skeleton.FindSlot("");
        //    return sg.AnimationState.AddComplete(fun);
        //}

        //public static void RemoveSpineCurrentEvent(Spine.AnimationState asx)
        //{
        //    if (asx != null)
        //        asx.ClearComplete();
        //}

        //public static void AddSpineEvent(Spine.TrackEntry te, Action fun)
        //{
        //    if (te != null)
        //    {
        //        te.AddComplete(fun);
        //    }
        //}

        //public static void RemoveSpineEvent(Spine.TrackEntry te, Action fun)
        //{
        //    if (te != null)
        //    {
        //        te.ClearComplete();
        //    }
        //}

        //public static float GetAnimationLength(GameObject go, string name = "")
        //{
        //    AnimationClip[] clips = go.GetComponent<Animator>().runtimeAnimatorController.animationClips;
        //    if (clips.Length == 0)
        //        return 0;
        //    if (name == "")
        //        return clips[0].length;
        //    for (int i = 0; i < clips.Length; i++)
        //    {
        //        if (clips[i].name == name)
        //        {
        //            return clips[i].length;
        //        }
        //    }
        //    return 0;
        //}

        //public static void ReplaceSpineSlot(GameObject go, string slotName, string ab, string game)
        //{
        //    string[] names = ab.Split('.');
        //    Slot slot = go.GetComponent<SkeletonGraphic>().Skeleton.FindSlot(slotName);
        //    Sprite sp = LoadManager.inst.Load(LoadManager.SP, names[0], names[1], game) as Sprite;
        //    CreateRegionAttachmentByTexture(slot, sp.texture);
        //}

        //public static AtlasRegion CreateRegion(Texture2D texture)
        //{
        //    Spine.AtlasRegion region = new AtlasRegion();
        //    region.width = texture.width;
        //    region.height = texture.height;
        //    region.originalWidth = texture.width;
        //    region.originalHeight = texture.height;
        //    region.rotate = false;
        //    region.page = new AtlasPage();
        //    region.page.name = texture.name;
        //    region.page.width = texture.width;
        //    region.page.height = texture.height;
        //    region.page.uWrap = TextureWrap.ClampToEdge;
        //    region.page.vWrap = TextureWrap.ClampToEdge;
        //    return region;
        //}

        //public static Material CreateRegionAttachmentByTexture(Slot slot, Texture2D texture)
        //{
        //    if (slot == null) { Debug.Log("slot == null"); return null; }
        //    if (texture == null) { Debug.Log("texture == null"); return null; }

        //    RegionAttachment attachment = slot.Attachment as RegionAttachment;
        //    if (attachment == null) { Debug.Log("attachment == null"); return null; }

        //    attachment.RendererObject = CreateRegion(texture);
        //    attachment.SetUVs(0f, 1f, 1f, 0f, false);

        //    Material mat = new Material(Shader.Find("Sprites/Default"));
        //    mat.mainTexture = texture;
        //    (attachment.RendererObject as AtlasRegion).page.rendererObject = mat;

        //    slot.Attachment = attachment;
        //    //slot.Attachment.UpdateOffsetByTexture2D(attachment, texture);
        //    return mat;
        //}

        //public static Material CreateMeshAttachmentByTexture(Slot slot, Texture2D texture)
        //{
        //    if (slot == null) return null;
        //    MeshAttachment oldAtt = slot.Attachment as MeshAttachment;
        //    if (oldAtt == null || texture == null) return null;

        //    MeshAttachment att = new MeshAttachment(oldAtt.Name);
        //    att.RendererObject = CreateRegion(texture);
        //    att.Path = oldAtt.Path;

        //    att.Bones = oldAtt.Bones;
        //    att.Edges = oldAtt.Edges;
        //    att.Triangles = oldAtt.Triangles;
        //    att.Vertices = oldAtt.Vertices;
        //    att.WorldVerticesLength = oldAtt.WorldVerticesLength;
        //    att.HullLength = oldAtt.HullLength;
        //    att.RegionRotate = false;

        //    att.RegionU = 0f;
        //    att.RegionV = 1f;
        //    att.RegionU2 = 1f;
        //    att.RegionV2 = 0f;
        //    att.RegionUVs = oldAtt.RegionUVs;

        //    att.UpdateUVs();

        //    Material mat = new Material(Shader.Find("Sprites/Default"));
        //    mat.mainTexture = texture;
        //    (att.RendererObject as Spine.AtlasRegion).page.rendererObject = mat;
        //    slot.Attachment = att;
        //    return mat;
        //}

        public static void CreateDir(string path, bool isDelete = false)
        {
            if (isDelete && Directory.Exists(path))
                Directory.Delete(path, true);
            Directory.CreateDirectory(path);
        }

        public static void DeleteDir(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }

        public static void DeleteFile(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        public static void CopyFile(string path1, string path2)
        {
            if (File.Exists(path1))
            {
                File.Copy(path1, path2, true);
            }
        }

        public static void CopyDir(string srcPath, string aimPath)
        {
            try
            {
                // 检查目标目录是否以目录分割字符结束如果不是则添加
                if (aimPath[aimPath.Length - 1] != Path.DirectorySeparatorChar)
                {
                    aimPath += Path.DirectorySeparatorChar;
                    //aimPath += "/";
                }
                // 判断目标目录是否存在如果不存在则新建
                if (!Directory.Exists(aimPath))
                {
                    Directory.CreateDirectory(aimPath);
                }
                if (!Directory.Exists(srcPath))
                {
                    Tool.Err("srcPath no exists - " + srcPath);
                    return;
                }
                string[] fileList = Directory.GetFileSystemEntries(srcPath);
                // 遍历所有的文件和目录
                foreach (string file in fileList)
                {
                    if (Directory.Exists(file))
                    {
                        CopyDir(file, aimPath + Path.GetFileName(file));
                    }
                    else
                    {
                        File.Copy(file, aimPath + Path.GetFileName(file), true);
                    }
                }
            }
            catch (Exception ex)
            {
                Tool.Err("copydir - " + ex.Message);
            }
        }

        public static Vector2 GetPreferredSize(GameObject obj)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(obj.GetComponent<RectTransform>());
            return new Vector2(HandleSelfFittingAlongAxis(0, obj), HandleSelfFittingAlongAxis(1, obj));
        }

        private static float HandleSelfFittingAlongAxis(int axis, GameObject obj)
        {
            ContentSizeFitter.FitMode fitting = (axis == 0 ? obj.GetComponent<ContentSizeFitter>().horizontalFit : obj.GetComponent<ContentSizeFitter>().verticalFit);
            if (fitting == ContentSizeFitter.FitMode.MinSize)
            {
                return LayoutUtility.GetMinSize(obj.GetComponent<RectTransform>(), axis);
            }
            else
            {
                return LayoutUtility.GetPreferredSize(obj.GetComponent<RectTransform>(), axis);
            }
        }

        /// <summary>
        /// 获取鼠标下的T类型物体
        /// </summary>
        public static T GetComponentByRay<T>(Camera camera, Vector3 screenPoint, float rayDis, string layerName = "") where T : Component
        {
            T t = null;
            RaycastHit raycastHit;
            Ray ray = camera.ScreenPointToRay(screenPoint);
            bool isHit = false;
            if (layerName == "")
                isHit = Physics.Raycast(ray, out raycastHit, rayDis);
            else
                isHit = Physics.Raycast(ray, out raycastHit, rayDis, 1 << LayerMask.NameToLayer(layerName));
            if (isHit)
            {
                //t = GetRootComponent<T>(raycastHit.collider.gameObject.transform);
                t = raycastHit.collider.gameObject.transform.GetComponent<T>();
                if (t == null)
                {
                    t = raycastHit.collider.gameObject.transform.parent.GetComponent<T>();
                }
            }
            return t;
        }

        /// <summary>
        /// 获取鼠标下所有UI
        /// </summary>
        //public static List<GameObject> GetPointerUI()
        //{
        //    PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        //    eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        //    List<RaycastResult> list = new List<RaycastResult>();
        //    EventSystem.current.RaycastAll(eventDataCurrentPosition, list);

        //    List<GameObject> temp = new List<GameObject>();
        //    for (int i = 0; i < list.Count; i++)
        //    {
        //        //Debug.Log($"results [{i}] : {results[i].gameObject.name }");
        //        temp.Add(list[i].gameObject);
        //    }
        //    return temp;
        //}

        /// <summary>
        /// 获取鼠标下的世界坐标点
        /// </summary>
        //public static Vector3 GetScreenRayPoint(Vector3 screenPoint, float rayDis, string layerName = "")
        //{
        //    RaycastHit raycastHit;
        //    Ray ray = Camera.main.ScreenPointToRay(screenPoint);
        //    bool isHit;
        //    if (string.IsNullOrEmpty(layerName))
        //    {
        //        isHit = Physics.Raycast(ray, out raycastHit, rayDis);
        //    }
        //    else
        //    {
        //        isHit = Physics.Raycast(ray, out raycastHit, rayDis, 1 << LayerMask.NameToLayer(layerName));
        //    }

        //    if (isHit)
        //    {
        //        return raycastHit.point;
        //    }
        //    return Vector3.zero;
        //}
        //public static T GetRootComponent<T>(Transform transform) where T : Component
        //{
        //    T t = transform.GetComponent<T>();
        //    if (t != null)
        //    {
        //        return t;
        //    }
        //    else
        //    {
        //        return GetRootComponent<T>(transform.parent);
        //    }
        //}
        //public static T[] GetComponent<T>(Transform parent) where T : Component
        //{
        //    T[] ts = parent.GetComponentsInChildren<T>();
        //    return ts;
        //}

        /// <summary>
        /// 获取射线检测到的世界坐标点 
        /// </summary>
        public static Vector3 GetWorldPointByRay(Camera camera, Vector3 screenPoint, float rayDis, string layerName = "")
        {
            RaycastHit raycastHit;
            Ray ray = camera.ScreenPointToRay(screenPoint);
            bool isHit = false;
            if (layerName == "")
                isHit = Physics.Raycast(ray, out raycastHit, rayDis);
            else
                isHit = Physics.Raycast(ray, out raycastHit, rayDis, 1 << LayerMask.NameToLayer(layerName));

            if (isHit)
            {
                return raycastHit.point;
            }
            return new Vector3(0, 0, 0);
        }

        /// <summary>
        /// 获取射线检测到的游戏物体
        /// </summary>
        public static GameObject GetGameObjectByRay(Camera camera, Vector3 screenPoint, float rayDis, string layerName = "")
        {
            GameObject go = null;
            RaycastHit raycastHit;
            Ray ray = camera.ScreenPointToRay(screenPoint);
            bool isHit = false;
            if (layerName == "")
                isHit = Physics.Raycast(ray, out raycastHit, rayDis);
            else
                isHit = Physics.Raycast(ray, out raycastHit, rayDis, 1 << LayerMask.NameToLayer(layerName));
            if (isHit)
            {
                go = raycastHit.collider.gameObject;
            }
            return go;
        }

        //public static void FileWrite(string path, string s)  
        //{
        //    if (!File.Exists(path))
        //    {
        //        File.Create(path).Close();
        //    }
        //    File.WriteAllText(path, s);
        //}
        //public static string FileRead(string path) 
        //{
        //    if (!File.Exists(path)) return null;
        //    string str = File.ReadAllText(path, System.Text.Encoding.UTF8);
        //    //string[] strs = File.ReadAllLines(path, System.Text.Encoding.UTF8);
        //    return str;
        //}

        public static Animator[] GetChildAnimators(GameObject go)
        {
            Animator[] anis = go.transform.GetComponentsInChildren<Animator>();

            for (int i = 0; i < anis.Length; i++)
            {
                Debug.Log($"{go.name } -> {i} -> {anis[i].gameObject.name }");
            }
            return anis;
        }

        public static void BytesToFile(string path, byte[] bytes)
        {
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                fs.Write(bytes, 0, bytes.Length);
            }
        }

        public static byte[] FileToBytes(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read))
            {
                byte[] buffur = new byte[fs.Length];
                fs.Read(buffur, 0, buffur.Length);
                return buffur;
            }
        }

        private static DateTime timeStampStartTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long TimeStamp()
        {
            return (long)(DateTime.Now.ToUniversalTime() - timeStampStartTime).TotalSeconds;
        }
    }
}