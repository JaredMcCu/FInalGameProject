using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Player : NetworkBehaviour {
    public float movementSpeed = 50f;
    public float rotationSpeed = 130f;
    public NetworkVariable<Color> playerColorNetVar = new NetworkVariable<Color>(Color.red);

    public bool isInfected = false;

    private Camera playerCamera;
    private GameObject playerBody;
    private Vector3 initialPosition;

    private void Start() {
        playerCamera = transform.Find("Camera").GetComponent<Camera>();
        playerCamera.enabled = IsOwner;
        playerCamera.GetComponent<AudioListener>().enabled = IsOwner;
        initialPosition = transform.position;

        playerBody = transform.Find("PlayerBody").gameObject;
        ApplyColor();
    }

    private void Update() {
        if (IsOwner) {
            OwnerHandleInput();
        }
    }

    private void OwnerHandleInput() {
        Vector3 movement = CalcMovement();
        Vector3 rotation = CalcRotation();
        if (movement != Vector3.zero || rotation != Vector3.zero) {
            MoveServerRpc(movement, rotation);
        }
    }


    [ServerRpc]
    private void MoveServerRpc(Vector3 movement, Vector3 rotation)
    {
        Vector3 newPosition = transform.position + movement;

        float minX = -200f;
        float maxX = 200f;
        float minZ = -200f;
        float maxZ = 200f;


        bool isHost = IsServer && IsOwner;

        if (isHost || (newPosition.x >= minX && newPosition.x <= maxX && newPosition.z >= minZ && newPosition.z <= maxZ))
        {
            transform.Translate(movement);
            transform.Rotate(rotation);
        }
        else
        {
            transform.position = initialPosition;
        }
    }

    // Rotate around the y axis when shift is not pressed
    private Vector3 CalcRotation() {
        bool isShiftKeyDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        Vector3 rotVect = Vector3.zero;
        if (!isShiftKeyDown) {
            rotVect = new Vector3(0, Input.GetAxis("Horizontal"), 0);
            rotVect *= rotationSpeed * Time.deltaTime;
        }
        return rotVect;
    }


    // Move up and back, and strafe when shift is pressed
    private Vector3 CalcMovement() {
        bool isShiftKeyDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float x_move = 0.0f;
        float z_move = Input.GetAxis("Vertical");

        if (isShiftKeyDown) {
            x_move = Input.GetAxis("Horizontal");
        }

        Vector3 moveVect = new Vector3(x_move, 0, z_move);
        moveVect *= movementSpeed * Time.deltaTime;

        return moveVect;
    }
    void OnCollisionEnter(Collision collision) {
        if (IsOwner) {
            Player otherPlayer = collision.gameObject.GetComponent<Player>();
            Debug.Log($"{gameObject.name} collided with {otherPlayer.gameObject.name}");
            if (otherPlayer != null && !otherPlayer.isInfected && isInfected) {
                
                // Infect the collided player
                otherPlayer.CmdInfectServerRpc(); // Update the method name here
                LogInfection(otherPlayer);
            }
        }
    }

    [ServerRpc (RequireOwnership = false)]
    void CmdInfectServerRpc() {
        isInfected = true;
        RpcUpdateInfectionStatusClientRpc(isInfected);
    }

    [ClientRpc]
    public void RpcUpdateInfectionStatusClientRpc(bool infected) {
        isInfected = infected;
        // Update player appearance based on infection status
        ApplyColor();
    }

    void LogInfection(Player otherPlayer) {
        if (IsOwner) {
            Debug.Log("infected player!");
        }
    }

    void ApplyColor() {
        // Modify player's appearance based on infection status
        if (isInfected) {
            // Change color/material to green for infected players
            playerBody.GetComponent<MeshRenderer>().material.color = Color.green;
        } else {
            // Change color/material to blue for non-infected players
            playerBody.GetComponent<MeshRenderer>().material.color = Color.blue;
        }
    }

}