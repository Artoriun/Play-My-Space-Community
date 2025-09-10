namespace PlayMySpace.PMSC.UI
{
    using UnityEngine;
    using PlayMySpace.PMSC.Managers;
    using PlayMySpace.PMSC.Models;
    using PlayMySpace.PMSC.Caches;
    using PlayMySpace.PMSC.Wrappers;
    using Framework.Firestore.Wrappers;
    using TMPro;
    using LitJson;

    /// <summary>
    /// IntroductionPanelScript.cs
    /// 
    /// Handles logic related to the IntroductionPanel.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class IntroductionPanelScript : MonoBehaviour
    {
        #region Class Members
        [SerializeField] private TextMeshProUGUI textboxText;
        [SerializeField] private TextMeshProUGUI confirmText;
        [SerializeField] private TMP_InputField nameInputField;
        #endregion

        #region MonoBehaviour Stuff
        private void Awake()
        {
            nameInputField.onEndEdit.AddListener(SubmitPlayerName);
        }
        #endregion

        #region Class Implementation - Public
        public void FinishIntroduction()
        {
            GameManager.Instance.IntroductionFinished();
        }

        public void SubmitPlayerName(string name)
        {
            PlayerDataCache.Instance.PlayerData.name = name;
        }

        public void ConfirmName()
        {
            PlayerData playerData = new PlayerData
            {
                userId = AuthWrapper.Instance.UserId,
                name = PlayerDataCache.Instance.PlayerData.name
            };

            PlayerDataWrapper.Instance.PostData(playerData, OnSuccessfulPostPlayerData, OnErrorPostPlayerData);
        }

        public void OnSuccessfulPostPlayerData(PlayerData data)
        {
            Debug.Log(JsonMapper.ToJson(data));
        }

        public void OnErrorPostPlayerData(string error)
        {
            Debug.Log(error);
        }

        public void SetTextboxText(string text)
        {
            textboxText.text = text;
        }

        public void ConfirmNameText()
        {
            confirmText.text = "So your name is\n" + PlayerDataCache.Instance.PlayerData.name + "?";
        }
        #endregion
    }
}
