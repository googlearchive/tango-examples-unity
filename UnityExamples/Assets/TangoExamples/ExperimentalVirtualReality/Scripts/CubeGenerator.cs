using UnityEngine;
using System.Collections;

public class CubeGenerator : MonoBehaviour {

	public GameObject prefab;

	public float size = 1;
	public int gridSize = 4;


	// Use this for initialization
	void Start () {

		float gridStep = 5*size;
		for (int x = -gridSize; x <= gridSize; x++) {
			for (int y = -gridSize; y <= gridSize; y++) {
				for (int z = -gridSize; z <= gridSize; z++) {
					GameObject obj = (GameObject)GameObject.Instantiate (prefab);		
					obj.transform.parent = transform;
					obj.transform.localScale = new Vector3 (size, size, size);
					obj.transform.position = new Vector3(x*gridStep,y*gridStep,z*gridStep);
				}
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
