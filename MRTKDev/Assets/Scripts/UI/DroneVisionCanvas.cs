using UnityEngine;
using UnityEngine.UI;

namespace YueUltimateDronePhysics
{
    public class DroneVisionCanvas : MonoBehaviour
    {
        private const string DefaultSelfLevelingQuadName = "SelfLevelingQuad";
        private const string DefaultCamMountName = "Cam";
        private const string DefaultMainPageName = "Main Page";
        private const string AlternateMainPageName = "MainPage";
        private const string DefaultFpvCameraName = "FPVCam";
        private const string AlternateFpvCameraName = "FPVCamera";
        private const string HudQuadName = "XR Joystick HUD Quad";

        [SerializeField] private Transform droneVisionRoot;
        [SerializeField] private string selfLevelingQuadName = DefaultSelfLevelingQuadName;
        [SerializeField] private string camMountName = DefaultCamMountName;
        [SerializeField] private string mainPageName = DefaultMainPageName;
        [SerializeField] private string fpvViewObjectName = "FPV View";
        [Header("Manual References (used if auto-find fails)")]
        [Tooltip("Assign Main Page transform directly. If set, name search is skipped.")]
        [SerializeField] private Transform mainPageReference;
        [Tooltip("Assign SelfLevelingQuad/Cam transform directly. If set, drone + Cam lookup is skipped.")]
        [SerializeField] private Transform camMountReference;
        [Tooltip("Assign existing FPVCam camera directly. If set, camera lookup under Cam is skipped.")]
        [SerializeField] private Camera fpvCameraReference;
        [Tooltip("Assign target RawImage directly. If set, no child object is auto-created under Main Page.")]
        [SerializeField] private RawImage fpvViewReference;
        [Tooltip("Layer name used for HUD quad so FPV camera can exclude it.")]
        [SerializeField] private string hudLayerToHideFromFpv = "UI";
        private Camera fpvCamera;
        private RawImage fpvViewRawImage;

        private void Awake()
        {
            SetupVisionCanvas();
        }

        private void SetupVisionCanvas()
        {
            Transform mainPage = ResolveMainPage();

            if (mainPage == null)
            {
                Debug.LogWarning("DroneVisionCanvas: Main Page was not found. Set mainPageReference in Inspector.", this);
                return;
            }

            fpvViewRawImage = EnsureFpvView(mainPage, fpvViewReference);
            if (fpvViewRawImage == null)
            {
                Debug.LogWarning("DroneVisionCanvas: FPV view RawImage was not created/found.", this);
                return;
            }

            Transform camMount = ResolveCamMount();
            if (camMount == null)
                Debug.LogWarning("DroneVisionCanvas: Cam mount was not found. Set camMountReference in Inspector.", this);

            fpvCamera = ResolveFpvCamera(camMount);
            if (fpvCamera == null)
            {
                Debug.LogWarning("DroneVisionCanvas: FPVCam camera was not found. Set fpvCameraReference in Inspector.", this);
                return;
            }

            ApplyHudVisibilityForFpvCamera(fpvCamera);
            fpvViewRawImage.texture = fpvCamera.targetTexture;
            fpvViewRawImage.color = fpvCamera.targetTexture != null ? Color.white : Color.black;

            if (fpvCamera.targetTexture == null)
                Debug.LogWarning("DroneVisionCanvas: FPVCam has no target texture assigned.", this);
        }

        private void ApplyHudVisibilityForFpvCamera(Camera cameraToConfigure)
        {
            if (cameraToConfigure == null)
                return;

            GameObject hudQuad = GameObject.Find(HudQuadName);
            if (hudQuad == null)
                return;

            int hudLayer = LayerMask.NameToLayer(hudLayerToHideFromFpv);
            if (hudLayer < 0)
                hudLayer = 5;

            hudQuad.layer = hudLayer;
            cameraToConfigure.cullingMask &= ~(1 << hudLayer);
        }

        private Transform ResolveMainPage()
        {
            if (mainPageReference != null)
                return mainPageReference;

            Transform root = droneVisionRoot != null ? droneVisionRoot : transform;
            Transform mainPage = FindChildRecursive(root, mainPageName);
            if (mainPage == null)
                mainPage = FindChildRecursive(root, AlternateMainPageName);

            return mainPage;
        }

        private Transform ResolveCamMount()
        {
            if (camMountReference != null)
                return camMountReference;

            return FindCamMount();
        }

        private Camera ResolveFpvCamera(Transform camMount)
        {
            if (fpvCameraReference != null)
                return fpvCameraReference;

            Camera foundCamera = FindExistingFpvCamera(camMount);
            if (foundCamera != null)
                return foundCamera;

            return FindExistingFpvCameraGlobal();
        }

        private Transform FindCamMount()
        {
            GameObject drone = GameObject.Find(selfLevelingQuadName);
            if (drone == null)
                return null;

            Transform droneTransform = drone.transform;
            Transform directChild = droneTransform.Find(camMountName);
            if (directChild != null)
                return directChild;

            return FindChildRecursive(droneTransform, camMountName);
        }

        private Camera FindExistingFpvCamera(Transform camMount)
        {
            if (camMount == null)
                return null;

            Transform existingFpv = camMount.Find(DefaultFpvCameraName);
            if (existingFpv == null)
                existingFpv = camMount.Find(AlternateFpvCameraName);

            if (existingFpv == null)
                existingFpv = FindChildRecursive(camMount, DefaultFpvCameraName);

            if (existingFpv == null)
                existingFpv = FindChildRecursive(camMount, AlternateFpvCameraName);

            if (existingFpv == null)
                return null;

            Camera cameraComponent = existingFpv.GetComponent<Camera>();
            return cameraComponent;
        }

        private static Camera FindExistingFpvCameraGlobal()
        {
            GameObject fpvByDefaultName = GameObject.Find(DefaultFpvCameraName);
            if (fpvByDefaultName != null)
                return fpvByDefaultName.GetComponent<Camera>();

            GameObject fpvByAlternateName = GameObject.Find(AlternateFpvCameraName);
            if (fpvByAlternateName != null)
                return fpvByAlternateName.GetComponent<Camera>();

            return null;
        }

        private RawImage EnsureFpvView(Transform mainPage, RawImage existingReference)
        {
            if (existingReference != null)
                return existingReference;

            Transform fpvView = mainPage.Find(fpvViewObjectName);
            if (fpvView == null)
                return null;

            RectTransform rectTransform = fpvView.GetComponent<RectTransform>();
            if (rectTransform == null)
                return null;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;

            RawImage rawImage = fpvView.GetComponent<RawImage>();
            if (rawImage == null)
                return null;

            rawImage.raycastTarget = false;
            return rawImage;
        }

        private static Transform FindChildRecursive(Transform parent, string targetName)
        {
            if (parent == null || string.IsNullOrEmpty(targetName))
                return null;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == targetName)
                    return child;

                Transform found = FindChildRecursive(child, targetName);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
