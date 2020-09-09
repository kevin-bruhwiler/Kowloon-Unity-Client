using System.Collections;
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

	void Start()
	{
		Populate();
	}

	void Update()
	{

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
			//newObj.transform.position = Math3d.ProjectPointOnPlane(transform.position, transform.up, newObj.transform.position);
			newObj.transform.parent = transform;

			newObj.transform.localScale = new Vector3(objScale, objScale, objScale);

			// Randomize the color of our image
			newObj.GetComponent<Renderer>().material.color = Random.ColorHSV();
		}
	}
}