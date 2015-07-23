using UnityEngine;
using System.Collections;

public class ARLocationMarker : MonoBehaviour {
    /// <summary>
    /// The animation playing.
    /// </summary>
    private Animation m_anim;

    public void Start()
    {
        m_anim = GetComponent<Animation>();
        m_anim.Play("Show", PlayMode.StopAll);
    }

    public void Hide()
    {
        m_anim.Play("Hide", PlayMode.StopAll);
    }

    public void HideDone()
    {
        Destroy(gameObject);
    }
}
