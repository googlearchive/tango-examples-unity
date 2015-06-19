using UnityEngine;
using System.Collections;


/**
 * Follows that target from above, for top down view of the main camera
 */
public class TopDownFollow : MonoBehaviour {

    public GameObject followTarget;
    public bool followYaw = false;
    private Vector3 pos;

    private Vector3 rotation;

    // Use this for initialization
    void Start () {
        transform.parent = null;
        pos = transform.position;
    }
    
    // Update is called once per frame
    void Update () {
        pos.x = followTarget.transform.position.x;
        pos.z = followTarget.transform.position.z;
        transform.position = pos;

        if (followYaw) {
            rotation = followTarget.transform.rotation.eulerAngles;
            rotation.x = 90;
            rotation.z = 0;
            transform.rotation = Quaternion.Euler(rotation);
        }
    }
}
