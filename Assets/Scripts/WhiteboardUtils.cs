using UnityEngine;
using UnityEngine.SceneManagement;

public class WhiteboardUtils : MonoBehaviour
{

    public GameObject LeftHand;
    public GameObject RightHand;

    private OVRHand leftOVRHand;
    private OVRSkeleton leftOVRSkeleton;

    private OVRHand rightOVRHand;
    private OVRSkeleton rightOVRSkeleton;

    private Transform rightIndexTip;
    private Transform rightThumbTip;

    private Transform leftIndexTip;
    private Transform leftMiddleTip;
    private Transform leftThumbTip;

    public GameObject WhiteboardPrefab;
    private GameObject whiteboard;

    public GameObject SpherePrefab;

    private bool isCalibrating = false;

    private RaycastHit touch;

    private float sizeCalibrationTimer;
    private GameObject calibrationIndicator;

    private Vector3 sizeCalibrationInitialPoint;

    private GameObject projectionSphereLeft;
    private GameObject projectionSphereRight;

    private Transform whiteboardTransform;

    private const float ADJUSTMENT_DISTANCE = .2f;
    private const int BOARD_LAYER = 10;

    private Vector3 MIN_SIZE = new Vector3(.01f, .01f, .01f);
    private Vector3 MAX_SIZE = new Vector3(.05f, .05f, .05f);

    private Color MIN_COLOR = Color.red;
    private Color MAX_COLOR = Color.green;

    //Need to hold middle pinch for 2 seconds to start creating a new board
    private const float CALIBRATION_TIME = 2f;


    //Gets the normal vector of a plane which contains the three specified points
    Vector3 GetNormalVector(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return Vector3.Cross(p3 - p1, p2 - p1);
    }

    void Start()
    {
        //Get the OVR components of right and left hands
        rightOVRHand = RightHand.GetComponent<OVRHand>();
        rightOVRSkeleton = RightHand.GetComponent<OVRSkeleton>();
        leftOVRHand = LeftHand.GetComponent<OVRHand>();
        leftOVRSkeleton = LeftHand.GetComponent<OVRSkeleton>();


        //Initialize the projection spheres that will be helpful
        //visual tools when rotating the board.
        projectionSphereLeft = Instantiate(SpherePrefab);
        projectionSphereRight = Instantiate(SpherePrefab);

        projectionSphereLeft.SetActive(false);
        projectionSphereRight.SetActive(false);

    }

