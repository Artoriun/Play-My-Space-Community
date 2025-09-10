namespace PlayMySpace.PMSC.Wrappers
{
    using System;
    using System.ComponentModel;
    using UnityEngine;
    using Framework.Patterns;
    using FMODUnity;

    /// <summary>
    /// SoundWrapper.cs
    /// 
    /// Handles all audio-related logic for the game.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class SoundWrapper : PersistentSingleton<SoundWrapper>
    {
        #region Class Members
        [HideInInspector] public enum SoundEffect { [Description("event:/UI/Okay")] Okay,
                                                    [Description("event:/Music/Introduction")] IntroductionJingle,
                                                    [Description("event:/UI/Start Game")] StartGameJingle
        };
        #endregion

        #region MonoBehaviour Stuff

        #endregion

        #region Class Implementation - Private
        #endregion

        #region Class Implementation - Public
        public void PlayOneShot(SoundEffect se)
        {
            RuntimeManager.PlayOneShot(se.GetDescription());
        }

        public void PlayOneShot(string se)
        {
            if (Enum.TryParse(se, out SoundEffect parsedSoundEffect))
            {
                RuntimeManager.PlayOneShot(parsedSoundEffect.GetDescription());
            }
            else
            {
                throw new ArgumentException("This sound effect does not exist!", "se");
            }
        }
        #endregion
    }
}
