using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundTrack : MonoBehaviour
{
    [System.Serializable]
    public struct BGM_Type
    {
        public string Name;
        public AudioClip Audio;
    }

    private AudioSource Audio;
    private string BGM_Name = "";

    public BGM_Type[] BGM_List;

    void Awake()
    {
        Audio = GetComponent<AudioSource>();
        if (BGM_List.Length > 0) Play_BGM("Original");
    }

    public void Play_BGM(string _name)
    {
        if (BGM_Name.Equals(_name)) return;

        for (int i = 0; i < BGM_List.Length; ++i)
        {
            if (BGM_List[i].Name.Equals(_name))
            {
                Audio.clip = BGM_List[i].Audio;
                Audio.Play();
                BGM_Name = _name;
            }
        }
    }
}
