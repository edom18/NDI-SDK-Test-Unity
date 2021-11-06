namespace NDIPlugin
{
    public class WifiManager
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _nsdManager;
#endif

        private static WifiManager _instance = new WifiManager();

        public static WifiManager Instance => _instance;

        private WifiManager()
        {
        }

        public void SetupNetwork()
        {
            // The NDI SDK for Android uses NsdManager to search for NDI video sources on the local network.
            // So we need to create and maintain an instance of NSDManager before performing Find, Send and Recv operations.
#if UNITY_ANDROID && !UNITY_EDITOR
            using (AndroidJavaObject activity = new AndroidJavaObject("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
            {
                using (AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext"))
                {
                    using (AndroidJavaObject nsdManager = context.Call<AndroidJavaObject>("getSystemService", "servicediscovery"))
                    {
                        _nsdManager = nsdManager;
                    }
                }
            }
#endif
        }
    }
}