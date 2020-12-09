using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using System.Linq;
using System.IO;

public class PopulateContent : MonoBehaviour
{
	public Canvas canvas;

	GameObject newObj;

	public int numColumns;

	public float horizontalSpacing;

	public float verticalSpacing;

	public float objScale;

	public int numberToCreate; // number of objects to create. Exposed in inspector

	public GameObject view;

	List<GameObject> elements = new List<GameObject>();

	long grabTime = 0;

	GameObject changedElem = null;

	private Object[] items;
	private string[] menus = new string[8] { "Polygons", "Lights", "Foliage", "Roads", "Street Props", "Cyberpunk", "Utopian", "Custom Prefabs" };
	private int idx = 0;
	private int row = 0;
	private int resourcesSize = 0;

	void Start()
	{

	}

	void Update()
	{
		foreach(GameObject el in elements) 
		{
			el.transform.Rotate(0, 30 * Time.deltaTime, 0);

			if(changedElem == null && System.DateTime.Now.Ticks - grabTime > 20000000 && el.transform.GetComponent<OVRGrabbable>().isGrabbed)
            {
				grabTime = System.DateTime.Now.Ticks;
				changedElem = el;
				break;
			}
		}

		if(changedElem != null)
        {
			//Grab object from UI - replace with duplicate
			newObj = (GameObject)Instantiate(changedElem, changedElem.transform, true);
			newObj.transform.parent = transform;

			elements[elements.FindIndex(ind => ind.Equals(changedElem))] = newObj;
			changedElem.transform.parent = null;
			changedElem.GetComponent<Rigidbody>().WakeUp();
			changedElem.GetComponent<Rigidbody>().isKinematic = false;

			changedElem = null;
			Clear();
			Populate();
		}
	}

	public void Populate()
	{
		if (!canvas.enabled)
			return;

		elements.Clear();
		transform.parent.gameObject.GetComponent<TextMesh>().text = menus[idx];

		if (true)
        {
			string path = Application.persistentDataPath + "/LoadedAssetBundles/" + menus[idx];

			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);

			DirectoryInfo dir = new DirectoryInfo(path);
			FileInfo[] info = dir.GetFiles("*.*");
			int j = 0;
			for (int i = 0; i < info.Length; i++)
			{
				var lab = AssetBundle.LoadFromFile(info[i].FullName);

				if (lab == null)
					continue;

				foreach (string assetName in lab.GetAllAssetNames())
				{
					if (assetName.EndsWith(".prefab"))
                    {
						float x = 8 + horizontalSpacing * (j - row) % (horizontalSpacing * numColumns);
						float y = -8 - verticalSpacing * Mathf.Floor((j - row) / numColumns);
						Vector3 pos = view.transform.TransformPoint(new Vector3(x, y, 0));
						j += 1;
						if (j <= row)
							continue;

						var prefab = lab.LoadAsset<GameObject>(assetName);
						newObj = (GameObject)Instantiate(prefab, pos, transform.rotation);

						newObj.GetComponent<Rigidbody>().useGravity = false;
						newObj.GetComponent<Rigidbody>().isKinematic = true;

						FilepathStorer fps = newObj.AddComponent(typeof(FilepathStorer)) as FilepathStorer;
						fps.SetFilepath(info[i].FullName);
						fps.SetFilename(info[i].Name);
						fps.SetPrefabName(assetName);

						ConfigureNewObject(newObj);
						if (j == row + numberToCreate)
							break;
					}
				}

				lab.Unload(false);
				resourcesSize = j;
				if (j == row + numberToCreate)
					break;
			}
		} else {
			items = Resources.LoadAll(menus[idx], typeof(GameObject));
			resourcesSize = items.Length;
			items = items.Skip(row).Take(numberToCreate).Cast<Object>().ToArray();

			for (int i = 0; i < items.Length; i++)
			{
				float x = 8 + horizontalSpacing * i % (horizontalSpacing * numColumns);
				float y = -8 - verticalSpacing * Mathf.Floor(i / numColumns);
				Vector3 pos = view.transform.TransformPoint(new Vector3(x, y, 0));

				// Create new instances of our prefab until we've created as many as we specified
				newObj = (GameObject)Instantiate(items[i], pos, transform.rotation);

				ConfigureNewObject(newObj);

				elements.Add(newObj);
			}
		}
	}

	private void ConfigureNewObject(GameObject newObj)
    {
		newObj.transform.parent = transform;
		var MySize = newObj.GetComponent<Collider>().bounds.size;
		var MaxSize = 1 / Mathf.Max(MySize.x, Mathf.Max(MySize.y, MySize.z));
		if (MaxSize <= 0 || MaxSize == float.PositiveInfinity)
			MaxSize = 0.001f;

		newObj.transform.localScale = objScale * new Vector3(MaxSize, MaxSize, MaxSize);

		elements.Add(newObj);
	}

	public void Enable()
    {
		canvas.enabled = true;
		Populate();
	}

	public void Disable()
	{
		canvas.enabled = false;
		Clear();
	}

	public void IncrementIndex(bool pos)
    {
		if (pos)
			idx = (((idx + 1) % menus.Length) + menus.Length) % menus.Length;
		else
			idx = (((idx - 1) % menus.Length) + menus.Length) % menus.Length;

		row = 0;
	}

	public void IncrementRow(bool pos)
    {
		if (pos) 
			row = Mathf.Max((row - numColumns) % resourcesSize, 0);
		else
			row = Mathf.Min((row + numColumns) % resourcesSize, resourcesSize);

	}

	public void Clear()
    {
		transform.parent.gameObject.GetComponent<TextMesh>().text = "";
		foreach (Transform child in transform)
		{
			Destroy(child.gameObject);
		}

		elements = new List<GameObject>();
	}
}