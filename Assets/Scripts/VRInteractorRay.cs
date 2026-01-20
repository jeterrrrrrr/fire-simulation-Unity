using UnityEngine;


public class VRInteractorRay : MonoBehaviour
{
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rayInteractor;

    void Update()
    {
        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)) // 右手 Trigger
            {
                var obj = hit.collider.GetComponent<InteractableObject>();
                if (obj != null)
                {
                    obj.Interact();
                }
            }
        }
    }
}
