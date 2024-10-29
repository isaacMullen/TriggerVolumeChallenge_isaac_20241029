using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetManager : MonoBehaviour
{
    public GameObject[] targets;
    public GameObject currentTarget;

    readonly int childCount;

    int lastTargetIndex = 0;

    public float targetInterval = 2f;


    // Start is called before the first frame update
    public void Awake()
    {
        int childCount = transform.childCount;

        //targets = new GameObject[childCount];
                
        Debug.Log($"Child Count: {childCount}");
              

    }
    public void Start()
    {
        //populating out array with each child inside of the parent object
        
        Debug.Log($"Targets Array Length: {targets.Length}");

        

        foreach(GameObject target in targets)
        {
            Debug.Log(target.name);
        }
    }

    // Update is called once per frame
    public void Update()
    {
        
    }
    public void SelectRandomTarget()
    {
        if (currentTarget != null)
        {
            ResetTargetColor(currentTarget);
        }

        int randomIndex = Random.Range(0, targets.Length);
        
        //if the target index is the same as the last time. Choose another random target
        while (lastTargetIndex == randomIndex)
        {
            randomIndex = Random.Range(0, targets.Length);            
        }
        
        currentTarget = targets[randomIndex];
        lastTargetIndex = randomIndex;


        //changes current target to green to indicate it is ready to shoot
        ChangeTargetColor(currentTarget, Color.green);
        Debug.Log($"Current Target Name: {currentTarget.name}");
    }

    //resets the current targets color
    public void ResetTargetColor(GameObject target)
    {
        ChangeTargetColor(currentTarget, Color.white);
    }
    
    //changes a targets color, either to reset it or to make it ready to shoot
    public void ChangeTargetColor(GameObject target, Color color)
    {
        //accessing the renderer so we can change the color
        Renderer renderer = target.GetComponent<Renderer>();

        if(renderer != null)
        {
            renderer.material.color = color;
        }
    }

    public void StartTargetPractice(float interval)
    {
        InvokeRepeating(nameof(SelectRandomTarget), 0f, interval);
    }

    
}
