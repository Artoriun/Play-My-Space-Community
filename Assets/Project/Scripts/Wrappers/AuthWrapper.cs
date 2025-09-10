namespace PlayMySpace.PMSC.Wrappers
{
    using System;
    using System.Threading.Tasks;
    using Framework.Patterns;
    using UnityEngine;
    using Google;
    //using Firebase.Auth;

    /// <summary>
    /// AuthWrapper.cs
    /// 
    /// Takes care of authenticating the user via Google SignIn.
    /// 
    /// by Peter de Keijzer
    /// </summary>
    public class AuthWrapper : PersistentSingleton<AuthWrapper>
    {
        #region Class Members
        [Header("Google SignIn")]
        [SerializeField] private string webClientId;
        
        private Action onAuthenticated;
        private string userId;

        // Google SignIn
        GoogleSignInConfiguration googleSignInConfiguration;
        #endregion

        #region Class Accessors
        public string UserId
        {
            get
            {
                if (Application.isEditor)
                {
                    return "12345";
                }
                else
                {
                    return userId;
                }
            }
        }
        #endregion

        #region MonoBehaviour Stuff
        protected override void Awake()
        {
            base.Awake();
            ConfigureGoogleSignIn();
        }
        #endregion

        #region Class Implementation
        private void ConfigureGoogleSignIn()
        {
            googleSignInConfiguration = new GoogleSignInConfiguration
            {
                WebClientId = webClientId,
                RequestIdToken = true
            };
        }

        public void OnGoogleSignIn()
        {
            GoogleSignIn.Configuration = googleSignInConfiguration;
            GoogleSignIn.Configuration.UseGameSignIn = false;
            GoogleSignIn.Configuration.RequestIdToken = true;

            Task<GoogleSignInUser> signIn = GoogleSignIn.DefaultInstance.SignIn();
            TaskScheduler taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            //TaskCompletionSource<FirebaseUser> onSignIn = new TaskCompletionSource<FirebaseUser>();

            signIn.ContinueWith(task =>
            {
                if (task.IsCanceled)
                {
                    //onSignIn.SetCanceled();
                    Debug.Log("Google SignIn canceled");
                }
                else if (task.IsFaulted)
                {
                    //onSignIn.SetException(task.Exception);
                    Debug.Log("Google SignIn exception: " + task.Exception.ToString());
                }
                else
                {
                    Debug.Log("User ID: " + task.Result.UserId);
                    userId = task.Result.UserId;

                    //Credential credential = GoogleAuthProvider.GetCredential(task.Result.IdToken, null);

                    //FirebaseAuth.DefaultInstance.SignInWithCredentialAsync(credential).ContinueWith(authTask =>
                    //{
                    //    if (authTask.IsCanceled)
                    //    {
                    //        onSignIn.SetCanceled();
                    //        Debug.Log("Firebase sign in canceled");
                    //    }
                    //    else if (authTask.IsFaulted)
                    //    {
                    //        onSignIn.SetException(authTask.Exception);
                    //        Debug.Log("Firebase sign in exception: " + authTask.Exception.ToString());
                    //    }
                    //    else
                    //    {
                    //        onSignIn.SetResult(authTask.Result);
                    //        Debug.Log("Firebase sign in successful. UserId: " + onSignIn.Task.Result.UserId);

                    //        if (callbacksWhenAuthenticated != null)
                    //        {
                    //            callbacksWhenAuthenticated.Invoke();
                    //        }
                    //    }
                    //});

                    onAuthenticated?.Invoke();
                }
            }, taskScheduler);
        }

        public void AddCallback(Action callback)
        {
            if (callback != null)
            {
                onAuthenticated += callback;
            }
            else
            {
                Debug.LogError(GetType().Name + ".AddCallback(Action callback): callback is null!");
            }
        }

        public void InvokeAuthenticatedCallbacks()
        {
            onAuthenticated?.Invoke();
        }
        #endregion
    }
}