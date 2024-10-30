using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ProBuilder;
using TMPro;

// Sam Robichaud 2022
// NSCC-Truro
// Based on tutorial by (Comp - 3 Interactive)  * with modifications *

public class FirstPersonController_Sam : MonoBehaviour
{
    public GameObject player;
    public Transform respawnLocation;
    
    float playTime = 15f;
    bool playing = false;
    int pointsScored;
    
    public TargetManager targetManager;

    public TextMeshProUGUI learnToShoot;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI doneGameText;
    
    //string to be evaluated for hitScan
    public string raycastHitName;
    
    //point from which the projectile will come from
    public Transform gunPoint;

    //projectile gameObject and it's speed
    public GameObject projectilePrefab;
    public float projectileSpeed = 20f;
    GameObject projectile;

    //Triggers
    GameObject teleportTrigger;
    GameObject stairTriggerOne;
    //Things to be triggered
    GameObject house;
    GameObject secondStairs;
    
    public bool canMove { get; private set; } = true;
    private bool isRunning => canRun && Input.GetKey(runKey);
    private bool shouldJump => Input.GetKeyDown(jumpKey) && characterController.isGrounded;
    private bool shouldCrouch => Input.GetKeyDown(crouchKey) && !duringCrouchAnimation && characterController.isGrounded;

    #region Settings

    [Header("Functional Settings")]
    [SerializeField] private bool canRun = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;    
    [SerializeField] private bool canSlideOnSlopes = true;
    [SerializeField] private bool canZoom = true;
  

    [Header("Controls")]
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode zoomKey = KeyCode.Mouse1;

    [Header("Move Settings")]
    [SerializeField] private float walkSpeed = 4.0f;
    [SerializeField] private float runSpeed = 10.0f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float slopeSpeeed = 12f;

    [Header("Look Settings")]
    [SerializeField, Range(1, 10)] private float lookSpeedX = 2.0f;
    [SerializeField, Range(1, 10)] private float lookSpeedY = 2.0f;
    [SerializeField, Range(1, 180)] private float upperLookLimit = 70.0f;
    [SerializeField, Range(-180, 1)] private float lowerLookLimit = -70.0f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 8.0f;
    [SerializeField] private float gravity = 30f;

    [Header("Crouch Settings")]
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float standingHeight = 1.8f;
    [SerializeField] private float timeToCrouch = 0.15f;
    [SerializeField] private Vector3 crouchingCenter = new Vector3(0, 0.5f, 0);
    [SerializeField] private Vector3 standingCenter = new Vector3(0, 0, 0);
    private bool isCrouching;
    private bool duringCrouchAnimation;

  

    [Header("Zoom Settings")]
    [SerializeField] private float timeToZoom = 0.2f;
    [SerializeField] private float zoomFOV = 30f;
    private float defaultFOV;
    private Coroutine zoomRoutine;

 
  

