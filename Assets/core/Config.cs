using UnityEngine;

namespace grpania_unity3d_demo
{
    public class Config
    {
        public static bool appUpdateMode = false;
        public static bool resUpdateMode = false;
        public static bool showLog = true;
        public static bool showXrr = true;

        public static string[] games = new string[] { "base,1.001" };
        public const string version = "1";
        public const string packageName = "com.coolplaystore.island";
        public const string fileExt = ".mh";
        public const string name = "grpania_unity3d_demo";
        public const string appName = "grpania_unity3d_demo";
        public const string privateKey = "3c6e0b8a9c15224a8228b9a98ca1531d";
        public const string httpVerify = "mohe!#$#!2017#$!$#appauthprivatekey";
        public const string assetBundleName = "ab";
        public const string keyaliasName = "grpania_unity3d_demo";
        public const string keystorePass = "grpania_unity3d_demo";
        public const string type = "ios";//ios app

        public static Vector2 barSize = new Vector2(845, 15);
        public static string userId = "";
        public static string cdn_url = "";
        public static string icon = "image/default";
        public static Color color = Color.white;
        public static string cachePath = Config.persistentDataPath + "cache";

        public Config()
        {

        }

        public static string GetVersion(string game)
        {
            return PlayerPrefs.GetString("v_" + game, "0");
        }

        public static void SetVersion(string game, string version)
        {
            Tool.Err("SetVersion game=" + game + " version=" + version);
            PlayerPrefs.SetString("v_" + game, version);
        }

        public static string persistentDataPath
        {
            get { return Application.persistentDataPath + "/"; }
        }

        public static string streamingAssetsPath
        {
            get { return Application.streamingAssetsPath + "/"; }
        }

        public static string editorPath
        {
            get { return Application.dataPath + "/Res/ab/"; }
        }

        public static string path
        {
            get
            {
#if G
                return Application.persistentDataPath + "/" + assetBundleName + "/";
#else
                return Application.streamingAssetsPath + "/" + assetBundleName + "/";
#endif
            }
        }

        public static string dataPath
        {
            get
            {
                return Application.persistentDataPath + "/" + assetBundleName + "/";
            }
        }

        public static string resPath
        {
            get
            {
                return Application.streamingAssetsPath + "/" + assetBundleName + "/";
            }
        }

        public static string platform
        {
            get
            {
                if (Application.platform == RuntimePlatform.Android)
                    return "and";
                else if (Application.platform == RuntimePlatform.IPhonePlayer)
                    return "ios";
                else
                    return "and";
            }
        }
    }

    public class UpdateGameItem
    {
        public string filesURL;
        public string baseURL;
        public string game;
        public string curVersion;
        public bool isBase;
    }

    public class UpdateFileItem
    {
        public string localFile;
        public string remoteFile;
        public int fileSize;
    }

    public class FileJsonItem
    {
        public string path;
        public string md5;
        public int size;
    }
}