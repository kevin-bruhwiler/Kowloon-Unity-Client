/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Licensed under the Oculus Master SDK License Version 1.0 (the "License"); you may not use
the Utilities SDK except in compliance with the License, which is provided at the time of installation
or download, or which otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at
https://developer.oculus.com/licenses/oculusmastersdk-1.0/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SimpleJSON;

/// <summary>
/// Allows grabbing and throwing of objects with the OVRGrabbable component on them.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class OVRGrabber : MonoBehaviour
{
    // Grip trigger thresholds for picking up objects, with some hysteresis.
    public float grabBegin = 0.55f;
    public float grabEnd = 0.35f;

    bool alreadyUpdated = false;

    // Demonstrates parenting the held object to the hand's transform when grabbed.
    // When false, the grabbed object is moved every FixedUpdate using MovePosition.
    // Note that MovePosition is required for proper physics simulation. If you set this to true, you can
    // easily observe broken physics simulation by, for example, moving the bottom cube of a stacked
    // tower and noting a complete loss of friction.
    [SerializeField]
    protected bool m_parentHeldObject = false;

	// If true, this script will move the hand to the transform specified by m_parentTransform, using MovePosition in
	// FixedUpdate. This allows correct physics behavior, at the cost of some latency. In this usage scenario, you
	// should NOT parent the hand to the hand anchor.
	// (If m_moveHandPosition is false, this script will NOT update the game object's position.
	// The hand gameObject can simply be attached to the hand anchor, which updates position in LateUpdate,
    // gaining us a few ms of reduced latency.)
    [SerializeField]
    protected bool m_moveHandPosition = false;

    // Child/attached transforms of the grabber, indicating where to snap held objects to (if you snap them).
    // Also used for ranking grab targets in case of multiple candidates.
    [SerializeField]
    protected Transform m_gripTransform = null;
    // Child/attached Colliders to detect candidate grabbable objects.
    [SerializeField]
    protected Collider[] m_grabVolumes = null;

    // Should be OVRInput.Controller.LTouch or OVRInput.Controller.RTouch.
    [SerializeField]
    protected OVRInput.Controller m_controller;

	// You can set this explicitly in the inspector if you're using m_moveHandPosition.
	// Otherwise, you should typically leave this null and simply parent the hand to the hand anchor
	// in your scene, using Unity's inspector.
    [SerializeField]
    protected Transform m_parentTransform;

    [SerializeField]
    protected GameObject m_player;

	protected bool m_grabVolumeEnabled = true;
    protected Vector3 m_lastPos;
    protected Quaternion m_lastRot;
    protected Quaternion m_anchorOffsetRotation;
    protected Vector3 m_anchorOffsetPosition;
    protected float m_prevFlex;
	protected OVRGrabbable m_grabbedObj = null;
    protected Vector3 m_grabbedObjectPosOff;
    protected Quaternion m_grabbedObjectRotOff;
	protected Dictionary<OVRGrabbable, int> m_grabCandidates = new Dictionary<OVRGrabbable, int>();
	protected bool m_operatingWithoutOVRCameraRig = true;

    private int placementMode = 0;
    private string[] placementModes = new string[3] { "Static Mode", "Semi-Static Mode", "Physics Mode" };
    private int movementMode = 0;
    private string[] movementModes = new string[3] { "Free", "Upright", "Snap" };
    private float thumbStickHoldTimer;
    private float snapTimer;
    private Vector3[,] possibleRotations = new Vector3[24,3] { { Vector3.up, Vector3.right, Vector3.forward }, { Vector3.right, Vector3.up, Vector3.forward },
                                                              { Vector3.right, Vector3.forward, Vector3.up }, { Vector3.up, Vector3.forward, Vector3.right },
                                                              { Vector3.forward, Vector3.right, Vector3.up }, { Vector3.forward, Vector3.up, Vector3.right },
                                                              { Vector3.up, Vector3.right, -Vector3.forward }, { Vector3.right, Vector3.up, -Vector3.forward },
                                                              { Vector3.right, -Vector3.forward, Vector3.up }, { Vector3.up, -Vector3.forward, Vector3.right },
                                                              { -Vector3.forward, Vector3.right, Vector3.up }, { -Vector3.forward, Vector3.up, Vector3.right },
                                                              { -Vector3.up, Vector3.right, Vector3.forward }, { Vector3.right, -Vector3.up, Vector3.forward },
                                                              { Vector3.right, Vector3.forward, -Vector3.up }, { -Vector3.up, Vector3.forward, Vector3.right },
                                                              { Vector3.forward, Vector3.right, -Vector3.up }, { Vector3.forward, -Vector3.up, Vector3.right },
                                                              { Vector3.up, -Vector3.right, Vector3.forward }, { -Vector3.right, Vector3.up, Vector3.forward },
                                                              { -Vector3.right, Vector3.forward, Vector3.up }, { Vector3.up, Vector3.forward, -Vector3.right },
                                                              { Vector3.forward, -Vector3.right, Vector3.up }, { Vector3.forward, Vector3.up, -Vector3.right }};
    private int rotationIx = 0;
    private Vector3 desiredDirection = Vector3.up;
    public Text UIText;
    public Image deleteTimer;
    public Image addTimer;
    private float dFillAmount = 0.0f;
    private float aFillAmount = 0.0f;
    private float currentlySelectedTimer = 0.0f;
    private bool pointDelay = false;

    public JSONNode filesToDelete = JSON.Parse("{}");

    public GameObject rightHandAnchor;
    public Shader highlight;
    public Shader baseShader;
    private GameObject currentlySelected;
    private Shader tempShader;

    public OVRPlayerController playerController;

    /// <summary>
    /// The currently grabbed object.
    /// </summary>
    public OVRGrabbable grabbedObject
    {
        get { return m_grabbedObj; }
    }

	public void ForceRelease(OVRGrabbable grabbable)
    {
        bool canRelease = (
            (m_grabbedObj != null) &&
            (m_grabbedObj == grabbable)
        );
        if (canRelease)
        {
            GrabEnd();
        }
    }

    protected virtual void Awake()
    {
        m_anchorOffsetPosition = transform.localPosition;
        m_anchorOffsetRotation = transform.localRotation;

        if(!m_moveHandPosition)
        {
		    // If we are being used with an OVRCameraRig, let it drive input updates, which may come from Update or FixedUpdate.
		    OVRCameraRig rig = transform.GetComponentInParent<OVRCameraRig>();
		    if (rig != null)
		    {
			    rig.UpdatedAnchors += (r) => {OnUpdatedAnchors();};
			    m_operatingWithoutOVRCameraRig = false;
		    }
        }
    }

    protected virtual void Start()
    {
        m_lastPos = transform.position;
        m_lastRot = transform.rotation;
        if(m_parentTransform == null)
        {
			m_parentTransform = gameObject.transform;
        }
		// We're going to setup the player collision to ignore the hand collision.
		SetPlayerIgnoreCollision(gameObject, true);

        if (UIText != null)
            UIText.enabled = false;
    }

    float Round(float i, float v)
    {
        return Mathf.Round(i / v) * v;
    }

    virtual public void Update()
    {
        alreadyUpdated = false;
        //Change position/size of held object
        if (m_grabbedObj != null)
        {
            Vector2 inp = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
            if (inp.magnitude != 0)
            {
                if (OVRInput.Get(OVRInput.Button.SecondaryThumbstick))
                {
                    //Rotate object
                    if (movementMode == 0)
                        m_grabbedObjectRotOff.eulerAngles = m_grabbedObjectRotOff.eulerAngles + new Vector3(0, inp[0], inp[1]);
                    else if (movementMode == 1)
                    {
                        if (Mathf.Abs(inp[0]) > 0.5 &&  Mathf.Abs(inp[1]) > 0.5)
                        {
                            if (Mathf.Sign(inp[0]) > 0 && Mathf.Sign(inp[1]) > 0)
                                desiredDirection = Vector3.forward;
                            else if (Mathf.Sign(inp[0]) < 0 && Mathf.Sign(inp[1]) < 0)
                                desiredDirection = -Vector3.forward;
                        }
                        else if (Mathf.Abs(inp[0]) > Mathf.Abs(inp[1]))
                        {
                            if (Mathf.Sign(inp[0]) > 0)
                                desiredDirection = Vector3.right;
                            else
                                desiredDirection = Vector3.left;
                        }
                        else
                        {
                            if (Mathf.Sign(inp[1]) > 0)
                                desiredDirection = Vector3.up;
                            else
                                desiredDirection = Vector3.down;
                        }
                    }
                    else if (movementMode == 2 && Time.timeSinceLevelLoad - snapTimer > 0.5)
                    {
                        rotationIx = (((rotationIx + (int)Mathf.Sign(inp[1])) % possibleRotations.GetLength(0)) + possibleRotations.GetLength(0)) % possibleRotations.GetLength(0);
                        snapTimer = Time.timeSinceLevelLoad;
                    }
                }
                else
                {
                    Vector3 dir = m_player.transform.up * -inp[0] + m_player.transform.forward * inp[1];
                    //Move object
                    if (movementMode == 0 || movementMode == 1)
                    {
                        m_grabbedObjectPosOff += (transform.up * -inp[0] + transform.forward * inp[1]) * 0.02f * m_grabbedObj.transform.lossyScale.magnitude;
                    }
                    else if (movementMode == 2 && Time.timeSinceLevelLoad - snapTimer > 0.25)
                    {
                        //Vector3 m_s = m_grabbedObj.GetComponent<Renderer>().bounds.size;
                        m_grabbedObjectPosOff += Vector3.Normalize((transform.up * -inp[0] + transform.forward * inp[1])); //new Vector3(0.2f * Mathf.Sign(dir[0]), 0.2f * Mathf.Sign(dir[1]), 0.2f * Mathf.Sign(dir[2]));
                        snapTimer = Time.timeSinceLevelLoad;
                    }
                }
                    
            }

            //Increase object scale
            if (movementMode == 2 && OVRInput.Get(OVRInput.Button.Two) && Time.timeSinceLevelLoad - snapTimer > 0.75)
            {
                m_grabbedObj.transform.localScale = m_grabbedObj.transform.localScale + new Vector3(0.1f, 0.1f, 0.1f);
                snapTimer = Time.timeSinceLevelLoad;
            }
            else if (OVRInput.Get(OVRInput.Button.Two) && movementMode != 2)
                m_grabbedObj.transform.localScale = m_grabbedObj.transform.localScale + new Vector3(0.02f, 0.02f, 0.02f);

            //Decrease object scale
            if (movementMode == 2 && OVRInput.Get(OVRInput.Button.One) && Time.timeSinceLevelLoad - snapTimer > 0.75)
            {
                m_grabbedObj.transform.localScale = Vector3.Max(m_grabbedObj.transform.localScale - new Vector3(0.1f, 0.1f, 0.1f), new Vector3(0.02f, 0.02f, 0.02f));
                snapTimer = Time.timeSinceLevelLoad;
            }
            else if (OVRInput.Get(OVRInput.Button.One) && movementMode != 2)
                m_grabbedObj.transform.localScale = Vector3.Max(m_grabbedObj.transform.localScale - new Vector3(0.02f, 0.02f, 0.02f), new Vector3(0.02f, 0.02f, 0.02f));

            //Don't change the mode if the thumbstick is being held down
            if ((OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick) || OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick)) && UIText != null)
                thumbStickHoldTimer = Time.timeSinceLevelLoad;

            //Swap placement mode
            if ((OVRInput.GetUp(OVRInput.Button.SecondaryThumbstick)) && UIText != null && Time.timeSinceLevelLoad - thumbStickHoldTimer < 0.25f)
                StartCoroutine(ShowMessage(0.5f, true));

            //Swap movement mode
            if ((OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick)) && UIText != null && Time.timeSinceLevelLoad - thumbStickHoldTimer < 0.25f)
                StartCoroutine(ShowMessage(0.5f, false));
        }

        if (gameObject.name == "CustomHandRight")
        {
            //Grab selected object
            if (currentlySelected != null && OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger) > 0 && OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > 0 &&
                Time.timeSinceLevelLoad - currentlySelectedTimer > 1.0f)
            {
                m_grabbedObj = currentlySelected.GetComponent<OVRGrabbable>();
                m_grabbedObj.GrabBegin(this, currentlySelected.GetComponent<Collider>());
                m_grabbedObjectRotOff.eulerAngles = (Quaternion.Inverse(transform.rotation) * currentlySelected.transform.rotation).eulerAngles;
                m_grabbedObjectPosOff = Vector3.Scale(new Vector3(0, 0, -1), (transform.position - currentlySelected.transform.position));
            }

            //Point at object to select/delete it
            if (OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger) > 0 && OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) == 0)
            {
                //Determine which object is being pointed at and highlight it
                RaycastHit hitPoint;
                Ray ray = new Ray(rightHandAnchor.transform.position + rightHandAnchor.transform.TransformDirection(new Vector3(0.05f, 0, 0)), rightHandAnchor.transform.forward);
                if (Physics.Raycast(ray, out hitPoint, Mathf.Infinity) && hitPoint.collider.gameObject.GetComponent<MeshRenderer>() != null && 
                    currentlySelected != hitPoint.collider.gameObject && hitPoint.collider.gameObject.name != "Floor")
                {
                    if (currentlySelected != null)
                        currentlySelected.GetComponent<MeshRenderer>().material.shader = tempShader;
                    currentlySelected = hitPoint.collider.gameObject;
                    currentlySelectedTimer = Time.timeSinceLevelLoad;
                    tempShader = currentlySelected.GetComponent<MeshRenderer>().material.shader;
                    currentlySelected.GetComponent<MeshRenderer>().material.shader = highlight;
                }
                if (currentlySelected != null)
                {
                    //Increase the "fill amount" on the deletion rainbow circle indicator thingy
                    if (!OVRInput.Get(OVRInput.Touch.Two) && !OVRInput.Get(OVRInput.Touch.One))
                    {
                        dFillAmount += 0.015f;
                        aFillAmount = 0.0f;
                    } 
                    else if (currentlySelected.tag == "Unapproved")
                    {
                        aFillAmount += 0.015f; // add a different icon
                        dFillAmount = 0.0f;
                    }
                    else
                    {
                        dFillAmount = 0.0f;
                        aFillAmount = 0.0f;
                    }

                    //If the indicator is full
                    if (dFillAmount >= 1)
                    {
                        //If this object has not been placed this session, it must be added to the list of things that need to be removed from the server
                        if (currentlySelected.tag != "RecentlyPlaced")
                        {
                            FilepathStorer fps = currentlySelected.GetComponent<FilepathStorer>();
                            filesToDelete[fps.GetID()+","+ fps.GetFilename()] = currentlySelected.transform.position;
                        } 
                        //If it has been placed this session, remove the tag so it will not be uploaded
                        else
                        {
                            currentlySelected.tag = "Untagged";
                        }
                        //Remove the object and reset
                        Object.Destroy(currentlySelected);
                        currentlySelected = null;
                        dFillAmount = 0.0f;
                    }
                    if (aFillAmount >= 1)
                    {
                        FilepathStorer fps = currentlySelected.GetComponent<FilepathStorer>();
                        filesToDelete[fps.GetID() + "," + fps.GetFilename()] = currentlySelected.transform.position;

                        currentlySelected.tag = "RecentlyPlaced";
                        currentlySelected.GetComponent<MeshRenderer>().material.shader = baseShader;
                        currentlySelected = null;
                        aFillAmount = 0.0f;
                    }
                }
            }
            //Reset shader when the user stops pointing
            else if (currentlySelected != null)
            {
                currentlySelected.GetComponent<MeshRenderer>().material.shader = tempShader;
                currentlySelected = null;
            }
            else
            {
                aFillAmount = 0.0f;
                dFillAmount = 0.0f;
            }
            //Haptic feedback to warn users they are deleting something
            if (dFillAmount > 0 || aFillAmount > 0)
                OVRInput.SetControllerVibration(0.0001f, 0.2f, OVRInput.Controller.RTouch);
            else
                OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);

            deleteTimer.fillAmount = dFillAmount;
            addTimer.fillAmount = aFillAmount;
        }
    }

    IEnumerator Wait(float duration)
    {
        yield return new WaitForSeconds(duration);
        pointDelay = true;
    }

    virtual public void FixedUpdate()
	{
		if (m_operatingWithoutOVRCameraRig)
        {
		    OnUpdatedAnchors();
        }
	}

    // Hands follow the touch anchors by calling MovePosition each frame to reach the anchor.
    // This is done instead of parenting to achieve workable physics. If you don't require physics on
    // your hands or held objects, you may wish to switch to parenting.
    void OnUpdatedAnchors()
    {
        // Don't want to MovePosition multiple times in a frame, as it causes high judder in conjunction
        // with the hand position prediction in the runtime.
        if (alreadyUpdated) return;
        alreadyUpdated = true;

        Vector3 destPos = m_parentTransform.TransformPoint(m_anchorOffsetPosition);
        Quaternion destRot = m_parentTransform.rotation * m_anchorOffsetRotation;

        if (m_moveHandPosition)
        {
            GetComponent<Rigidbody>().MovePosition(destPos);
            GetComponent<Rigidbody>().MoveRotation(destRot);
        }

        if (!m_parentHeldObject)
        {
            MoveGrabbedObject(destPos, destRot);
        }

        m_lastPos = transform.position;
        m_lastRot = transform.rotation;

		float prevFlex = m_prevFlex;
		// Update values from inputs
		m_prevFlex = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, m_controller);

		CheckForGrabOrRelease(prevFlex);
    }

    void OnDestroy()
    {
        if (m_grabbedObj != null)
        {
            GrabEnd();
        }
    }

    void OnTriggerEnter(Collider otherCollider)
    {
        // Get the grab trigger
		OVRGrabbable grabbable = otherCollider.GetComponent<OVRGrabbable>() ?? otherCollider.GetComponentInParent<OVRGrabbable>();
        if (grabbable == null) return;

        // Add the grabbable
        int refCount = 0;
        m_grabCandidates.TryGetValue(grabbable, out refCount);
        m_grabCandidates[grabbable] = refCount + 1;
    }

    void OnTriggerExit(Collider otherCollider)
    {
		OVRGrabbable grabbable = otherCollider.GetComponent<OVRGrabbable>() ?? otherCollider.GetComponentInParent<OVRGrabbable>();
        if (grabbable == null) return;

        // Remove the grabbable
        int refCount = 0;
        bool found = m_grabCandidates.TryGetValue(grabbable, out refCount);
        if (!found)
        {
            return;
        }

        if (refCount > 1)
        {
            m_grabCandidates[grabbable] = refCount - 1;
        }
        else
        {
            m_grabCandidates.Remove(grabbable);
        }
    }

    protected void CheckForGrabOrRelease(float prevFlex)
    {
        if ((m_prevFlex >= grabBegin) && (prevFlex < grabBegin))
        {
            GrabBegin();
        }
        else if ((m_prevFlex <= grabEnd) && (prevFlex > grabEnd))
        {
            GrabEnd();
        }
    }

    protected virtual void GrabBegin()
    {
        float closestMagSq = float.MaxValue;
		OVRGrabbable closestGrabbable = null;
        Collider closestGrabbableCollider = null;

        playerController.EnableRotation = false;

        // Iterate grab candidates and find the closest grabbable candidate
        foreach (OVRGrabbable grabbable in m_grabCandidates.Keys)
        {
            bool canGrab = !(grabbable.isGrabbed && !grabbable.allowOffhandGrab);
            if (!canGrab)
            {
                continue;
            }

            for (int j = 0; j < grabbable.grabPoints.Length; ++j)
            {
                Collider grabbableCollider = grabbable.grabPoints[j];
                // Store the closest grabbable
                Vector3 closestPointOnBounds = grabbableCollider.ClosestPointOnBounds(m_gripTransform.position);
                float grabbableMagSq = (m_gripTransform.position - closestPointOnBounds).sqrMagnitude;
                if (grabbableMagSq < closestMagSq)
                {
                    closestMagSq = grabbableMagSq;
                    closestGrabbable = grabbable;
                    closestGrabbableCollider = grabbableCollider;
                }
            }
        }

        // Disable grab volumes to prevent overlaps
        GrabVolumeEnable(false);

        if (closestGrabbable != null)
        {
            if (closestGrabbable.isGrabbed)
            {
                closestGrabbable.grabbedBy.OffhandGrabbed(closestGrabbable);
            }

            m_grabbedObj = closestGrabbable;
            m_grabbedObj.GrabBegin(this, closestGrabbableCollider);

            m_lastPos = transform.position;
            m_lastRot = transform.rotation;

            // Set up offsets for grabbed object desired position relative to hand.
            if(m_grabbedObj.snapPosition)
            {
                m_grabbedObjectPosOff = m_gripTransform.localPosition;
                if(m_grabbedObj.snapOffset)
                {
                    Vector3 snapOffset = m_grabbedObj.snapOffset.position;
                    if (m_controller == OVRInput.Controller.LTouch) snapOffset.x = -snapOffset.x;
                    m_grabbedObjectPosOff += snapOffset;
                }
            }
            else
            {
                Vector3 relPos = m_grabbedObj.transform.position - transform.position;
                relPos = Quaternion.Inverse(transform.rotation) * relPos;
                m_grabbedObjectPosOff = relPos;
            }

            if (m_grabbedObj.snapOrientation)
            {
                m_grabbedObjectRotOff = m_gripTransform.localRotation;
                if(m_grabbedObj.snapOffset)
                {
                    m_grabbedObjectRotOff = m_grabbedObj.snapOffset.rotation * m_grabbedObjectRotOff;
                }
            }
            else
            {
                Quaternion relOri = Quaternion.Inverse(transform.rotation) * m_grabbedObj.transform.rotation;
                m_grabbedObjectRotOff = relOri;
            }

            // Note: force teleport on grab, to avoid high-speed travel to dest which hits a lot of other objects at high
            // speed and sends them flying. The grabbed object may still teleport inside of other objects, but fixing that
            // is beyond the scope of this demo.
            MoveGrabbedObject(m_lastPos, m_lastRot, true);
            SetPlayerIgnoreCollision(m_grabbedObj.gameObject, true);
            if (m_parentHeldObject)
            {
                m_grabbedObj.transform.parent = transform;
            }
        }
    }

    //Used to show when the placement mode has been changed
    private IEnumerator ShowMessage(float delay, bool placement)
    {
        if (placement)
        {
            placementMode = (placementMode + 1) % placementModes.Length;
            UIText.text = placementModes[placementMode];
            UIText.color = Color.blue;
        }
        else
        {
            movementMode = (movementMode + 1) % movementModes.Length;
            UIText.text = movementModes[movementMode];
            UIText.color = Color.red;
        }
        
        UIText.enabled = true;
        yield return new WaitForSeconds(delay);
        UIText.enabled = false;
    }

    protected virtual void MoveGrabbedObject(Vector3 pos, Quaternion rot, bool forceTeleport = false)
    {
        if (m_grabbedObj == null)
        {
            return;
        }

        Rigidbody grabbedRigidbody = m_grabbedObj.grabbedRigidbody;

        Quaternion grabbableRotation = rot * m_grabbedObjectRotOff;
        Vector3 grabbablePosition = pos + rot * m_grabbedObjectPosOff;

        if (true) //forceTeleport)
        {
            grabbedRigidbody.transform.position = grabbablePosition;
            grabbedRigidbody.transform.rotation = grabbableRotation;
        }
        else
        {
            grabbedRigidbody.MovePosition(grabbablePosition);
            grabbedRigidbody.MoveRotation(grabbableRotation);
        }
        // Set upright
        if (movementMode == 1)
        {
            Quaternion q = Quaternion.FromToRotation(grabbedRigidbody.transform.up, desiredDirection) * grabbedRigidbody.transform.rotation;
            grabbedRigidbody.transform.rotation = q; //Quaternion.Slerp(grabbedRigidbody.transform.rotation, q, 1f);
        } 
        // Snap to invisible grid
        else if (movementMode == 2)
        {
            Quaternion q = Quaternion.FromToRotation(grabbedRigidbody.transform.up, possibleRotations[rotationIx, 0]) * grabbedRigidbody.transform.rotation;
            grabbedRigidbody.transform.rotation = q;

            q = Quaternion.FromToRotation(grabbedRigidbody.transform.right, possibleRotations[rotationIx, 1]) * grabbedRigidbody.transform.rotation;
            grabbedRigidbody.transform.rotation = q;

            q = Quaternion.FromToRotation(grabbedRigidbody.transform.forward, possibleRotations[rotationIx, 2]) * grabbedRigidbody.transform.rotation;
            grabbedRigidbody.transform.rotation = q;

            //Vector3 m_s = m_grabbedObj.GetComponent<Collider>().bounds.extents;
            float approxScale = grabbedRigidbody.transform.lossyScale.magnitude;
            grabbedRigidbody.transform.position = new Vector3(Round(grabbedRigidbody.transform.position.x, 2f * approxScale),
                                                    Round(grabbedRigidbody.transform.position.y, 2f * approxScale),
                                                    Round(grabbedRigidbody.transform.position.z, 2f * approxScale));
        }
    }

    protected void GrabEnd()
    {
        if (m_grabbedObj != null)
        {
			OVRPose localPose = new OVRPose { position = OVRInput.GetLocalControllerPosition(m_controller), orientation = OVRInput.GetLocalControllerRotation(m_controller) };
            OVRPose offsetPose = new OVRPose { position = m_anchorOffsetPosition, orientation = m_anchorOffsetRotation };
            localPose = localPose * offsetPose;

			OVRPose trackingSpace = transform.ToOVRPose() * localPose.Inverse();
			Vector3 linearVelocity = trackingSpace.orientation * OVRInput.GetLocalControllerVelocity(m_controller);
			Vector3 angularVelocity = trackingSpace.orientation * OVRInput.GetLocalControllerAngularVelocity(m_controller);

            GrabbableRelease(linearVelocity, angularVelocity);
        }
        playerController.EnableRotation = true;

        // Re-enable grab volumes to allow overlap events
        GrabVolumeEnable(true);
    }

    protected void GrabbableRelease(Vector3 linearVelocity, Vector3 angularVelocity)
    {
        m_grabbedObj.GrabEnd(linearVelocity, angularVelocity);
        if(m_parentHeldObject) m_grabbedObj.transform.parent = null;
        SetPlayerIgnoreCollision(m_grabbedObj.gameObject, false);

        //Depending on the placement mode, alter the collider, gravity, and kinematicicity of the object
        if (placementMode == 0)
        {
            Destroy(m_grabbedObj.GetComponent<OVRGrabbable>());
            Destroy(m_grabbedObj.GetComponent<Rigidbody>());
            if (m_grabbedObj.GetComponent<MeshCollider>() != null && m_grabbedObj.GetComponent<MeshCollider>().sharedMesh.isReadable)
                m_grabbedObj.GetComponent<MeshCollider>().convex = false;
        } else if (placementMode == 2)
        {
            m_grabbedObj.GetComponent<Rigidbody>().isKinematic = false;
            m_grabbedObj.GetComponent<Rigidbody>().useGravity = true;
        }
        
        //Objects that have been placed but not uploaded require the "RecentlyPlaced" tag so the client knows what objects to upload
        playerController.EnableRotation = true;
        m_grabbedObj.tag = "RecentlyPlaced";
        m_grabbedObj = null;
    }

    protected virtual void GrabVolumeEnable(bool enabled)
    {
        if (m_grabVolumeEnabled == enabled)
        {
            return;
        }

        m_grabVolumeEnabled = enabled;
        for (int i = 0; i < m_grabVolumes.Length; ++i)
        {
            Collider grabVolume = m_grabVolumes[i];
            grabVolume.enabled = m_grabVolumeEnabled;
        }

        if (!m_grabVolumeEnabled)
        {
            m_grabCandidates.Clear();
        }
    }

	protected virtual void OffhandGrabbed(OVRGrabbable grabbable)
    {
        if (m_grabbedObj == grabbable)
        {
            GrabbableRelease(Vector3.zero, Vector3.zero);
        }
    }

	protected void SetPlayerIgnoreCollision(GameObject grabbable, bool ignore)
	{
		if (m_player != null)
		{
			Collider[] playerColliders = m_player.GetComponentsInChildren<Collider>();
			foreach (Collider pc in playerColliders)
			{
				Collider[] colliders = grabbable.GetComponentsInChildren<Collider>();
				foreach (Collider c in colliders)
				{
					Physics.IgnoreCollision(c, pc, ignore);
				}
			}
		}
	}
}

