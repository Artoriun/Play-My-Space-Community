namespace Framework.Firestore.Wrappers
{
    using System;
    using UnityEngine;
    using Framework.Patterns;

    /// <summary>
    /// DataWrapper.cs
    /// 
    /// A generic Singleton wrapper class serving as the base for any wrapper class that needs to communicate
    /// with the Firestore database.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    /// <typeparam name="T">The child wrapper that needs to communicate with the Firebase database.</typeparam>
    public class DataWrapper<MB, M> : PersistentSingleton<MB> where MB : MonoBehaviour
    {
        #region Class Members
        #endregion

        #region MonoBehaviour Stuff
        #endregion

        #region Class Implementation - Private

        #endregion

        #region Class Implementation - Public
        public virtual void GetData(Action<M> successCallback, Action<string> errorCallback)
        {
        }

        public virtual void PostData(M data, Action<M> successCallback, Action<string> errorCallback)
        {
        }
        #endregion
    }
}
