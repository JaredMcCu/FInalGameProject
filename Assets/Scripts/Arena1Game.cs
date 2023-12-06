using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class Arena1Game : NetworkBehaviour {
    public Player playerPrefab;
    public Player PlayerWithCapePrefab;
    public Camera arenaCamera;
    private float gameTime = 60f;
    private float startTime;
    private bool gameEnded = false;
    public TMP_Text timerText;

    private int positionIndex = 0;
    private Vector3[] startPositions = new Vector3[] {
        new Vector3(20, 2, 0),
        new Vector3(-20, 2, 0),
        new Vector3(0, 2, 20),
        new Vector3(0, 2, -20)
    };

    private int colorIndex = 0;
    private Color[] playerColors = new Color[] {
        Color.blue,
        Color.green,
        Color.yellow,
        Color.magenta,
    };

    void Start() {
        arenaCamera.enabled = !IsClient;
        arenaCamera.GetComponent<AudioListener>().enabled = !IsClient;
        if (IsServer) {
            SpawnPlayers();
            startTime = Time.time;
        }
        
    }

    void Update()
    {
        if (!gameEnded)
        {
            float elapsedTime = Time.time - startTime; // Calculate elapsed time

            float remainingTime = gameTime - elapsedTime;

            UpdateTimerUI(remainingTime); // Update the UI on all clients

            if (remainingTime <= 0 || AllPlayersInfected())
            {
                gameEnded = true;
                EndGame();
            }
        }
    }

    void UpdateTimerUI(float remainingTime)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60F);
            int seconds = Mathf.FloorToInt(remainingTime - minutes * 60);
            string timeString = string.Format("{0:0}:{1:00}", minutes, seconds);

            // Update the TMP Text component
            timerText.text = "Time: " + timeString;

            // Hide the timer text when remaining time is zero
            if (remainingTime <= 0)
            {
                timerText.gameObject.SetActive(false);
            }
        }
    }
    void EndGame() {
        int nonInfectedCount = GetNonInfectedPlayersCount();
        if (nonInfectedCount == 0) {
            Debug.Log("All players are infected! Infected players win!");
            // Implement logic for infected player win
        } else {
            Debug.Log("Time's up! Non-infected players win!");
            // Implement logic for a tie or alternate condition
        }
    }

    bool AllPlayersInfected() {
        foreach (Player player in FindObjectsOfType<Player>()) {
            if (!player.isInfected) {
                return false;
            }
        }
        return true;
    }

    int GetNonInfectedPlayersCount() {
        int count = 0;
        foreach (Player player in FindObjectsOfType<Player>()) {
            if (!player.isInfected) {
                count++;
            }
        }
        return count;
    }

    

    private Vector3 NextPosition() {
        Vector3 pos = startPositions[positionIndex];
        positionIndex += 1;
        if (positionIndex > startPositions.Length - 1) {
            positionIndex = 0;
        }
        return pos;
    }

    private Color NextColor() {
        Color newColor = playerColors[colorIndex];
        colorIndex += 1;
        if (colorIndex > playerColors.Length - 1) {
            colorIndex = 0;
        }
        return newColor;
    }

    private void SpawnPlayers() {
        foreach (ulong clientId in NetworkManager.ConnectedClientsIds) {
            Player playerSpawn = Instantiate(playerPrefab, NextPosition(), Quaternion.identity);
            
                
            

            playerSpawn.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
            playerSpawn.playerColorNetVar.Value = NextColor();

            // Assign infection status to players
            playerSpawn.isInfected = (clientId == NetworkManager.ServerClientId); // Set the server player as infected
        }
    }
}
