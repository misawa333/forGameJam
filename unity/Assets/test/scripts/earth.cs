using UnityEngine;
using System.Collections;

public class earth : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		float dd = Time.deltaTime;
		transform.Rotate(0.0f, dd * 3.0f, 0.0f);
	}
}
