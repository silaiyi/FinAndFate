// SoundManager.cs
using UnityEngine;
using System.Collections.Generic;
using System;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource ambientSource;      // 環境音效 (水聲)
    public AudioSource musicSource;       // 背景音樂
    public AudioSource sfxSource;         // 通用音效
    public AudioSource uiSource;          // UI音效
    public AudioSource loopSource;        // 循環音效 (低血量/加速)

    [Header("Ambient Sounds")]
    public AudioClip underwaterAmbience;

    [Header("Background Music")]
    public List<AudioClip> backgroundMusicTracks;
    private int currentTrackIndex = 0;

    [Header("Game Over Music")]
    public AudioClip gameOverMusic;

    [Header("Victory Sounds")]
    public AudioClip victorySound;        // 短暫勝利音效
    public AudioClip victoryMusic;        // 長版勝利音樂

    [Header("Player Sounds")]
    public AudioClip hurtSound;
    public AudioClip eatSound;
    public AudioClip boostSound;
    public AudioClip attackedSound;

    [Header("UI Sounds")]
    public AudioClip buttonHoverSound;
    public AudioClip buttonClickSound;
    [Header("Boat Sounds")]
    public AudioClip chasingBoatEngine;   // 追逐船引擎声
    public AudioClip fishingBoatEngine;   // 捕鱼船引擎声
    public AudioClip netCatchSound;       // 渔网捕获音效

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 初始化循環播放背景音樂
        PlayNextBackgroundTrack();
    }

    void PlayNextBackgroundTrack()
    {
        if (backgroundMusicTracks.Count == 0) return;

        musicSource.clip = backgroundMusicTracks[currentTrackIndex];
        musicSource.Play();
        currentTrackIndex = (currentTrackIndex + 1) % backgroundMusicTracks.Count;
        Invoke("PlayNextBackgroundTrack", musicSource.clip.length);
    }

    public void PlayAmbient()
    {
        ambientSource.clip = underwaterAmbience;
        ambientSource.loop = true;
        ambientSource.Play();
    }

    public void PlayGameOverMusic()
    {
        musicSource.Stop();
        musicSource.clip = gameOverMusic;
        musicSource.loop = false;
        musicSource.Play();
    }

    public void PlayVictorySound(bool playMusic = false)
    {
        // 播放短暫勝利音效
        sfxSource.PlayOneShot(victorySound);

        if (playMusic)
        {
            // 停止當前音樂
            musicSource.Stop();

            // 播放長版勝利音樂
            musicSource.clip = victoryMusic;
            musicSource.loop = false;
            musicSource.Play();
        }
    }

    public void PlayHurtSound() => sfxSource.PlayOneShot(hurtSound);
    public void PlayEatSound() => sfxSource.PlayOneShot(eatSound);
    public void PlayAttackedSound() => sfxSource.PlayOneShot(attackedSound);
    public void PlayButtonHover() => uiSource.PlayOneShot(buttonHoverSound);
    public void PlayButtonClick() => uiSource.PlayOneShot(buttonClickSound);

    public void PlayBoostLoop(bool play)
    {
        if (play && !loopSource.isPlaying)
        {
            loopSource.clip = boostSound;
            loopSource.loop = true;
            loopSource.Play();
        }
        else if (!play && loopSource.isPlaying)
        {
            loopSource.Stop();
        }
    }

    public void PlayLowHealthLoop(bool play)
    {
        // 如果已經在播放低血量音效，不需要重複播放
        if (play && !loopSource.isPlaying)
        {
            // 使用hurtSound作為低血量循環音效
            loopSource.clip = hurtSound;
            loopSource.loop = true;
            loopSource.Play();
        }
        else if (!play && loopSource.isPlaying)
        {
            loopSource.Stop();
        }
    }

    internal void PlaySFX(AudioClip netCatchSound, Vector3 position)
    {
        throw new NotImplementedException();
    }
    public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1.0f)
    {
        if (clip == null) return;
        
        // 创建临时音频源
        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.position = position;
        AudioSource audioSource = tempGO.AddComponent<AudioSource>();
        
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = 1.0f;
        audioSource.minDistance = 5f;
        audioSource.maxDistance = 50f;
        audioSource.Play();
        
        Destroy(tempGO, clip.length);
    }
}