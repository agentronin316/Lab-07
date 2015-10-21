using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

public class Lab02b_PlayerControlPrediction : NetworkBehaviour
{
    struct PlayerState
    {
        public int movementNumber;
        public float posX, posY, posZ;
        public float rotX, rotY, rotZ;
    }
    [SyncVar(hook = "OnServerStateChange")]
    PlayerState serverState;

    PlayerState predictedState;

    Queue<KeyCode> pendingMoves;

    [Server]
    void InitState()
    {
        serverState = new PlayerState
        {
            posX = -119f,
            posY = 165.08f,
            posZ = -924f,
            rotX = 0f,
            rotY = 0f,
            rotZ = 0f
        };
    }

    void SyncState()
    {
        PlayerState stateToRender = isLocalPlayer ? predictedState : serverState;

        transform.position = new Vector3(stateToRender.posX, stateToRender.posY, stateToRender.posZ);
        transform.rotation = Quaternion.Euler(stateToRender.rotX, stateToRender.rotY, stateToRender.rotZ);
    }

    // Use this for initialization
    void Start()
    {
        InitState();
        predictedState = serverState;
        if (isLocalPlayer)
        {
            pendingMoves = new Queue<KeyCode>();
            UpdatePredictedState();
        }
        SyncState();
    }

    PlayerState Move(PlayerState previous, KeyCode newKey)
    {
        float deltaX = 0, deltaY = 0, deltaZ = 0;
        float deltaRotationY = 0;
        switch (newKey)
        {
            case KeyCode.Q:
                deltaX = -.5f;
                break;
            case KeyCode.S:
                deltaZ = -.5f;
                break;
            case KeyCode.E:
                deltaX = .5f;
                break;
            case KeyCode.W:
                deltaZ = .5f;
                break;
            case KeyCode.A:
                deltaRotationY = -1f;
                break;
            case KeyCode.D:
                deltaRotationY = 1f;
                break;
        }

        return new PlayerState
        {
            movementNumber = 1 + previous.movementNumber,
            posX = deltaX + previous.posX,
            posY = deltaY + previous.posY,
            posZ = deltaZ + previous.posZ,
            rotX = previous.rotX,
            rotY = previous.rotY + deltaRotationY,
            rotZ = previous.rotZ
        };
    }

    // Update is called once per frame
    void Update()
    {
        if (isLocalPlayer)
        {
            KeyCode[] possibleKeys = { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.Space };
            foreach (KeyCode possibleKey in possibleKeys)
            {
                if (Input.GetKey(possibleKey))
                {
                    pendingMoves.Enqueue(possibleKey);
                    UpdatePredictedState();
                    CmdMoveOnServer(possibleKey);
                }
            }
        }

        SyncState();
    }

    [Command]
    void CmdMoveOnServer(KeyCode pressedKey)
    {
        serverState = Move(serverState, pressedKey);
    }

    void OnServerStateChanged(PlayerState newState)
    {
        serverState = newState;
        if (pendingMoves != null)
        {
            while (pendingMoves.Count > (predictedState.movementNumber - serverState.movementNumber))
            {
                pendingMoves.Dequeue();
            }
            UpdatePredictedState();
        }
    }

    void UpdatePredictedState()
    {
        predictedState = serverState;
        foreach (KeyCode moveKey in pendingMoves)
        {
            predictedState = Move(predictedState, moveKey);
        }
    }
}
