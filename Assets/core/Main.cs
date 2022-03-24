using UnityEngine;
using KBEngine;
using Unity.Mathematics;

namespace grpania_unity3d_demo
{
    public class Main : MonoBehaviour
    {
        public static NetworkInterface _networkInterface = null;
        public GameObject player = null;
        public GameEntity player_entity = null;

        public void ConnectCallback(string ip, int port, bool success, object userData){

            Debug.LogFormat("{0}, {1}, {2}", ip, port, success);
        
        }

        private void Awake()
        {
            _networkInterface = new NetworkInterface();
            _networkInterface.connectTo("127.0.0.1", 9577, ConnectCallback, this);

            player =  GameObject.Find("player");
            player_entity = player.GetComponent<GameEntity>();
        }

        void Start()
        {
            GameObject.DontDestroyOnLoad(this.gameObject);
        }

        void FixedUpdate()
        {
            TimerManager.inst.FixedUpdate();
            this.process();
        }

        private void SetInstallGame(string game)
        {
            PlayerPrefs.SetInt("exe_" + game, 1);
        }

        public virtual void process()
        {
            if(_networkInterface != null)
            {
                _networkInterface.process(this);
            }
        }
    }
}