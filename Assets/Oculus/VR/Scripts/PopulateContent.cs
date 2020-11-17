﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class PopulateContent : MonoBehaviour
{
	public GameObject prefab; // This is our prefab object that will be exposed in the inspector

	GameObject newObj;

	public int numColumns;

	public float spacing;

	public float objScale;

	public int numberToCreate; // number of objects to create. Exposed in inspector

	public GameObject view;

	List<GameObject> elements = new List<GameObject>();

	long grabTime = 0;

	GameObject changedElem = null;

	void Start()
	{
		Populate();
	}

	void Update()
	{
		foreach(GameObject el in elements) 
		{
			el.transform.Rotate(0, 30 * Time.deltaTime, 0);

			if(changedElem == null && System.DateTime.Now.Ticks - grabTime > 20000000 && el.transform.GetComponent<OVRGrabbable>().isGrabbed)
            {
				grabTime = System.DateTime.Now.Ticks;

				//Grab object from UI - replace with duplicate
				newObj = (GameObject)Instantiate(el, el.transform, true);
				newObj.transform.parent = transform;
				changedElem = el;
			}
		}

		if(changedElem != null)
        {
			elements[elements.FindIndex(ind => ind.Equals(changedElem))] = newObj;
			changedElem.transform.parent = null;
			changedElem.GetComponent<Rigidbody>().WakeUp();
			changedElem.GetComponent<Rigidbody>().isKinematic = false;

			changedElem = null;
		}
	}

	public void Populate()
	{
		GameObject newObj;

		for (int i = 0; i < numberToCreate; i++)
		{
			float x = spacing * i % (spacing * numColumns);
			float y = -spacing * Mathf.Floor(i / numColumns);
			Vector3 pos = view.transform.TransformPoint(new Vector3(x, y, 0));

			// Create new instances of our prefab until we've created as many as we specified
			newObj = (GameObject)Instantiate(prefab, pos, transform.rotation);

			newObj.transform.Rotate(0, Random.Range(0, 360), 0); //initialize at different y rotations (for aesthetics)
			newObj.transform.parent = transform;
			newObj.transform.localScale = new Vector3(objScale, objScale, objScale);

			// Randomize the color of our image
			newObj.GetComponent<Renderer>().material.color = Random.ColorHSV();

			elements.Add(newObj);
		}
	}

	public void Clear()
    {
		foreach (Transform child in transform)
		{
			Destroy(child.gameObject);
		}

		elements = new List<GameObject>();
	}
}