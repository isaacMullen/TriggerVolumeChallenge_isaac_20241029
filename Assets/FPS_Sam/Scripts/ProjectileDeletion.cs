using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileDeletion : MonoBehaviour
{
    private Transform player;

    public float maxDistance;
  
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;      
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(transform.position, player.position) > maxDistance)
        {
            Destroy(this.gameObject);
        }
        
    }

}
