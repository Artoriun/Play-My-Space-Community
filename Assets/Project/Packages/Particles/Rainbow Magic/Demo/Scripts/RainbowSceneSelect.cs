using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rainbows
{

public class RainbowSceneSelect : MonoBehaviour
{
	public bool GUIHide = false;
	public bool GUIHide2 = false;
	public bool GUIHide3 = false;
	
    public void LoadSceneDemo1()
    {
        SceneManager.LoadScene("RainbowMissiles");
    }
    public void LoadSceneDemo2()
    {
        SceneManager.LoadScene("RainbowDemo01");
    }
    public void LoadSceneDemo3()
    {
        SceneManager.LoadScene("RainbowDemo02");
    }
    public void LoadSceneDemo4()
    {
        SceneManager.LoadScene("RainbowDemo03");
    }
    public void LoadSceneDemo5()
    {
        SceneManager.LoadScene("RainbowDemo04");
    }
    public void LoadSceneDemo6()
    {
        SceneManager.LoadScene("RainbowDemo05");
    }
	
	void Update ()
	 {
 
     if(Input.GetKeyDown(KeyCode.L))
	 {
         GUIHide = !GUIHide;
     
         if (GUIHide)
		 {
             GameObject.Find("CanvasSceneSelect").GetComponent<Canvas> ().enabled = false;
         }
		 else
		 {
             GameObject.Find("CanvasSceneSelect").GetComponent<Canvas> ().enabled = true;
         }
     }
	      if(Input.GetKeyDown(KeyCode.J))
	 {
         GUIHide2 = !GUIHide2;
     
         if (GUIHide2)
		 {
             GameObject.Find("Canvas").GetComponent<Canvas> ().enabled = false;
         }
		 else
		 {
             GameObject.Find("Canvas").GetComponent<Canvas> ().enabled = true;
         }
     }
		if(Input.GetKeyDown(KeyCode.H))
	 {
         GUIHide3 = !GUIHide3;
     
         if (GUIHide3)
		 {
             GameObject.Find("CanvasTips").GetComponent<Canvas> ().enabled = false;
         }
		 else
		 {
             GameObject.Find("CanvasTips").GetComponent<Canvas> ().enabled = true;
         }
     }
	}
}
}