    // Sliding Settings
    private Vector3 hitPointNormal;
    private bool isSliding
    {
        get
        {
            if (characterController.isGrounded && Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, 5.0f))
            {
                hitPointNormal = slopeHit.normal;

                //prevents the player from jumping while sliding
                if (Vector3.Angle(hitPointNormal, Vector3.up) > characterController.slopeLimit)
                {
                    canJump = false;
                }
                else
                {
                    canJump = true;
                }
                return Vector3.Angle(hitPointNormal, Vector3.up) > characterController.slopeLimit;
            }
            else { return false; }
        }
    }



    #endregion

    private Camera playerCamera;
    private CharacterController characterController;

    private Vector3 moveDirection;
    private Vector2 currentInput;

    private float rotationX = 0;

    private void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        
        defaultFOV = playerCamera.fieldOfView;        


        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        teleportTrigger = GameObject.Find("TriggerTeleport");
        stairTriggerOne = GameObject.Find("StairTriggerOne");
        
        house = GameObject.Find("House");
        secondStairs = GameObject.Find("SecondStairs");
        secondStairs.SetActive(false);
        house.SetActive(false);

        instructionText.gameObject.SetActive(false);
        doneGameText.gameObject.SetActive(false);
        learnToShoot.gameObject.SetActive(true);       
    }

    private void Start()
    {
        Debug.Log($"Position: {transform.position}\n Rotation: {transform.rotation}");        
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Mouse0))
        {
            Destroy(learnToShoot);
        }
        
        if (playing && playTime > 0)
        {
            playTime -= Time.deltaTime;
        }
        else if (playTime <= 0 && playing == true)
        {
            doneGameText.SetText($"Score: {pointsScored}\nPress 'Space' to continue.");
            
            doneGameText.gameObject.SetActive(true);                                    
                                    
            targetManager.CancelInvoke();
            targetManager.ResetTargetColor(targetManager.currentTarget);
            playing = false;  
            
        }
        if (doneGameText.isActiveAndEnabled == true)
        {            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                doneGameText.gameObject.SetActive(false);
            }
        }

        if (canMove)
        {
            HandleMovementInput();
            HandleMouseLook(); // look into moving into Lateupdate if motion is jittery

            if (canJump)        { HandleJump();                                         }
            if (canCrouch)      { HandleCrouch();                                       }

            ApplyFinalMovement();
        }
        if(Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
        if(instructionText != null && instructionText.isActiveAndEnabled == true)
        {
            Time.timeScale = 0f;

            if(Input.GetKeyDown(KeyCode.Space))
            { 
                Destroy(instructionText);
                Destroy(GameObject.Find("TargetPracticeTrigger"));
                targetManager.StartTargetPractice(targetManager.targetInterval);
                Time.timeScale = 1f;
                playing = true;
            }
        }
                
        
                                                                    
    }

    private void LateUpdate()
    {

    }

    private void HandleMovementInput()
    {
        // Read inputs
        currentInput = new Vector2(Input.GetAxisRaw("Vertical"), Input.GetAxis("Horizontal"));

        // normalizes input when 2 directions are pressed at the same time
        // TODO; find a more elegant solution to normalize, this is a bit of a hack method to normalize it estimates and is not 100% accurate.
        currentInput *= (currentInput.x != 0.0f && currentInput.y != 0.0f) ? 0.7071f : 1.0f;

        // Sets the required speed multiplier
        currentInput *= (isCrouching ? crouchSpeed : isRunning ? runSpeed : walkSpeed);

        float moveDirectionY = moveDirection.y;
        moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x) + (transform.TransformDirection(Vector3.right) * currentInput.y);
        moveDirection.y = moveDirectionY;
    }

    private void HandleMouseLook()
    {
        // Rotate camera up/down
        rotationX -= Input.GetAxis("Mouse Y") * lookSpeedY;
        rotationX = Mathf.Clamp(rotationX, lowerLookLimit, upperLookLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

        // Rotate player left/right
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeedX, 0);

    }

    private void HandleJump()
    {
        if (shouldJump)
        {
            moveDirection.y = jumpForce;
        }
    }

    private void HandleCrouch()
    {
        if (shouldCrouch)
        {
            StartCoroutine(CrouchStand());
        }
    }

    private void HandleZoom()
    {
        if (Input.GetKeyDown(zoomKey))
        {
            if (zoomRoutine != null)
            {
                StopCoroutine(zoomRoutine);
                zoomRoutine = null;
            }
            zoomRoutine = StartCoroutine(ToggleZoom(true));
        }

        if (Input.GetKeyUp(zoomKey))
        {
            if (zoomRoutine != null)
            {
                StopCoroutine(zoomRoutine);
                zoomRoutine = null;
            }
            zoomRoutine = StartCoroutine(ToggleZoom(false));
        }
    }



    

    private void ApplyFinalMovement()
    {
        // Apply gravity if the character controller is not grounded
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        if (characterController.velocity.y < - 1 && characterController.isGrounded)
            moveDirection.y = 0;


        // sliding
        if (canSlideOnSlopes && isSliding)
        {
            moveDirection += new Vector3(hitPointNormal.x, -hitPointNormal.y, hitPointNormal.z) * slopeSpeeed;
        }

        // applies movement based on all inputs
        characterController.Move(moveDirection * Time.deltaTime);
    }

    private IEnumerator CrouchStand()
    {
        if (isCrouching && Physics.Raycast(playerCamera.transform.position, Vector3.up, 1.0f))
        { yield break; }
        
        duringCrouchAnimation = true;

        float timeElapsed = 0;
        float targetHeight = isCrouching ? standingHeight : crouchHeight;
        float currentHeight = characterController.height;
        Vector3 targetCenter = isCrouching ? standingCenter : crouchingCenter;
        Vector3 currentCenter = characterController.center;

        while (timeElapsed < timeToCrouch)
        {
            characterController.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsed / timeToCrouch);
            characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed / timeToCrouch);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        characterController.height = targetHeight;
        characterController.center = targetCenter;

        isCrouching = !isCrouching;

        duringCrouchAnimation = false;
    }

    private IEnumerator ToggleZoom(bool isEnter)
    {
        float targetFOV = isEnter ? zoomFOV : defaultFOV;
        float startingFOV = playerCamera.fieldOfView; // capture reference to current FOV
        float timeElapsed = 0;

        while (timeElapsed < timeToZoom)
        {
            playerCamera.fieldOfView = Mathf.Lerp(startingFOV, targetFOV, timeElapsed / timeToZoom);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        playerCamera.fieldOfView = targetFOV;
        zoomRoutine = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == teleportTrigger)
        {        
            house.SetActive(true);
            secondStairs.SetActive(false);
        }
        if (other.gameObject == stairTriggerOne)
        {          
            secondStairs.SetActive(true);
            secondStairs.transform.SetParent(house.transform);
        }
        if(other.gameObject.CompareTag("TargetPracticeTrigger"))
        {
            instructionText.gameObject.SetActive(true);
        }
        
        //NOT WORKING FOR NO REASONS
        if(other.gameObject.CompareTag("PlayerCatcher"))
        {
            Debug.Log("ISNIDE");
            player.transform.position = respawnLocation.position;
        }
    }
    //void Respawn()
    //{
    //    Debug.Log("ASDASDASDAS");
    //    transform.position = new Vector3(0, 3, -6);                    
    //}
    void Shoot()
    {                
        //referencing the camera with the MainCamera tag
        Camera cam = Camera.main;
        Vector3 cameraPosition = cam.transform.position;
        

        //our ray will turn the mouse position into world space
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        //will be where we are aiming
        Vector3 targetPoint;

        //if the ray is hitting something in the world it will detect what it is
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            targetPoint = hit.point;
            //Debug.Log(hit.point);
        }
        else
        {
            targetPoint = ray.GetPoint(1000);
        }
        Vector3 direction = (targetPoint - gunPoint.position).normalized;
        projectile = Instantiate(projectilePrefab, gunPoint.position, Quaternion.identity);

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        rb.velocity = direction * projectileSpeed;

        try
        {
            raycastHitName = hit.collider.gameObject.name;
        }
        catch(NullReferenceException)
        {
            Debug.Log("NO OBJECT DETECTED");
        }

        //HitScanning the targets on the wall
        if (targetManager.currentTarget != null && raycastHitName == targetManager.currentTarget.name && playing)
        {
            //handles the speeding up of targets
            targetManager.CancelInvoke();
            
            if(targetManager.targetInterval > 1.1)
            {
                targetManager.targetInterval -= .1f;
            }            
            
            targetManager.StartTargetPractice(targetManager.targetInterval);
            pointsScored++;


            Debug.Log($"Interval: {targetManager.targetInterval}");
        }
        else
        {
            Debug.Log("No target hit");
        }
        


    }


   
}
