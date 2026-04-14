using UnityEngine;
using MixedReality.Toolkit.UX;

public class VideoPanelDebugButtonBinder : MonoBehaviour
{
    [SerializeField] private VideoPanelPlayer videoPanelPlayer;

    private bool isWired;

    private void Start()
    {
        WireButtons();
    }

    [ContextMenu("Wire Buttons")]
    public void WireButtons()
    {
        if (isWired)
            return;

        if (videoPanelPlayer == null)
            videoPanelPlayer = GetComponentInParent<VideoPanelPlayer>(true);

        if (videoPanelPlayer == null)
            return;

        PressableButton[] buttons = GetComponentsInChildren<PressableButton>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            PressableButton button = buttons[i];
            string key = ResolveActionKey(button.name);
            if (string.IsNullOrEmpty(key))
                continue;

            switch (key)
            {
                case "grab":
                    button.OnClicked.AddListener(videoPanelPlayer.DebugPlayGrab);
                    break;
                case "pitch":
                    button.OnClicked.AddListener(videoPanelPlayer.DebugPlayPitch);
                    break;
                case "roll":
                    button.OnClicked.AddListener(videoPanelPlayer.DebugPlayRoll);
                    break;
                case "throttle":
                    button.OnClicked.AddListener(videoPanelPlayer.DebugPlayThrottle);
                    break;
                case "yaw":
                    button.OnClicked.AddListener(videoPanelPlayer.DebugPlayYaw);
                    break;
                case "arm":
                    button.OnClicked.AddListener(videoPanelPlayer.DebugPlayArm);
                    break;
                case "trigger":
                    button.OnClicked.AddListener(videoPanelPlayer.DebugPlayTrigger);
                    break;
            }
        }

        isWired = true;
    }

    private static string ResolveActionKey(string source)
    {
        string lower = source.ToLowerInvariant();
        if (lower.Contains("throttle")) return "throttle";
        if (lower.Contains("trigger")) return "trigger";
        if (lower.Contains("pitch")) return "pitch";
        if (lower.Contains("roll")) return "roll";
        if (lower.Contains("yaw")) return "yaw";
        if (lower.Contains("grab")) return "grab";
        if (lower.Contains("arm")) return "arm";
        return null;
    }
}
