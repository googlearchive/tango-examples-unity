using UnityEngine;
using System.Collections;

public class ManagerSingleton : MonoBehaviour {
	private static ManagerSingleton _instance;
	
	public static ManagerSingleton instance
	{
		get
		{
			if(_instance == null)
			{
				_instance = GameObject.FindObjectOfType<ManagerSingleton>();
				DontDestroyOnLoad(_instance.gameObject);
            }
            
            return _instance;
        }
    }

	void Awake () {
		if(_instance == null) {
			_instance = this;
			DontDestroyOnLoad(this);
		}
		else {
			if(this != _instance)
            {
                Destroy(this.gameObject);
            }
        }
    }
}
