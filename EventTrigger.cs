using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventTrigger : MonoBehaviour
{
    public PlayerMovement Player;
    public Horse Horse;

    public NPC_AI NPC;
    public HorseAI_NPC NPC_Horse;

    public bool IsPlayer = false;
    public bool IsNPC = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Horse") && IsPlayer == true)
        {
            Horse.Die();
            Player.FallingToHorse();
        }

        if (other.gameObject.CompareTag("NPC") && IsNPC == true)
        {
            NPC_Horse.Die();
            NPC.Die();
        }
    }
}
