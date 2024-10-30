using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Respawner : MonoBehaviour
{
    public Transform respawnLocation;
    public int framesTeleport = 0;    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }


    //FIX THIS SCRIPT (COROUTINE???)------------------------------------------
    
    
    // Update is called once per frame
    void Update()
    {
        if (framesTeleport > 0)
        {
            framesTeleport--;
            transform.position = respawnLocation.position;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("PlayerCatcher"))
        {
            framesTeleport = 5;
            Debug.Log("HIT COLLIDER");
        }
    }

}
