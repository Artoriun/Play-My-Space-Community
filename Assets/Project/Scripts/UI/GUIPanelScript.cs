namespace PlayMySpace.PMSC.UI
{
    using UnityEngine;
    using TMPro;
    using PlayMySpace.PMSC.Caches;

    /// <summary>
    /// GUIPanelScript.cs
    /// 
    /// Updates the GUIPanel with data from the game caches/database.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class GUIPanelScript : MonoBehaviour
    {
        #region Class Members
        [SerializeField] private GameObject avatarIcon;
        #endregion

        #region MonoBehaviour - Public
        public void ChangeAvatarName()
        {
            avatarIcon.GetComponentInChildren<TextMeshProUGUI>().text = PlayerDataCache.Instance.PlayerData.name;
        }
        #endregion
    }
}
