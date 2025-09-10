using UnityEngine;
using System.Collections;

public class AutoMovimientoMedusa : MonoBehaviour
{
	public float smoothing = 1f;
	private Vector3 autotarget;
	public Vector3 Target{
		get{return autotarget;
		}
		set{
			autotarget = value;
			StopCoroutine ("Movimiento");
			StartCoroutine("Movimiento",autotarget);
		}
	}

	
	
	void Start ()
	{
		Target = transform.position;
		StartCoroutine("NuevoTarget");
	}

	IEnumerator NuevoTarget(){
		while (this.enabled) {
			Target = transform.position + new Vector3(Random.Range(-3f,3f),0,Random.Range(-3f,3f));
			yield return new WaitForSeconds(Random.Range(3f,6f));
		}
	}
	
	IEnumerator Movimiento (Vector3 target)
	{
		while(Vector3.Distance(transform.position, target) > 0.05f)
		{
			transform.position = Vector3.Lerp(transform.position, target, smoothing * Time.deltaTime);
			yield return null;
		}
	}
}