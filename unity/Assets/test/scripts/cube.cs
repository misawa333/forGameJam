using UnityEngine;
using System.Collections;

public class cube : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		float dd = Time.deltaTime;
		transform.Rotate(dd*0.5f, dd * 0.71f, dd*0.83f);
	}
}
