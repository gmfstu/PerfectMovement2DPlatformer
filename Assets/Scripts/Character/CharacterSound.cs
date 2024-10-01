using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class is responsible for playing the sounds of the character.
/// </summary>
public class CharacterSound : MonoBehaviour
{
    /// <summary>
    /// The audio source that will play the sounds.
    /// </summary>
    private AudioSource AudioPlayer;
    /// <summary>
    /// The character physics script. Main script that controls functionality of the game.
    /// </summary>
    public CharacterPhysics character;
    /// <summary>
    /// The array of audio clips. Important to be put in the same order as the dictionary names, so it can be correctly assigned.
    /// </summary>
    public AudioClip[] DictClips;
    /// <summary>
    /// The array of sounds. Important to be put in the same order as the dictionary clips, so it can be correctly assigned.
    /// </summary>
    public Sounds[] DictNames;
    /// <summary>
    /// The dictionary that will store the sounds and clips. One day I will get that Unity package that lets you serialize Dictionaries from the
    /// inspector!
    /// </summary>
    private Dictionary<Sounds, AudioClip> Clips = new Dictionary<Sounds, AudioClip>{};
    /// <summary>
    /// The base pitch of the sound. Used to give a little bit of randomness to the jump sound.
    /// </summary>
    [SerializeField] private float basePitch = 1;
    /// <summary>
    /// The random object that will be used to generate the random pitch of the jump sound, amongst other random sound effects.
    /// </summary>
    private System.Random rand = new System.Random();
    /// <summary>
    /// Whether the character was on the ground in the last frame. Used to play the landing sound.
    /// </summary>
    private bool wasOnGround = false;

    /// <summary>
    /// The sounds that can be played by the character.
    /// </summary>
    public enum Sounds{
        Jump, 
        Land
    };

    /// <summary>
    /// Assigns the clips to the dictionary.
    /// </summary>
    private void Start() {
        for (int i = 0; i < DictClips.Length; i++) {
            Clips.Add(DictNames[i], DictClips[i]);
        }

        AudioPlayer = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Plays the sound. Could this potentially be Update() instead? I figure it makes no difference.
    /// </summary>
    private void FixedUpdate() {
        if (!wasOnGround && character.CurrentState == character.StateGrounded) {
            PlaySound(Sounds.Land);
        }

        wasOnGround = character.CurrentState == character.StateGrounded;
    }

    /// <summary>
    /// Plays the sound. Does some unnecesary pitch and volume changes for the jump and landing sound, that in the future I might
    /// change either by standardizing the volume of clips or by adding a data structure instead of a dictionary for the sound, but
    /// that is really unnecessary for this project considering I have no clue what kind of sound stuff I will be doing on my 
    /// next platformer.
    /// </summary>
    /// <param name="sound">The given sound to play.</param>
    public void PlaySound(Sounds sound) {
        AudioPlayer.clip = Clips[sound];
        if (sound == Sounds.Jump) {
            AudioPlayer.pitch = basePitch + (.2f * rand.Next(-1, 2));
            AudioPlayer.time = 0.06f;
            AudioPlayer.Play();
            return;
        } else if (sound == Sounds.Land) {
            AudioPlayer.volume = .6f;
        } else {
            AudioPlayer.pitch = basePitch;
            AudioPlayer.volume = 1;
        }
        AudioPlayer.Play();
    }
}
