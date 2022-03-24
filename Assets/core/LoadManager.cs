using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace grpania_unity3d_demo
{
    public class LoadManager : MonoBehaviour
    {
        private static LoadManager instance;

        private Dictionary<string, AssetBundleManifest> _manifest;
        private Dictionary<string, AssetBundle> _abs;
        private Dictionary<string, Dictionary<string, object>> _res;
        private List<string> _del;
        //private Queue<LoadABItem> que = new Queue<LoadABItem>();
        //private bool isQue = false;

        public static string TXT = "text";
        public static string AC = "audio";
        public static string VC = "video";
        public static string MAT = "mat";
        public static string SP = "sprite";
        public static string OBJ = "object";
        public static string SHA = "shader";
        public static string SCENE = "scene";
        public static string FGUI = "fgui";

        public LoadManager()
        {
            _manifest = new Dictionary<string, AssetBundleManifest>();
            _abs = new Dictionary<string, AssetBundle>();
            _res = new Dictionary<string, Dictionary<string, object>>();
            _del = new List<string>();
        }

        public static LoadManager inst
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameObject("ab").AddComponent<LoadManager>();
                    instance.gameObject.name = "ab";
                    instance.gameObject.transform.parent = GameObject.Find("main").transform;
                    GameObject.DontDestroyOnLoad(instance.gameObject);
                }
                //instance = new LoadManager();
                return instance;
            }
        }

        public void Log()
        {
            Tool.Err("_manifest.len = " + _manifest.Keys.Count);
            Tool.Err("_abs.len = " + _abs.Keys.Count);
            Tool.Err("_res.len = " + _res.Keys.Count);
            foreach(string i in _res.Keys)
                Tool.Err("_res name="+i);
            Tool.Err("_del.len = " + _del.Count);
        }

        public void Update()
        {
            lock (_del)
            {
                int len = _del.Count;
                while (len > 0)
                {
                    string ab = _del[0];
                    _del.RemoveAt(0);
                    this.RemoveRes(ab);
                    _abs[ab].Unload(true);
                    _abs.Remove(ab);
                    len--;
                    //Tools.LogError("remove ab - " + ri.b);
                }
            }
        }

        public void ClearGame(string game)
        {
            // Tools.LogError("ClearGame _del.abs.count = "+_abs.Keys.Count + " " + game);
            // Tools.LogError("ClearGame game = " + game + " abs.len =" + _abs.Keys.Count);
            string[] msg;
            string ab;
            foreach (string g in _abs.Keys)
            {
                msg = g.Replace(Config.fileExt, "").Split('_');
                // ab = msg[msg.Length - 1];
                ab = msg[0];
                // Tools.LogError("ClearGame msg[0] = " + ab);
                if (ab == game)
                {
                    Tool.Err("ClearGame _del.add = "+g);
                    _del.Add(g);
                }
            }
        }

        public void RemoveRes(string ab)
        {
            // ab = game + "_" + ab.ToLower() + Config.fileExt;
            // Tools.LogError("RemoveRes ab = " + ab);
            if (_res.ContainsKey(ab))
            {
                Dictionary<string, object> res = _res[ab];
                foreach (UnityEngine.Object o in res.Values)
                    GameObject.DestroyImmediate(o,true);
                _res.Remove(ab);
                // Tools.LogError("remove res - " + ab);
            }
        }

        public void AddAB(string ab, AssetBundle abs,LoadABItem item = null)
        {
            // Tools.LogError("AddAB ab = " + ab);
            if (!_abs.ContainsKey(ab))
            {
                _abs.Add(ab, abs);

                if (item != null)
                {
                    UnityEngine.Object[] objs = abs.LoadAllAssets();
                    foreach (UnityEngine.Object o in objs)
                    {
                        // Tools.LogError("AddRes name = " + o.name);
                       this.AddRes(item.sb, o.name, o, item.game);
                    }
                }
            }
            else
            {
                Tool.Err("error add ab=" + ab);
            }
        }

        public void RemoveAB(string ab, string game)
        {
            ab = game + "_" + ab.ToLower() + Config.fileExt;
            if (_abs.ContainsKey(ab))
            {
                this.RemoveRes(ab);
                _abs[ab].Unload(true);
                _abs.Remove(ab);
            }
        }

        public bool IsExistsAB(string ab)
        {
            return _abs.ContainsKey(ab);
        }

        public void AddRes(string ab, string name, object obj, string game)
        {
            ab = game + "_" + ab.ToLower() + Config.fileExt;
            // Tools.LogError("AddRes ab = "+ab + " name = " + obj.name);
            if (_res.ContainsKey(ab))
            {
                _res[ab][name] = obj;
            }
            else
            {
                _res[ab] = new Dictionary<string, object>();
                _res[ab][name] = obj;
            }
        }

        public object GetRes(string ab, string name, string game)
        {
            ab = game + "_" + ab.ToLower() + Config.fileExt;

            if (_res.ContainsKey(ab) && _res[ab].ContainsKey(name))
            {
                return _res[ab][name];
            }
          
            return null;
        }

        public void ChangeSceneAsync(string ab, string name, Action fun, string game = "base")
        {
#if G
            this.LoadAsync(SCENE, ab, name, (x) =>
            {
                this.StartCoroutine(LoadSceneAsync(name, fun));
            }, game);
#else
            this.StartCoroutine(LoadSceneAsync(name, fun));
#endif
        }

        public void ChangeSceneSync(string ab, string name, string game = "base")
        {
#if G
            this.Load(SCENE, ab, name, game);
#else

#endif
            SceneManager.LoadScene(name);
        }

        private IEnumerator LoadSceneAsync(string name, Action fun)
        {
            AsyncOperation ao = SceneManager.LoadSceneAsync(name);
            ao.allowSceneActivation = false;
            while (!ao.isDone)
            {
                if (ao.progress >= 0.9f)
                {
                    ao.allowSceneActivation = true;
                    if (fun != null)
                        fun();
                    yield break;
                }
                yield return null;
            }
            //yield return ao;
            //if (ao.isDone)
            //{
            //    if (fun != null)
            //        fun();
            //}
        }

        public void AddManifest(string name)
        {
#if G
            //Debug.Log(Config.path + game + "/" + game);
            AssetBundle ab = AssetBundle.LoadFromFile(Config.path + name + "/" + name);
            // AssetBundle ab = AssetBundle.LoadFromFile(Config.path + name);
            if (ab != null)
            {
                AssetBundleManifest manifest = ab.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                ab.Unload(false);
                if (!_manifest.ContainsKey(name))
                    _manifest.Add(name, manifest);
            }
#endif
        }

        public void ClearManifest()
        {
#if G
            _manifest = new Dictionary<string, AssetBundleManifest>();
#endif
        }

        private string[] GetBundleDependencies(string ab, string game = "base")
        {
            //Tools.LogError("GetBundleDependencies - ab=" + ab + " game=" + game);
            // game = "ab";
            if (!_manifest.ContainsKey(game))
            {
                Tool.Err("GetBundleDependencies not contain game=" + game);
                return new string[] { };
            }
            AssetBundleManifest manifest = _manifest[game];

            string[] array = manifest.GetAllDependencies(ab);
            List<string> dependnces = new List<string>();
            if (array != null && array.Length > 0)
            {
                foreach (string item in array)
                {
                    if (!dependnces.Contains(item) && !this.IsExistsAB(item))
                    {
                        dependnces.Add(item);

                        //string[] arr = this.GetBundleDependencies(item, game);
                        //if (arr != null && arr.Length > 0)
                        //{
                        //    foreach (string t in arr)
                        //    {
                        //        if (!dependnces.Contains(t) && !this.IsExistsAB(item))
                        //            dependnces.Add(t);
                        //    }
                        //}
                    }
                }
            }
            if (dependnces.Count > 0)
                return dependnces.ToArray();
            return null;
        }

        private string GetGameByBundle(string name)
        {
            string[] names = name.Split('_');
            return names[0];
        }

        //private void CheckQueue()
        //{
        //    if(que.Count > 0)
        //    {
        //        LoadABItem item = que.Dequeue();
        //        if(item.type == 0)
        //        {

        //        }
        //        else
        //        {
        //            //this.isQue = false;
        //            this._LoadAsyncEx(item);
        //        }
        //    }
        //    else
        //    {
        //        this.isQue = false;
        //    }
        //}

        public void _LoadAsync(string ab, Action<AssetBundle> fun = null, string game = "base",bool isAll = false)
        {
#if G
            string sb = ab;
            ab = ab.ToLower();
            ab = game + "_" + ab.ToLower() + Config.fileExt;

            if (_abs.ContainsKey(ab))
            {
                if(fun != null)
                    fun(_abs[ab]);
                return;
            }

            LoadABItem item = new LoadABItem();
            item.sb = sb;
            item.ab = ab;
            item.game = game;
            item.type = 1;
            item.fun = fun;
            //if (this.isQue)
            //{
            //    que.Enqueue(item);
            //}
            //else
            //{
            //    this.isQue = true;
            this._LoadAsyncEx(item,isAll);
            //}
#else
            if(fun != null)
                fun(null);
#endif
        }

        private void _LoadAsyncEx(LoadABItem item,bool isAll = false)
        {
            string ab = item.ab;
            Action<AssetBundle> fun = item.fun;
            string game = item.game;

            string[] deps = this.GetBundleDependencies(ab, game);
            string path = "";
            List<LoadItem> list = new List<LoadItem>();
            if (deps != null)
            {
                foreach (string bundle in deps)
                {
                    if (this.IsExistsAB(bundle) || bundle == ab)
                        continue;
                    path = Config.path + this.GetGameByBundle(bundle) + "/" + bundle;

                    LoadItem li = new LoadItem();
                    li.path = path;
                    li.ab = bundle;
                    li.game = game;
                    li.fun = fun;
                    list.Add(li);
                }
            }
            if (!this.IsExistsAB(ab))
            {
                path = Config.path + this.GetGameByBundle(ab) + "/" + ab;
                LoadItem lx = new LoadItem();
                lx.path = path;
                lx.ab = ab;
                lx.game = game;
                lx.fun = fun;
                list.Add(lx);
            }
            foreach (LoadItem ll in list)
            {
                Tool.Err("_LoadAsync ab=" + ll.path);
            }
            if (list.Count > 0)
            {
                //Tools.LogError("list LoadItemList.inst.Load - " + list.Count.ToString());
                LoadItemList.inst.Load(list, (x) =>
                {
                    //Tools.LogError("x LoadItemList.inst.Load - " + x.Count.ToString());
                    foreach (LoadCom ax in x)
                    {
                        if (ax.ab != null)
                        {
                            if (isAll)
                                this.AddAB(ax.name, ax.ab, item);
                            else
                                this.AddAB(ax.name, ax.ab);
                        }
                    }
                    if (fun != null)
                    {
                        fun(_abs[ab]);
                        //this.CheckQueue();
                    }
                });
            }
        }

        public AssetBundle _LoadSync(string ab, string game = "base",bool isAll = false)
        {
#if G
            string sb = ab;
            ab = ab.ToLower();
            ab = game + "_" + ab + Config.fileExt;
            //Tools.LogError("_LoadSync ab=" + ab + " game=" + game);

            if (_abs.ContainsKey(ab))
            {
                return _abs[ab];
            }

            LoadABItem item = new LoadABItem();
            item.sb = sb;
            item.ab = ab;
            item.game = game;
            item.type = 0;
            item.fun = null;
            //if (que.Count > 0)
            //{
            //    que.Enqueue(item);
            //}
            //else
            //{
            return this._LoadSyncEx(item,isAll);
            //}
#else
            return null;
#endif
        }

        private AssetBundle _LoadSyncEx(LoadABItem item,bool isAll = false)
        {
            string ab = item.ab;
            string game = item.game;

            string[] deps = this.GetBundleDependencies(ab, game);
            string path = Config.path + game + "/" + ab;
            AssetBundle asset = null;
            if (deps != null)
            {
                foreach (string bundle in deps)
                {
                    if (this.IsExistsAB(bundle) || bundle == ab)
                        continue;
                    path = Config.path + this.GetGameByBundle(bundle) + "/" + bundle;
                    asset = AssetBundle.LoadFromFile(path);
                    if (asset != null)
                    {
                        if (isAll)
                            this.AddAB(bundle, asset, item);
                        else
                            this.AddAB(bundle, asset);
                    }
                }
            }
            if (!this.IsExistsAB(ab))
            {
                path = Config.path + this.GetGameByBundle(ab) + "/" + ab;
                Tool.Err("_LoadSync ab=" + path);
                asset = AssetBundle.LoadFromFile(path);
                if (asset != null)
                {
                    if (isAll)
                        this.AddAB(ab, asset, item);
                    else
                        this.AddAB(ab, asset);
                }
            }
            return asset;
        }
        
        private object LoadEditor(string type, string ab, string name, Action<object> fun, string game = "base")
        {
#if UNITY_EDITOR
            string path = "";
            if (type == SP)
            {
                path = this.GetLocalPath(@"/image/" + ab + "/", name, "*.png", game);
                UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(path);
                Sprite sp = null;
                if (obj != null && obj.GetType() == typeof(Texture2D))
                {
                    Texture2D texture = obj as Texture2D;
                    sp = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
                }
                else
                {
                    sp = obj as Sprite;
                }
                this.AddRes(ab, name, sp, game);
                if (fun != null)
                    fun(sp);
                else
                    return sp;
            }
            else if (type == AC)
            {
                path = this.GetLocalPath(@"/audio/" + ab + "/", name, "*.mp3", game);
                AudioClip ac = AssetDatabase.LoadMainAssetAtPath(path) as AudioClip;
                this.AddRes(ab, name, ac, game);
                if (fun != null)
                    fun(ac);
                else
                    return ac;
            }
            else if (type == VC)
            {
                path = this.GetLocalPath(@"/video/" + ab + "/", name, "*.ogv", game);
                VideoClip vc = AssetDatabase.LoadMainAssetAtPath(path) as VideoClip;
                this.AddRes(ab, name, vc, game);
                if (fun != null)
                    fun(vc);
                else
                    return vc;
            }
            else if (type == MAT)
            {
                path = this.GetLocalPath(@"/mat/" + ab + "/", name, "*.mat", game);
                Material mat = AssetDatabase.LoadMainAssetAtPath(path) as Material;
                this.AddRes(ab, name, mat, game);
                if (fun != null)
                    fun(mat);
                else
                    return mat;
            }
            else if (type == OBJ)
            {
                path = this.GetLocalPath(@"/ui/" + ab + "/", name, "*.prefab", game);
                UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(path) as UnityEngine.Object;
                this.AddRes(ab, name, obj, game);
                if (fun != null)
                    fun(obj);
                else
                    return obj;
            }
            else if (type == SHA)
            {
                path = this.GetLocalPath(@"/shader/" + ab + "/", name, "*.shader", game);
                UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(path) as UnityEngine.Object;
                this.AddRes(ab, name, obj, game);
                if (fun != null)
                    fun(obj);
                else
                    return obj;
            }
            else if (type == TXT)
            {
                path = this.GetLocalPath(@"/data/" + ab + "/", name, "*.json", game);
                UnityEngine.TextAsset obj = AssetDatabase.LoadMainAssetAtPath(path) as UnityEngine.TextAsset;
                this.AddRes(ab, name, obj.text, game);
                if (fun != null)
                    fun(obj.text);
                else
                    return obj.text;
            }
            else if (type == SCENE)
            {
                path = this.GetLocalPath(@"/scene/" + ab + "/", name, "*.unity", game);
                UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(path);
                if (fun != null)
                    fun(obj);
                else
                    return obj;
            }
#endif
            return null;
        }

        private string GetLocalPath(string ab, string assetName, string assetType, string game = "base")
        {
            string fullPath = "Res/ab/" + game + ab;
            //Debug.Log("GetLocalPath = " + fullPath);
            string[] files = Directory.GetFiles(Application.dataPath + "/" + fullPath, "*.*", SearchOption.AllDirectories);
            assetType = assetType.Replace("*", string.Empty);
            string path = null;
            foreach (string item in files)
            {
                //Debug.Log("path - " + item + "|" + Path.GetFileName(item) + "-" + assetName + assetType);
                if (Path.GetExtension(item) == ".meta")
                    continue;
                if (Path.GetFileName(item).ToLower() == (assetName + assetType).ToLower())
                {
                    path = item;
                    break;
                }
                if (assetType == ".mp3" && Path.GetFileName(item) == assetName + ".wav")
                {
                    path = item;
                    break;
                }
                if (assetType == ".mp3" && Path.GetFileName(item) == assetName + ".ogg")
                {
                    path = item;
                    break;
                }
                if (assetType == ".png" && Path.GetFileName(item) == assetName + ".jpg")
                {
                    path = item;
                    break;
                }
            }
            if (path != null)
                path = "Assets/" + path.Replace(Application.dataPath + @"/", string.Empty);
            //Tools.Log("GetLoadAssetPath - " + fullPath + " path-" + path);
            return path;
        }
    }

    public class LoadItemList : MonoBehaviour
    {
        private static LoadItemList instance;

        private List<LoadItem> list;
        private List<LoadCom> abs;
        private Action<List<LoadCom>> abFun;

        public LoadItemList()
        {
            abs = new List<LoadCom>();
        }

        public static LoadItemList inst
        {
            get
            {
                instance = new GameObject("load_item").AddComponent<LoadItemList>();
                instance.gameObject.name = "ab_" + Tool.GetRandom();
                GameObject.DontDestroyOnLoad(instance.gameObject);
                return instance;
            }
        }

        public void Load(List<LoadItem> list, Action<List<LoadCom>> fun)
        {
            this.list = list;
            this.abFun = fun;
            this.StartCoroutine(LoadItem());
        }

        private System.Collections.IEnumerator LoadItem()
        {
            while (list.Count > 0)
            {
                LoadItem li = list[0];
                list.RemoveAt(0);
                //Tools.LogError("async - ab=" + li.ab + " path=" + li.path);
                if (!LoadManager.inst.IsExistsAB(li.ab))
                {
                    AssetBundleCreateRequest abcr = AssetBundle.LoadFromFileAsync(li.path);
                    yield return abcr;
                    //Tools.LogError("async add ab - " + li.ab);
                    LoadCom lc = new LoadCom();
                    lc.name = li.ab;
                    lc.ab = abcr.assetBundle;
                    this.abs.Add(lc);
                }
            }
            yield return null;
            this.abFun(this.abs);
            GameObject.Destroy(this.gameObject);
        }
    }

    public class LoadCom
    {
        public string name;
        public AssetBundle ab;
    }

    public class LoadItem
    {
        public string path;
        public string ab;
        public string game;
        public Action<AssetBundle> fun;
    }

    public class RemoveItem
    {
        public string ab;
        public string k;

        public RemoveItem(string ab, string k)
        {
            this.ab = ab;
            this.k = k;
        }
    }

    public class LoadABItem
    {
        public string sb;
        public string ab;
        public string game;
        public Action<AssetBundle> fun;
        public int type = 0;//0同步 1异步
    }
}
