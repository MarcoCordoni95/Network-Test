using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking; // importo la libreria per il network

public class PlayerConnectionObject : NetworkBehaviour { // serve NetworkBehaviour

    public GameObject PlayerUnitPrefab; // prefab del player da instanziare
    [SyncVar(hook = "OnPlayerNameChanged")] public string playerName = "Anonymous"; // il tag SyncVar serve per fare in modo che se un valore cambia sul server tutti i client ne siano subito informati, l'hook invece serve per associare alla variabile una funzione che verrà richiamata ogni volta che la variabile viene modificata, passando alla funzione la variabile in questione

    void Start() { // ATTENZIONE: Start viene eseguito su tutti i client, anche se non posseggono quel player
        if (!isLocalPlayer) // se non è il mio local player esco e non faccio altro, ciò serve per evitare che start venga eseguito da tutti i client
            return; 
        CmdSpawnMyUnit(); // instanzia il player e richiama il metodo NetworkServer.Spawn() sul server perchè quest'ultimo PUO' ESSERE ESEGUITO SOLO DAL SERVER
    }

    void Update() { // ATTENZIONE: Update viene eseguito su tutti i client, anche se non posseggono quel player
        if (!isLocalPlayer) // controllo che sia il mio update, se no esco
            return;

        if (Input.GetKeyDown(KeyCode.S)) // se il giocatore preme S creo un'altra istanza 
            CmdSpawnMyUnit(); // crea il player e lo notifica a tutti i client

        if (Input.GetKeyDown(KeyCode.Q)) {
            string n = "Quill " + Random.Range(1, 100);
            Debug.Log("Send request = old name: " + playerName + " ;new name: " + n);
            CmdChangePlayerName(n);
        }
    }

    void OnPlayerNameChanged(string newName) {
        Debug.Log("OnPlayerNameChanged = Old Name: " + playerName + "; New name: " + newName);
        playerName = newName; // ATTENZIONE: se si usa un hook su una SyncVar, il nostro valore locale non verrà aggiornato automaticamente, è necessario dirglielo esplicitamente anche qui
        gameObject.name = "PlayerConnectionObject [" + newName + "]";
    }

    
    /* ---------------------- COMMAND ESEGUITI DAL CLIENT SUL SERVER, d'ora in poi sono sul server ---------------------- */

    [Command] // indico che questa funzione anche se viene richiamata da tutti i client dovrà essere eseguita solo dal server
    void CmdSpawnMyUnit() { // per convenzione OBBLIGATORIA questi metodi eseguiti dal server devono iniziare con "Cmd"
        GameObject go = Instantiate(PlayerUnitPrefab); // instanzio il player, che però è solo in locale, gli altri client ancora non sanno che esiste

        // PlayerUnitPrefab DEVE ESSERE AGGIUNTO ALLA LISTA DEI PREFABS SPAWNABILI NEL NETWORK MANAGER, ALTRIMENTI AVREMO UN ERRORE
        NetworkServer.SpawnWithClientAuthority(go, connectionToClient); // ora attraverso SpawnWithClientAuthority tutti i client conoscono questo player e gli assegno anche l'autorità (hasAuthority = true)
    }

    [Command]
    void CmdChangePlayerName(string n) {
        Debug.Log("CmdChangePlayerName: " + n);
        playerName = n;

        // qui posso verificare che il nome non contenga parole proibite

        //RpcChangePlayerName(playerName); // SE USO SyncVar NON SERVE CHE NOTIFICO MANUALMENTE IL CAMBIAMENTO AI CLIENT
    }


    /* ---------------------- RPC ESEGUITI DAL SERVER SUI CLIENT, d'ora in poi sono sul client ---------------------- */

    /*[ClientRpc] // tag che indica i comandi eseguiti solo dai client
    void RpcChangePlayerName(string n) { // devono per forza iniziare con Rpc, SE USO SyncVar NON SERVE CHE NOTIFICO MANUALMENTE IL CAMBIAMENTO AI CLIENT
        Debug.Log("RpcChangePlayerName" + n);
        playerName = n;
    }*/
}
