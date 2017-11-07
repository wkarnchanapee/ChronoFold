﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameControl : MonoBehaviour {
    public int maxSteps = 900;
    public int step = 0;
    public int layer = 0;
    public int time = 0;
    public string gameState = "start";
    public float[,] inputArray = new float[900 , 4];
    public GameObject playerPrefab;
    public GameObject spawnPos;
    public GameObject activeInst;
    int teamToSpawnFor = 1;
    public int amountOfTeams = 2;
	// Use this for initialization
	void Start () {
        gameState = "start";
        
	}
	
	// Update is called once per frame
	void LateUpdate () {
        switch (gameState)
        {
            case "start":
                spawnPos = GameObject.Find("PlayerSpawn");
                activeInst = Instantiate(playerPrefab, spawnPos.transform.position, Quaternion.identity);
                var instController = activeInst.GetComponent<PlayerController>();
                instController.active = true;

                instController.team = teamToSpawnFor;
                teamToSpawnFor++;
                if (teamToSpawnFor > amountOfTeams) teamToSpawnFor = 1;

                step = 0;
                layer++;
                gameState = "live";
                
                break;
            case "live":
                step++;
                time = (step / 60);
                if (step >= maxSteps) gameState = "end";
                break;

            case "playback":
                break;

            case "end":
                activeInst.GetComponent<PlayerController>().active = false;
                activeInst.GetComponent<PlayerController>().DestroyComponentsAtLayerEnd();
                
                gameState = "start";
                break;

        }
        
    }
}
