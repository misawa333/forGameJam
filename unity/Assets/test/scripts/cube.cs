using UnityEngine;
using System.Collections;

public class cube : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		float dd = Time.deltaTime;
		transform.Rotate(dd*1.5f, dd * 1.71f, dd*1.83f);
	}
}
