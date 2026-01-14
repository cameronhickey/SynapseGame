using UnityEngine;

namespace Cerebrum.Game
{
    /// <summary>
    /// Automatically configures the camera to use the 3D renderer (index 1) instead of the default 2D renderer.
    /// Attach this to the main camera in the Game3D scene, or it will be added automatically by BoardController3D.
    /// 
    /// SETUP REQUIRED:
    /// 1. In Unity Editor, create a Universal Renderer asset: 
    ///    Right-click Assets/Settings → Create → Rendering → URP Universal Renderer
    ///    Name it "Renderer3D"
    /// 
    /// 2. Add it to UniversalRP.asset:
    ///    Select Assets/Settings/UniversalRP.asset
    ///    In Renderer List, click + and add Renderer3D
    ///    It should be at index 1 (after Renderer2D at index 0)
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraRenderer3DSetup : MonoBehaviour
    {
        [Tooltip("Index of the 3D renderer in the URP asset's renderer list. Default is 1 (assuming 2D is at 0).")]
        [SerializeField] private int rendererIndex = 1;

        private void Awake()
        {
            ConfigureFor3D();
        }

        public void ConfigureFor3D()
        {
            Camera cam = GetComponent<Camera>();
            if (cam == null) return;

            // Use reflection to access URP camera data without direct dependency
            var cameraDataComponent = cam.GetComponent("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
            if (cameraDataComponent == null)
            {
                // Try to add the component via type lookup
                var urpCameraType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
                if (urpCameraType != null)
                {
                    cameraDataComponent = cam.gameObject.AddComponent(urpCameraType);
                }
            }

            if (cameraDataComponent != null)
            {
                // Use reflection to call SetRenderer
                var setRendererMethod = cameraDataComponent.GetType().GetMethod("SetRenderer");
                if (setRendererMethod != null)
                {
                    setRendererMethod.Invoke(cameraDataComponent, new object[] { rendererIndex });
                    Debug.Log($"[CameraRenderer3DSetup] Camera configured to use renderer index {rendererIndex}");
                }
                else
                {
                    Debug.LogWarning("[CameraRenderer3DSetup] SetRenderer method not found");
                }
            }
            else
            {
                Debug.LogWarning("[CameraRenderer3DSetup] Could not get or add UniversalAdditionalCameraData. Make sure URP is installed.");
            }
        }

        /// <summary>
        /// Static helper to configure any camera for 3D rendering
        /// </summary>
        public static void ConfigureCamera(Camera cam, int renderer3DIndex = 1)
        {
            if (cam == null) return;
            
            var setup = cam.GetComponent<CameraRenderer3DSetup>();
            if (setup == null)
            {
                setup = cam.gameObject.AddComponent<CameraRenderer3DSetup>();
            }
            setup.rendererIndex = renderer3DIndex;
            setup.ConfigureFor3D();
        }
    }
}
