﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Abilities;



public class PlayerController : MonoBehaviour
{

    public CharacterController characterCtrlr;
    public string agentName = "???";
    public int maxHealth = 100;
    public int health;
    public bool alive = true;
    public int team = 0;
    public int bounty = 100;
    public int maxAmmo = 5;
    public int ammo = 5;

    bool showMesh = true;

    // abilities
    public int[] abilityIDs = new int[] { 0, 0, 0, 0 };
    public int[] abilityDMG = new int[] { 0, 0, 0, 0 };


    public float moveSpd;
    public Vector3 moveDirection = Vector3.zero;
    public float jumpSpd = 16f;
    public float gravity = 20f;
    public Vector3 camOffset = Vector3.zero;
    public Camera cam;
    public bool active = false;
    [SerializeField] bool isExisting = true;
    public GameControl gameCtrl;
    public GameObject bullet;
    GameObject blockShield;
    [SerializeField] GameObject smokePFX;
    Ability abilities;

    CapsuleCollider coll;
    public Renderer rend;
    public Color team1Color = Color.blue;
    public Color team2Color = Color.red;
    public Color aliveColor, deadColor;

    // animation references
    public bool animShooting = false;
    float footstepCooldown = 0f;
    [SerializeField] float footstepRate = 0.015f;

    // CharacterModel Obj
    GameObject charModel;
    GameObject wepObj;
    
    
    public Transform camObj;
    float h, v;
    public float fireRate = 0.5f;
    public float ability1CD = 0f;

	// Rewind variables
	List<Vector3> playerPos;
	List <Quaternion> playerRot;
	List <Quaternion> cameraRot;
	List<bool> playerIsShooting;
    List<bool> playerIsBlocking;

	// Use this for initialization
	void Start ()
    {
		//Sets layer to default
		gameObject.layer = 0;

		characterCtrlr = GetComponent<CharacterController> ();
        camObj = transform.GetChild(0);
        wepObj = camObj.GetChild(2).gameObject;
        camOffset = cam.transform.position - transform.position;

        //wepModel = camObj.transform.GetChild(3).gameObject;
        charModel = transform.GetChild(2).gameObject;
        blockShield = transform.GetChild(3).gameObject;
        

        abilities = GetComponent<Ability>();
        health = maxHealth;
        gameCtrl = GameControl.main;


        // Get collider component
        coll = GetComponent<CapsuleCollider>();
        // Setting Alive State opacity fade
        rend = GetComponent<Renderer>();
        if (team == 1)
        {
            aliveColor = team1Color;
            //record this character's time limit to use for later.
        }

        if (team == 2) 
        {
            aliveColor = team2Color;
            
        }

        deadColor = aliveColor;
        deadColor.a = 0.2f;

        //initialise lists
		playerPos = new List<Vector3>();
		playerRot = new List<Quaternion>();
		cameraRot = new List<Quaternion>();
		playerIsShooting = new List<bool>();
        playerIsBlocking = new List<bool>();
        
    }