    void Update()
    {

        if (rightIndexTip == null)
        {
            if (rightOVRSkeleton.IsInitialized)
            {
                rightThumbTip = rightOVRSkeleton.Bones[(int)OVRPlugin.BoneId.Hand_ThumbTip].Transform;
                rightIndexTip = rightOVRSkeleton.Bones[(int)OVRPlugin.BoneId.Hand_IndexTip].Transform;

            }
            return;
        }


        if (leftIndexTip == null)
        {
            if (leftOVRSkeleton.IsInitialized)
            {
                leftMiddleTip = leftOVRSkeleton.Bones[(int)OVRPlugin.BoneId.Hand_MiddleTip].Transform;
                leftThumbTip = leftOVRSkeleton.Bones[(int)OVRPlugin.BoneId.Hand_ThumbTip].Transform;
                leftIndexTip = leftOVRSkeleton.Bones[(int)OVRPlugin.BoneId.Hand_IndexTip].Transform;
            }
            return;
        }

        //Make sure that both hands are initialized.
        if (!leftOVRSkeleton.IsInitialized || !leftOVRSkeleton.IsInitialized) return;

        //If the left pinky is pinched, reset the scene
        if (leftOVRHand.GetFingerIsPinching(OVRHand.HandFinger.Pinky))
        {
            SceneManager.LoadScene("WhiteboardScene");
        }

        //If the left middle finger is being pinched, start calibration
        if (leftOVRHand.GetFingerIsPinching(OVRHand.HandFinger.Middle))
        {

            //Holds the coordinate for the point between the tip of the thumb and the middle finger
            Vector3 thumbMiddleMidpoint = (leftThumbTip.position + leftMiddleTip.position) / 2;

            //Increment the calibration timer.
            sizeCalibrationTimer += Time.deltaTime;

            //If the calibration indicator sphere hasn't been initialized, instantiate one
            if (calibrationIndicator == null)
            {
                calibrationIndicator = Instantiate(SpherePrefab, thumbMiddleMidpoint, Quaternion.identity);
            }
            else
            {
                //If already calibrating, adjust the size and the color of the indicator
                // based on how far along the middle pinch has been held.
                calibrationIndicator.transform.localScale = Vector3.Lerp(MIN_SIZE, MAX_SIZE, sizeCalibrationTimer / CALIBRATION_TIME);
                calibrationIndicator.GetComponent<Renderer>().material.color = Color.Lerp(MIN_COLOR, MAX_COLOR, sizeCalibrationTimer / CALIBRATION_TIME);
            }

            //If the middle pinch is held for long enough, start initializing the board
            if (sizeCalibrationTimer >= CALIBRATION_TIME)
            {
                //If you have just created the board
                if (!isCalibrating)
                {
                    //Size the board from the top left corner
                    sizeCalibrationInitialPoint = thumbMiddleMidpoint;

                    //Instantiate the whiteboard prefab
                    whiteboard = Instantiate(WhiteboardPrefab, sizeCalibrationInitialPoint, Quaternion.identity);
                }

                //We're assuming that every board is perpendicular to the ground.

                //Vertical difference between the top left corner and the pinch.
                //Holds the board's height.
                float vertDiff = sizeCalibrationInitialPoint.y - thumbMiddleMidpoint.y;

                //"Horizontal" difference between the top left corner and the pinch
                //Holds the board's width.
                float horizDiff = Mathf.Sqrt(
                    Mathf.Pow((sizeCalibrationInitialPoint.x - thumbMiddleMidpoint.x), 2) +
                    Mathf.Pow((sizeCalibrationInitialPoint.z - thumbMiddleMidpoint.z), 2));

                //Scale the whiteboard according to where the middle pinch is located.
                whiteboard.transform.localScale = new Vector3(horizDiff, .1f, Mathf.Abs(vertDiff)) / 10f;

                //Find the normal vector using the top left corner, current position of the left hand
                //middle finge pinch, and the point one unit below the top left corner (note that we can do this because we
                //assume the board is perpendicular to the ground)
                Vector3 normalVec = GetNormalVector(sizeCalibrationInitialPoint, sizeCalibrationInitialPoint + Vector3.down, thumbMiddleMidpoint);

                //Rotate the board to look at the normal direction
                whiteboard.transform.LookAt(whiteboard.transform.position + normalVec);
                whiteboard.transform.Rotate(new Vector3(90, 0, 0), Space.Self);

                //Position the board
                whiteboard.transform.position = (sizeCalibrationInitialPoint + thumbMiddleMidpoint) / 2;

                //Initialize the texture of the board
                whiteboard.GetComponent<Whiteboard>().Initialize();

                isCalibrating = true;
            }
        }
        else
        {
            //Reset the indicator, timer and other variables controlling
            //the indicator.
            sizeCalibrationInitialPoint = Vector3.zero;
            sizeCalibrationTimer = 0;
            isCalibrating = false;
            Destroy(calibrationIndicator);
            calibrationIndicator = null;
        }

        //If both hands are pinching with their index finger
        if (rightOVRHand.GetFingerIsPinching(OVRHand.HandFinger.Index) && leftOVRHand.GetFingerIsPinching(OVRHand.HandFinger.Index))
        {

            projectionSphereLeft.SetActive(true);
            projectionSphereRight.SetActive(true);

            Vector3 leftThumbIndexMidpoint = (leftThumbTip.position + leftIndexTip.position) / 2;
            Vector3 rightThumbIndexMidpoint = (rightThumbTip.position + rightIndexTip.position) / 2;

            Vector3 leftDirection = leftIndexTip.position - leftOVRSkeleton.Bones[(int)OVRPlugin.BoneId.Hand_Index3].Transform.position;

            //Raycast to check if a board is in front of the left hand
            if (Physics.Raycast(leftThumbIndexMidpoint, leftDirection, out touch, ADJUSTMENT_DISTANCE, 1 << BOARD_LAYER))
            {
                //If there is a board, then hold its transform in the whiteboardTransform variable
                whiteboardTransform = touch.transform;

                //Since we're rotating the board and our index finger is
                //touching the board while we do this, we don't want to be writing,
                //so set the board temporarily inactive.
                whiteboardTransform.gameObject.GetComponent<Whiteboard>().isActive = false;
            }

            //If the raycast has found a whiteboard
            if (whiteboardTransform != null)
            {
                //Project the left and right middle finger pinches onto the board
                Vector3 projLeft = Vector3.ProjectOnPlane(leftThumbIndexMidpoint - whiteboardTransform.position, whiteboardTransform.up) + whiteboardTransform.position;
                Vector3 projRight = Vector3.ProjectOnPlane(rightThumbIndexMidpoint - whiteboardTransform.position, whiteboardTransform.up) + whiteboardTransform.position;

                //Bring the board to the left middle finger pinch locatoin
                whiteboardTransform.position = whiteboardTransform.position + leftThumbIndexMidpoint - projLeft;

                //Get the new normal vector to the plane based on the relative orientation of hands.
                Vector3 newNormal = GetNormalVector(leftThumbIndexMidpoint - Vector3.down, leftThumbIndexMidpoint, rightThumbIndexMidpoint);
                //^ Covid pun?

                //Rotate the board accordingly
                whiteboardTransform.LookAt(newNormal + whiteboardTransform.position);
                whiteboardTransform.Rotate(new Vector3(90, 0, 0), Space.Self);

                projectionSphereLeft.transform.position = projLeft;
                projectionSphereRight.transform.position = projRight;
            }

        }
        else
        {
            whiteboardTransform.gameObject.GetComponent<Whiteboard>().isActive = true;
            whiteboardTransform = null;
            projectionSphereLeft.SetActive(false);
            projectionSphereRight.SetActive(false);
        }
    }
}