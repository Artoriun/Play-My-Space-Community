namespace PlayMySpace.PMSC.Network
{
    using System.Collections.Generic;
    using UnityEngine;
    using MLAPI;

    public class NetworkCommandLine : MonoBehaviour
    {
        #region Class Members
        [SerializeField] private NetworkManager networkManager;
        #endregion

        #region MonoBehaviour Stuff
        private void Start()
        {
            if (Application.isEditor)
            {
                return;
            }

            var args = GetCommandLineArgs();

            if (args.TryGetValue("-mlapi", out string value))
            {
                switch (value)
                {
                    case "server":
                        networkManager.StartServer();
                        break;
                    case "host":
                        networkManager.StartHost();
                        break;
                    case "client":
                        networkManager.StartClient();
                        break;
                }
            }
        }
        #endregion

        #region Class Implementation - Private
        private Dictionary<string, string> GetCommandLineArgs()
        {
            var argsDictionary = new Dictionary<string, string>();
            string[] args = System.Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower();

                if (arg.StartsWith("-"))
                {
                    string value = i < args.Length - 1 ? args[i + 1].ToLower() : null;
                    value = (value?.StartsWith("-") ?? false) ? null : value;

                    argsDictionary.Add(arg, value);
                }
            }

            return argsDictionary;
        }
        #endregion
    }
}
