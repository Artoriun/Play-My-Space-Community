namespace PlayMySpace.PMSC.UI
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    /// TimelineSequenceController.cs
    /// 
    /// A controller script for a sequence of Timelines to serve as a scripted cutscene.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class TimelineSequenceController : MonoBehaviour
    {
        #region Class Members
        [SerializeField] private GameObject[] timelines;
        [SerializeField] private UnityEvent finalCallback; // The callback that is triggered upon completing the UISequence.

        private int timelineCounter = 0;
        #endregion

        #region MonoBehaviour Stuff
        private void Awake()
        {
            for (int i = 1; i < timelines.Length; i++)
            {
                timelines[i].SetActive(false);
            }
        }
        #endregion

        #region Class Implementation - Public
        public void NextUIScene()
        {
            if (timelineCounter == timelines.Length - 1)
            {
                if (finalCallback != null)
                {
                    finalCallback.Invoke();
                }
            }
            else
            {
                timelines[timelineCounter].SetActive(false);
                timelines[timelineCounter + 1].SetActive(true);
                timelineCounter++;
            }
        }

        public void GoToScene(int i)
        {
            try
            {
                timelines[timelineCounter].SetActive(false);
                timelineCounter = i;
                timelines[timelineCounter].SetActive(true);
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }
        #endregion
    }
}
