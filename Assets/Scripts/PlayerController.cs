﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Abilities;



public class PlayerController : MonoBehaviour {

	public CharacterController characterCtrlr;
    public string agentName = "???";
    public int maxHealth = 100;
    public int health;
    public bool alive = true;
    public int team = 0;
    public int bounty = 100;
    // abilities
    public int[] abilityIDs = new int[] { 0, 0, 0, 0};
    public int[] abilityDMG = new int[] { 0, 0, 0, 0 };
    

	public float moveSpd;
	public Vector3 moveDirection = Vector3.zero;
	public float jumpSpd = 16f;
	public float gravity = 20f;
	public Vector3 camOffset = Vector3.zero;
	public Camera cam;
    public bool active = false;
    public GameControl gameCtrl;
    public GameObject bullet;
    Ability abilities;

    CapsuleCollider coll;
    public Renderer rend;
    public Color team1Color = Color.blue;
    public Color team2Color = Color.red;
    public Color aliveColor, deadColor;

    // animation references
    public bool animShooting = false;
    
    public Transform camObj;
    float h, v;
    public float fireRate = 0.5f;
    public float ability1CD = 0f;

    int maxSteps;
    object[,] recordArray = new object[900,4];

	// Use this for initialization
	void Start () {
		//Sets layer to default
		gameObject.layer = 0;

		characterCtrlr = GetComponent<CharacterController> ();
        camObj = transform.GetChild(0);
        camOffset = cam.transform.position - transform.position;

        abilities = GetComponent<Ability>();
        health = maxHealth;

        GameObject gamecontrollerObj = GameObject.Find("GameController");
        gameCtrl = gamecontrollerObj.GetComponent<GameControl>();

        // Get collider component
        coll = GetComponent<CapsuleCollider>();
        // Setting Alive State opacity fade
        rend = GetComponent<Renderer>();
        if (team == 1) aliveColor = team1Color;

        if (team == 2) aliveColor = team2Color;

        deadColor = aliveColor;
        deadColor.a = 0.2f;

        //initialise array values.
        for (int i = 0; i < gameCtrl.maxSteps; i++)
        {
            recordArray[i, 0] = Vector3.zero;
            recordArray[i, 1] = Quaternion.identity;
            recordArray[i, 2] = Quaternion.identity;
            recordArray[i, 3] = false;

        }

        
    }
	

	// Update is called once per frame
	void FixedUpdate () {

        switch (gameCtrl.gameState)
        {
            case "start":
                gameObject.layer = 0;
                health = maxHealth;
                alive = true;
                break;
            case "live":
                // check health state
                HealthCheck();
                if (alive == true)
                {
                    rend.material.color = aliveColor;
                    coll.enabled = true;
                } else
                {
                    rend.material.color = deadColor;
                    coll.enabled = false;
                }

                if (active == true)
                {
                    // Get Input
                    h = Input.GetAxis("Horizontal");
                    v = Input.GetAxis("Vertical");

                    // Record pos and rot
                    recordArray[gameCtrl.step, 0] = transform.position;
                    recordArray[gameCtrl.step, 1] = transform.rotation;
                    // record camera rot
                    recordArray[gameCtrl.step, 2] = camObj.rotation;
                    //Set Cam Dist
                    SetCameraOffset();

                    //check for fire button
                    if (Input.GetButton("Fire1"))
                    {
                        UseWeapon();
                        recordArray[gameCtrl.step, 3] = true;
                    } else
                    {
                        animShooting = false;
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

                }
                else
                {
                    PlaybackCharacterActions();
                }
                break;
            case "pre-rewind":
                active = false;
                break;
            case "rewind":
                
                PlaybackCharacterActions();
                break;
        }
            
	}

    void PlaybackCharacterActions()
    {
        // Get events for recordArray and set them.
        transform.position = (Vector3)recordArray[gameCtrl.step, 0];
        transform.rotation = (Quaternion)recordArray[gameCtrl.step, 1];
        camObj.rotation = (Quaternion)recordArray[gameCtrl.step, 2];
        // Check recordArray if weapon was pressed.
        if ((bool)recordArray[gameCtrl.step, 3] == true)
        {
            UseWeapon();
        } else {
            animShooting = false;
        }
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
}