    // Update is called once per frame
    void Update()
    {

        switch (gameCtrl.gameState)
        {
            case "pre-pick":
                gameObject.layer = 0;
                Reset();
                isExisting = true;
                break;
            case "pick":
                PlaybackCharacterActions();
                break;
            case "start":

                break;
            case "countdown":
                //Set Cam Dist
                SetCameraOffset();
                break;
            case "pre-live":
                Reset();
                ammo = maxAmmo;
                break;
            case "live":
                // check health state
                HealthCheck();
                if (active)
                {
                    charModel.SetActive(false);
                    //wepObj.SetActive(false);
                }
                
                if (!active && alive == true)
                {
                    //rend.material.color = aliveColor;
                    charModel.SetActive(true);
                    wepObj.SetActive(true);
                    coll.enabled = true;
                    
                }
                else if (!active && alive == false)
                {
                    //rend.material.color = deadColor;
                    charModel.SetActive(false);
                    wepObj.SetActive(false);
                    coll.enabled = false;
                    
                }

                if (active == true)
                {
                    // Get Input
                    h = Input.GetAxis("Horizontal");
                    v = Input.GetAxis("Vertical");

                    //Set Cam Dist
                    SetCameraOffset();
                    


                    // Adds new player position, player rotation and camera rotation to each list
                    playerPos.Add(transform.position);
                    playerRot.Add(transform.rotation);
                    cameraRot.Add(camObj.rotation);


                    //check for fire button
                    if (Input.GetButton("Fire1"))
                    {
                        UseWeapon();
                        playerIsShooting.Add(true);
                    }
                    else
                    {
                        animShooting = false;
                        playerIsShooting.Add(false);
                    }

                    // check if blocking
                    if (Input.GetButton("Fire2"))
                    {
                        Block(true);
                        playerIsBlocking.Add(true);
                    }
                    else
                    {
                        Block(false);
                        playerIsBlocking.Add(false);
                    }


                    // Check Gravity
                    if (characterCtrlr.isGrounded == true)
                    {
                        moveDirection = new Vector3(h, 0, v);
                        if (Input.GetButton("Jump"))
                        {
                            moveDirection.y = jumpSpd;
                        }
                    }
                    else
                    {
                        moveDirection.x = h;
                        moveDirection.z = v;
                        moveDirection.y -= gravity * Time.deltaTime;
                    }

                    //  Apply movement
                    moveDirection.x *= moveSpd;
                    moveDirection.z *= moveSpd;
                    moveDirection = transform.TransformDirection(moveDirection);
                    characterCtrlr.Move(moveDirection * Time.deltaTime);
                    // play footsteps
                    if (footstepCooldown <= 0f && characterCtrlr.isGrounded && moveDirection.x != 0f && moveDirection.z != 0f)
                    {

                        FMODUnity.RuntimeManager.PlayOneShot(AudioControl.instance.footstepSFX, transform.position);
                        footstepCooldown = 1f;
                        print("footstep");
                    }
                    else if (footstepCooldown > 0)
                    {
                        footstepCooldown -= footstepRate;
                    }


                }
                else
                {   // Not the active instance, playback actions.
                    PlaybackCharacterActions();
                }
                break;
            case "pre-rewind":


                break;
            case "rewind":

                PlaybackCharacterActions();
                break;
            case "rewind-end":
                charModel.SetActive(true);
                break;
        }
    }
            
	

    void PlaybackCharacterActions()
    {
        // Get events from respective lists and set them.
        if (gameCtrl.step < playerPos.Count) // u do -1 to stop error
        {
            if (!isExisting)
            {
                isExisting = true;
                transform.GetChild(2).gameObject.SetActive(true);
            }

            transform.position = playerPos[gameCtrl.step];
            transform.rotation = playerRot[gameCtrl.step];
            camObj.rotation = cameraRot[gameCtrl.step];

            // check if shooting
            if (playerIsShooting[gameCtrl.step])
            {
                UseWeapon();
            }
            else
            {
                animShooting = false;
            }
            // check if blocking
            if (playerIsBlocking[gameCtrl.step])
            {
                Block(true);
            } else
            {
                Block(false);
            }
        } else
        {
            //print(name + " doesnt exist in this moment. Time: "+gameCtrl.time+" gameState: "+gameCtrl.gameState);
            if (isExisting)
            {
                isExisting = false;
                //transform.GetChild(2).gameObject.SetActive(false);
                Destroy(Instantiate(smokePFX,transform.position,transform.rotation),2f);
                transform.position = new Vector3(0f, -15f, 0f);
                
            }
                
            
        }
        
		
    }

    void Block(bool _isBlocking)
    {
        blockShield.SetActive(_isBlocking);
    }
    private void OnGUI() // draw ammo text temporarily
    {   if (active) GUI.Label(new Rect(10, 10, 100, 20), "Ammo: " + ammo.ToString());
    }


    void SetCameraOffset()
	{	cam.transform.position = transform.position + camOffset;
	}

    public void DestroyComponentsAtLayerEnd()
    {
        camObj.GetComponent<Camera>().enabled = false;
        Destroy(GetComponent<Aiming>());
        Destroy(camObj.GetComponent<AudioListener>());
        Destroy(camObj.GetComponent<Aiming>());
        //Destroy FMOD Listener
        //GetComponent<FMOD_Listener>().enabled = false;

    }

    void UseWeapon()
    {
        if (Time.time >= ability1CD && alive == true)
        {
            abilities.Use(this,abilityIDs[0]);
            animShooting = true;
            ability1CD = Time.time + fireRate;
            //var inst = Instantiate(bullet, camObj.position+(camObj.forward*1.5f), camObj.rotation);
        }
    }

    void HealthCheck()
    {
        if (health <= 0)
        {
            alive = false;
        } else
        {
            alive = true;
        }
    }
    public void Reset()
    {
        health = maxHealth;
        HealthCheck();
        
    }
}
