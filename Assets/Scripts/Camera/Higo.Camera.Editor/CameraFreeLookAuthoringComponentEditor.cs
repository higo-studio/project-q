using UnityEditor;
using UnityEngine;
using Unity.Mathematics;

namespace Higo.Camera
{
    [CustomEditor(typeof(CameraFreeLookAuthoringComponent))]
    public class CameraFreeLookAuthoringComponentEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }


        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CameraFreeLookAuthoringComponent))]
        private static void DrawFreeLookGizmos(CameraFreeLookAuthoringComponent vcam, GizmoType selectionType)
        {
            // Standard frustum and logo
            // Color originalGizmoColour = Gizmos.color;
            // bool isActiveVirtualCam = CinemachineCore.Instance.IsLive(vcam);
            // Gizmos.color = isActiveVirtualCam
            //     ? CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour
            //     : CinemachineSettings.CinemachineCoreSettings.InactiveGizmoColour;
            vcam.UpdateCached();
            if (vcam.Follow != null)
            {
                var follow = vcam.Follow;
                float3 pos = follow.position;
                quaternion orient = follow.rotation;
                float3 up = follow.up;

                CameraUtility.DrawCircleAtPointWithRadius(pos + up * vcam.TopRig.Height, orient, vcam.TopRig.Radius);
                CameraUtility.DrawCircleAtPointWithRadius(pos + up * vcam.MiddleRig.Height, orient, vcam.MiddleRig.Radius);
                CameraUtility.DrawCircleAtPointWithRadius(pos + up * vcam.BottomRig.Height, orient, vcam.BottomRig.Radius);
                CameraUtility.DrawCameraPath(pos, orient, in vcam.cachedKnots, in vcam.cachedCtrl1, in vcam.cachedCtrl2);
            }

            // Gizmos.color = originalGizmoColour;
        }
    }
}