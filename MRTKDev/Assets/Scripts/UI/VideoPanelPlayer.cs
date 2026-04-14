using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

[RequireComponent(typeof(RawImage), typeof(VideoPlayer))]
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
        videoPlayer = GetComponent<VideoPlayer>();
        ConfigureVideoPlayer();
    }

    private void Awake()
    {
        ConfigureVideoPlayer();
        ApplySelectedClip(playNow: false);
    }

    private void OnEnable()
    {
        videoPlayer.prepareCompleted += OnPrepared;

        if (Application.isPlaying)
            PlaySelectedVideo();
    }

    private void OnDisable()
    {
        videoPlayer.prepareCompleted -= OnPrepared;
    }

    private void OnValidate()
    {
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
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.isLooping = true;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
    }

    private void UpdateInstructionText(string title, string description)
    {
        headerText.text = title;
        bodyText.text = description;
    }

}
