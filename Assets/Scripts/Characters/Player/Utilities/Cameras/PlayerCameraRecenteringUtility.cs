using Cinemachine;
using System;
using UnityEngine;

namespace EverdrivenDays
{
    [Serializable]
    public class PlayerCameraRecenteringUtility
    {
        [field: SerializeField] public CinemachineVirtualCamera VirtualCamera { get; private set; }
        [field: SerializeField] public float DefaultHorizontalWaitTime { get; private set; } = 0f;
        [field: SerializeField] public float DefaultHorizontalRecenteringTime { get; private set; } = 4f;

        private CinemachinePOV cinemachinePOV;

        public void Initialize()
        {
            if (VirtualCamera == null)
            {
                Debug.LogError("VirtualCamera is not assigned in PlayerCameraRecenteringUtility");
                return;
            }

            try
            {
                cinemachinePOV = VirtualCamera.GetCinemachineComponent<CinemachinePOV>();
                
                if (cinemachinePOV == null)
                {
                    Debug.LogError("Failed to get CinemachinePOV component. Make sure the virtual camera has this component.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error initializing PlayerCameraRecenteringUtility: {ex.Message}");
            }
        }

        public void EnableRecentering(float waitTime = -1f, float recenteringTime = -1f, float baseMovementSpeed = 1f, float movementSpeed = 1f)
        {
            if (cinemachinePOV == null)
            {
                Debug.LogWarning("Cannot enable recentering - cinemachinePOV is null");
                return;
            }
            
            cinemachinePOV.m_HorizontalRecentering.m_enabled = true;

            cinemachinePOV.m_HorizontalRecentering.CancelRecentering();

            if (waitTime == -1f)
            {
                waitTime = DefaultHorizontalWaitTime;
            }

            if (recenteringTime == -1f)
            {
                recenteringTime = DefaultHorizontalRecenteringTime;
            }

            recenteringTime = recenteringTime * baseMovementSpeed / movementSpeed;

            cinemachinePOV.m_HorizontalRecentering.m_WaitTime = waitTime;
            cinemachinePOV.m_HorizontalRecentering.m_RecenteringTime = recenteringTime;
        }

        public void DisableRecentering()
        {
            if (cinemachinePOV == null)
            {
                Debug.LogWarning("Cannot disable recentering - cinemachinePOV is null");
                return;
            }
            
            cinemachinePOV.m_HorizontalRecentering.m_enabled = false;
        }
    }
}