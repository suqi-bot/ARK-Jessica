using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    public bool isGrounded;
    public AudioSource[] audioSource;
    private AudioSource jumpSource;
    private AudioSource moveSource;
    // 0-jumpup 1-jumpdown 2-move 3-deploy 4-die
    public AudioClip[] audioClips;
    private void Awake()
    {
        audioSource = this.GetComponents<AudioSource>();
        jumpSource = audioSource[0];
        moveSource = audioSource[1];

    }

    private void SetAudio(AudioSource source)
    {
        source.mute = !GameDataMgr.Instance.audioData.soundOpen;
        source.volume = GameDataMgr.Instance.audioData.soundValue;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Ground") || collision.CompareTag("Obstacle"))
        {
            isGrounded = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        isGrounded = false;
    }

    public void JumpUpAudio()
    {
        SetAudio(jumpSource);
        jumpSource.PlayOneShot(audioClips[1]);
    }

    public void JumpDownAudio()
    {
        SetAudio(jumpSource);
        jumpSource.PlayOneShot(audioClips[0]);
    }

    public void MoveAudio(bool loop, float playSpeed)
    {
        SetAudio(moveSource);
        moveSource.loop = loop;
        if (loop)
        {
            moveSource.clip = audioClips[2];
            moveSource.pitch = playSpeed;
            moveSource.Play();
        }
        else
        {
            moveSource.Stop();
        }
       
        
    }

    public void DeployAudio()
    {
        SetAudio(jumpSource);
        jumpSource.PlayOneShot(audioClips[3]);
    }

    public void DieAudio()
    {
        SetAudio(jumpSource);
        jumpSource.PlayOneShot(audioClips[4]);
    }
}
