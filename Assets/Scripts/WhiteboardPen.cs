using UnityEngine;
using UnityEngine.SceneManagement;

public class WhiteboardPen : MonoBehaviour
{
    private Whiteboard whiteboard;
    private RaycastHit touch;

    private OVRHand m_hand;
    private OVRSkeleton m_skeleton;

    private Transform indexTip;
    private Transform thumbTip;
    private Transform indexDistal;

    private Vector3 originPoint;
    private Vector3 targetPoint;

    private const int WHITEBOARD_LAYER = 10;

    private void Awake()
    {
        //Get the scripts that hold information about hand tracking
        m_hand = GetComponent<OVRHand>();
        m_skeleton = GetComponent<OVRSkeleton>();
    }

    // Update is called once per frame
    void Update()
    {
        // Hands are not initialized immediately, so we need to wait until they appear
        // and are initialized.
        if (indexTip == null && m_skeleton.IsInitialized)
        {
            Debug.Log("Skeleton initialized");
            indexTip = m_skeleton.Bones[(int)OVRPlugin.BoneId.Hand_IndexTip].Transform;
            indexDistal = m_skeleton.Bones[(int)OVRPlugin.BoneId.Hand_Index2].Transform;
            thumbTip = m_skeleton.Bones[(int)OVRPlugin.BoneId.Hand_ThumbTip].Transform;
        }

        // If hands aren't initialized yet, don't execute the rest of the script.
        if (!indexTip) return;

        // Since we're going to be using our index finger as the pen
        // for this whiteboard, we need to cast a ray from the second joint
        // of our index finger to the tip of the finger.

        originPoint = indexDistal.position;
        targetPoint = indexTip.position;

        Vector3 direction = Vector3.Normalize(targetPoint - originPoint);
        float distance = Vector3.Distance(originPoint, targetPoint);

        //Cast a ray starting from the second index finger joint to the tip of the index finger.
        //Only check for objects that are in the whiteboard layer.
        if (Physics.Raycast(originPoint, direction, out touch, distance, 1 << WHITEBOARD_LAYER))
        {
            //Get the Whiteboard component of the whiteboard we obtain from the raycast.
            whiteboard = touch.collider.GetComponent<Whiteboard>();

            //touch.textureCoord gives us the texture coordinates at which our raycast
            //intersected the whiteboard. We can use this to tell the whiteboard where to
            //render the next circle.
            whiteboard.SetTouchPosition(touch.textureCoord.x, touch.textureCoord.y);

            //If the raycast intersects the board, it means we are touching the board
            whiteboard.ToggleTouch(true);
        }
        else
        {
            if (whiteboard != null)
            {
                //If the raycast no longer intersects, stop drawing on the board.
                whiteboard.ToggleTouch(false);
            }
        }


        //If your thumb touches your pinky, reset the scene.
        if (m_hand.GetFingerIsPinching(OVRHand.HandFinger.Pinky))
        {
            //SceneManager.LoadScene("WhiteboardScene");
            if (whiteboard)
            {
                whiteboard.Initialize();
            }
        }
    }
}