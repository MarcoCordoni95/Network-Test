using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerUnit : NetworkBehaviour {

    Vector3 velocity;

    // The position we think is most correct for this player. NOTE: if we are the authority, then this will be exactly the same as transform.position
    Vector3 bestGuessPosition;

    // This is a constantly updated value about our latency to the server (i.e. how many second it takes for us to receive a one-way message)
    float ourLatency; // TODO: This should probably be something we get from the PlayerConnectionObject

    // This higher this value, the faster our local position will match the best guess position
    float latencySmoothingFactor = 10;

    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {

        transform.Translate(velocity * Time.deltaTime); // questa parte di codice è eseguita da tutte le versione di questo object in esecuzione, anche se non ne hanno l'autorità

        if (!hasAuthority) { // true se è un oggetto che possiedo sul mio pc e ho l'autorità per usarlo direttamente, bisogna però assegnare questa autorità al client

            // We aren't the authority for this object, but we still need to update our local position for this object based on our best guess of where it probably is on the owning player's screen.
            bestGuessPosition = bestGuessPosition + (velocity * Time.deltaTime);

            // Instead of TELEPORTING our position to the best guess's position, we can smoothly lerp to it.
            transform.position = Vector3.Lerp(transform.position, bestGuessPosition, Time.deltaTime * latencySmoothingFactor);

            return;
        }

        // If we get to here, we are the authoritative owner of this object
        transform.Translate(velocity * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space)) // se premo space l'oggetto si alza verso l'alto, spetterà poi al network trasform di notificarlo al server e poi ai client
            this.transform.Translate(0, 1, 0);
        
        if (Input.GetKeyDown(KeyCode.D)) // se premo D distruggo l'object
            Destroy(gameObject);

        if ( /* some input */ true) {
            // The player is asking us to change our direction/speed (i.e. velocity)
            velocity = new Vector3(1, 0, 0);
            CmdUpdateVelocity(velocity, transform.position);
        }
    }


    /* ---------------------- COMMAND ESEGUITI DAL CLIENT SUL SERVER ---------------------- */
    
    [Command]
    void CmdUpdateVelocity(Vector3 v, Vector3 p) {

        //Sul server
        velocity = v;
        transform.position = p;

        // If we know what our current latency is, we could do something like this:
        //  transform.position = p + (v * (thisPlayersLatencyToServer))

        //Notifico ai client la posizione
        RpcUpdateVelocity(velocity, transform.position);
    }


    /* ---------------------- RPC ESEGUITI DAL SERVER SUI CLIENT -------------------------- */

    [ClientRpc]
    void RpcUpdateVelocity(Vector3 v, Vector3 p) { // sono sul client

        if (hasAuthority) {
            // Hey, this is my own object. I "should" already have the most accurate
            // position/velocity (possibly more "Accurate") than the server
            // Depending on the game, I MIGHT want to change to patch this info
            // from the server, even though that might look a little wonky to the user.

            // Let's assume for now that we're just going to ignore the message from the server.
            return;
        }

        // I am a non-authoratative client, so I definitely need to listen to the server.
        // If we know what our current latency is, we could do something like this: transform.position = p + (v * (ourLatency))

        //transform.position = p;
        velocity = v;
        bestGuessPosition = p + (velocity * (ourLatency));

        // Now position of player one is as close as possible on all player's screens
        // IN FACT, we don't want to directly update transform.position, because then players will keep teleporting/blinking as the updates come in. It looks dumb.
    }
}
