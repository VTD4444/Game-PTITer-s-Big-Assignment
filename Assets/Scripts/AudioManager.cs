using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources (Tự động gán nếu để trống)")]
    public AudioSource bgmSource;       // Nhạc nền
    public AudioSource sfxSource;       // Hiệu ứng (Click, Win...)
    public AudioSource loopSfxSource;   // Hiệu ứng lặp (Mì sôi)

    [Header("Background Music (4 Giai Đoạn)")]
    public AudioClip[] stageMusics; 

    [Header("Sound Effects - UI & System")]
    public AudioClip sfxClick;
    public AudioClip sfxWin;
    public AudioClip sfxFail;   

    [Header("Sound Effects - Cooking & Barista")]
    public AudioClip sfxEat;
    public AudioClip sfxDrink;
    public AudioClip sfxPourWater; 
    public AudioClip sfxBoiling;
    public AudioClip sfxSugar;
    public AudioClip sfxGetThings;

    private int currentStage = -1;

    void Awake()
    {
        // --- SINGLETON PATTERN ---
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // --- TỰ ĐỘNG SETUP AUDIO SOURCE (MỚI THÊM) ---
            SetupAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void SetupAudioSources()
    {
        // 1. Setup Nhạc nền (BGM)
        if (bgmSource == null)
        {
            GameObject bgmObj = new GameObject("Src_BGM");
            bgmObj.transform.SetParent(transform);
            bgmSource = bgmObj.AddComponent<AudioSource>();
            bgmSource.loop = true; // Nhạc nền luôn lặp
            bgmSource.playOnAwake = false;
        }

        // 2. Setup Hiệu ứng (SFX)
        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("Src_SFX");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.loop = false; // Hiệu ứng không lặp
            sfxSource.playOnAwake = false;
        }

        // 3. Setup Hiệu ứng Lặp (Loop SFX)
        if (loopSfxSource == null)
        {
            GameObject loopObj = new GameObject("Src_LoopSFX");
            loopObj.transform.SetParent(transform);
            loopSfxSource = loopObj.AddComponent<AudioSource>();
            loopSfxSource.loop = true; // Tiếng sôi phải lặp
            loopSfxSource.playOnAwake = false;
        }
    }

    // --- CÁC HÀM PHÁT NHẠC (GIỮ NGUYÊN) ---
    public void PlayStageMusic(int stageIndex)
    {
        if (stageIndex == currentStage) return;
        if (stageMusics == null || stageIndex < 0 || stageIndex >= stageMusics.Length) return;

        currentStage = stageIndex;
        if(bgmSource) 
        {
            bgmSource.clip = stageMusics[stageIndex];
            bgmSource.Play();
        }
    }

    public void PlayClick() { PlaySFX(sfxClick); }
    public void PlayWin() { PlaySFX(sfxWin); }
    public void PlayFail() { PlaySFX(sfxFail); }
    public void PlayEat() { PlaySFX(sfxEat); }
    public void PlayDrink() { PlaySFX(sfxDrink); }
    public void PlayPourWater() { PlaySFX(sfxPourWater); }
    public void PlaySugar() { PlaySFX(sfxSugar); }
    public void PlayGetThings() { PlaySFX(sfxGetThings); }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip);
    }

    public void ToggleBoilingSound(bool isPlaying)
    {
        if (sfxBoiling == null || loopSfxSource == null) return;

        if (isPlaying)
        {
            if (!loopSfxSource.isPlaying)
            {
                loopSfxSource.clip = sfxBoiling;
                loopSfxSource.loop = true;
                loopSfxSource.Play();
            }
        }
        else
        {
            loopSfxSource.Stop();
        }
    }
}