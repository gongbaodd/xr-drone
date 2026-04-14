using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class VideoPanelPlayer : MonoBehaviour
{
    public enum VideoOption
    {
        FpvGrab,
        FpvPitch,
        FpvRoll,
        FpvThrottle,
        FpvTrigger,
        FpvYaw,
        FpvArm
    }

    [Header("References")]
    [SerializeField] private RawImage rawImage;
    [SerializeField] private GameObject videoPlayerObject;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text bodyText;

    [Header("Instructions")]
    [SerializeField] private VideoInstructionTextLibrary instructionTextLibrary;

    [Header("Video Selection")]
    [SerializeField] private VideoOption selectedVideo = VideoOption.FpvGrab;

    public VideoOption SelectedVideo => selectedVideo;

    private void Reset()
    {
        rawImage = GetComponent<RawImage>();
        if (videoPlayer == null)
        {
            videoPlayer = GetComponentInChildren<VideoPlayer>(true);
            if (videoPlayer != null)
                videoPlayerObject = videoPlayer.gameObject;
        }
        ConfigureVideoPlayer();
    }

    private void Awake()
    {
        ConfigureVideoPlayer();
        ApplySelectedClip(playNow: false);
    }

    private void OnEnable()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.prepareCompleted += OnPrepared;

        if (Application.isPlaying)
            PlaySelectedVideo();
    }

    private void OnDisable()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.prepareCompleted -= OnPrepared;
    }

    private void OnValidate()
    {
        EnsureReferences();
        ConfigureVideoPlayer();
        ApplySelectedClip(playNow: false);
    }

    public void SetSelectedVideo(VideoOption option, bool playNow)
    {
        selectedVideo = option;
        ApplySelectedClip(playNow);
    }

    public void PlaySelectedVideo()
    {
        ApplySelectedClip(playNow: true);
    }

    public void DebugPlayGrab() => PlayOption(VideoOption.FpvGrab);
    public void DebugPlayPitch() => PlayOption(VideoOption.FpvPitch);
    public void DebugPlayRoll() => PlayOption(VideoOption.FpvRoll);
    public void DebugPlayThrottle() => PlayOption(VideoOption.FpvThrottle);
    public void DebugPlayYaw() => PlayOption(VideoOption.FpvYaw);
    public void DebugPlayArm() => PlayOption(VideoOption.FpvArm);
    public void DebugPlayTrigger() => PlayOption(VideoOption.FpvTrigger);

    private void PlayOption(VideoOption option)
    {
        SetSelectedVideo(option, playNow: true);
    }

    private void ApplySelectedClip(bool playNow)
    {
        EnsureReferences();
        if (videoPlayer == null || rawImage == null || instructionTextLibrary == null)
            return;

        instructionTextLibrary.TryGet(selectedVideo, out VideoClip clip, out string title, out string description);
        UpdateInstructionText(title, description);

        if (clip == null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = null;
            rawImage.texture = null;
            return;
        }

        videoPlayer.clip = clip;
        if (!playNow)
            return;

        if (videoPlayer.isPlaying)
            videoPlayer.Stop();

        videoPlayer.Prepare();
    }

    private void OnPrepared(VideoPlayer source)
    {
        rawImage.texture = source.texture;
        source.Play();
    }

    private void ConfigureVideoPlayer()
    {
        EnsureReferences();
        if (videoPlayer == null)
            return;

        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.isLooping = true;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
    }

    private void UpdateInstructionText(string title, string description)
    {
        if (headerText != null)
            headerText.text = title;
        if (bodyText != null)
            bodyText.text = description;
    }

    private void EnsureReferences()
    {
        if (rawImage == null)
            rawImage = GetComponent<RawImage>();

        if (videoPlayerObject != null)
            videoPlayer = videoPlayerObject.GetComponent<VideoPlayer>();
        else if (videoPlayer == null)
            videoPlayer = GetComponentInChildren<VideoPlayer>(true);

        if (videoPlayer != null && videoPlayerObject == null)
            videoPlayerObject = videoPlayer.gameObject;
    }
}
