namespace PlayMySpace.PMSC.UI
{
    using UnityEngine;
    using PlayMySpace.PMSC.Managers;

    public class BuildingLabelScript : MonoBehaviour
    {
        #region Class Members
        private RectTransform rectTransform;
        private float width, height;
        #endregion

        #region MonoBehaviour Stuff
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            width = rectTransform.sizeDelta.x;
            height = rectTransform.sizeDelta.y;
        }
        private void Update()
        {
            rectTransform.rotation = Quaternion.Euler(90, 0, -GameManager.Instance.CameraController.transform.rotation.eulerAngles.y);
            rectTransform.sizeDelta = new Vector2(width, height) * (GameManager.Instance.MinimapController.ZoomLevel + 1) * 0.5f;
        }
        #endregion
    }
}
