using Unity.Netcode;
using UnityEngine;

public class PlayerBallControl : NetworkBehaviour
{
    private Rigidbody ballRigidBody;

    /// <summary>
    /// If this method is invoked on the client instance of this player, it will invoke a `ServerRpc` on the server-side.
    /// If this method is invoked on the server instance of this player, it will make player move.
    /// </summary>
    /// <remarks>
    /// Since a `NetworkTransform` component is attached to this player, and the authority on that component is set to "Server",
    /// this transform's position modification can only be performed on the server, where it will then be replicated down to all clients through `NetworkTransform`.
    /// </remarks>
    ///

    void Awake()
    {
        ballRigidBody = GetComponent<Rigidbody>();
    }
   
    void Start()
    {
        if (IsClient && IsOwner)
        {
            transform.position = new Vector3(0f, 0f,0f);
        }
    }
    
    [ServerRpc]
    public void ClientInputServerRpc(float horizontal, float vertical)
    {
        float speed = 10.0f;
        Vector3 horizontalForce = horizontal > 0 ? Vector3.right : Vector3.left;
        Vector3 verticalForce = vertical > 0 ? Vector3.forward : Vector3.back;
        
        if (horizontal > 0 || horizontal < 0)
            ballRigidBody.AddForce(speed * horizontalForce);
        if (vertical > 0 || vertical < 0)
            ballRigidBody.AddForce(speed * verticalForce);
    }
}