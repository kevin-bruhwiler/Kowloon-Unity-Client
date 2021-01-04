using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using System.Linq;
using System.IO;

// Populate the item menu from asset bundles
public class PopulateContent : MonoBehaviour
{
	public Canvas canvas;

	GameObject newObj;

	public int numColumns;

	public float horizontalSpacing;

	public float verticalSpacing;

	public float objScale;

	public int numberToCreate;

	public GameObject view;

	List<GameObject> elements = new List<GameObject>();

	long grabTime = 0;

	GameObject changedElem = null;

	private Object[] items;
	// Directory names of asset bundles available to all users
	private string[] menus = new string[8] { "Fantasy", "Lights", "Foliage", "Roads", "Street Props", "Cyberpunk", "Utopian", "Custom Prefabs" };
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
			// Spin objects (for aesthetics)
			el.transform.Rotate(0, 30 * Time.deltaTime, 0);

			//Check if an object has been grabbed - delay between when objects can be grabbed sequentially
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
			FilepathStorer fps = newObj.GetComponent<FilepathStorer>();
			FilepathStorer old_fps = changedElem.GetComponent<FilepathStorer>();
			fps.SetFilepath(old_fps.GetFilepath());
			fps.SetFilename(old_fps.GetFilename());
			fps.SetPrefabName(old_fps.GetPrefabName());
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

	// Add objects from an asset bundle(s) to the menu
	public void Populate()
	{
		if (!canvas.enabled)
			return;

		// Clear the list holding all the prefabs and update the name at the top of the menu
		elements.Clear();
		transform.parent.gameObject.GetComponent<TextMesh>().text = menus[idx];

		// Get the path to the appropriate asset bundle directory
		string path = Application.persistentDataPath + "/LoadedAssetBundles/" + menus[idx];

		if (!Directory.Exists(path))
			Directory.CreateDirectory(path);

		DirectoryInfo dir = new DirectoryInfo(path);
		FileInfo[] info = dir.GetFiles("*.*");
		int j = 0;
		// For each asset bundle in the directory, load the bundle
		for (int i = 0; i < info.Length; i++)
		{
			var lab = AssetBundle.LoadFromFile(info[i].FullName);

			if (lab == null)
				continue;

			// Iterate all the assets in the bundle
			foreach (string assetName in lab.GetAllAssetNames())
			{
				// Only load prefabs
				if (assetName.EndsWith(".prefab"))
                {
					// Place the prefab in the correct spot in the menu
					float x = 8 + horizontalSpacing * (j - row) % (horizontalSpacing * numColumns);
					float y = -8 - verticalSpacing * Mathf.Floor((j - row) / numColumns);
					Vector3 pos = view.transform.TransformPoint(new Vector3(x, y, 0));
					j += 1;
					if (j <= row)
						continue;

					// Instantiate the prefab
					var prefab = lab.LoadAsset<GameObject>(assetName);
					newObj = (GameObject)Instantiate(prefab, pos, transform.rotation);

					// Prefabs with gravity will fall out of the menu
					newObj.GetComponent<Rigidbody>().useGravity = false;
					newObj.GetComponent<Rigidbody>().isKinematic = true;

					// Add metadata - used to store the prefab on the server with the appropriate asset bundle and know which bundle to load it from in future
					FilepathStorer fps = newObj.AddComponent(typeof(FilepathStorer)) as FilepathStorer;
					fps.SetFilepath(info[i].FullName);
					fps.SetFilename(info[i].Name);
					fps.SetPrefabName(assetName);

					// Adjust the scale of the prefab so it fits in the menu
					ConfigureNewObject(newObj);
					if (j == row + numberToCreate)
						break;
				}
			}

			// Unload the asset bundle - if we've filled up the visible portion of the menu, stop
			lab.Unload(false);
			resourcesSize = j;
			if (j == row + numberToCreate)
				break;
		}
	}

	private void ConfigureNewObject(GameObject newObj)
    {
		// Scale all the prefabs to the same absolute size
		newObj.transform.parent = transform;
		var MySize = newObj.GetComponent<Collider>().bounds.size;
		var MaxSize = 1 / Mathf.Max(MySize.x, Mathf.Max(MySize.y, MySize.z));
		if (MaxSize <= 0 || MaxSize == float.PositiveInfinity)
			MaxSize = 0.0001f;

		newObj.transform.localScale = objScale * new Vector3(MaxSize, MaxSize, MaxSize);

		// Add the prefab to the list of active elements
		elements.Add(newObj);
	}

	// Turn the item menu on all fill it
	public void Enable()
    {
		canvas.enabled = true;
		Populate();
	}

	// Turn the item menu off
	public void Disable()
	{
		canvas.enabled = false;
		Clear();
	}

	// Scroll up/down assets for a given menu
	public void IncrementIndex(bool pos)
    {
		if (pos)
			idx = (((idx + 1) % menus.Length) + menus.Length) % menus.Length;
		else
			idx = (((idx - 1) % menus.Length) + menus.Length) % menus.Length;

		row = 0;
	}

	// Scroll left/right to a new category of assets
	public void IncrementRow(bool pos)
    {
		if (pos) 
			row = Mathf.Max((row - numColumns) % resourcesSize, 0);
		else
			row = Mathf.Min((row + numColumns) % resourcesSize, resourcesSize);

	}

	// Clear all objects from the menu
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