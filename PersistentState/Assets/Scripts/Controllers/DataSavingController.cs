using UnityEngine;
using System.Collections;
using Tango;

public class DataSavingController : MonoBehaviour {
	private static TouchScreenKeyboard keyboard;
	private static string keyboardString;
	private static UUIDUnityHolder savedAdfHolder;
	private static bool startedSaving = false;
	
	void Update () {
		if (startedSaving) {
			if(keyboard.done) {
				savedAdfHolder = new UUIDUnityHolder();
				keyboardString = keyboard.text;
				PoseProvider.SaveAreaDescription (savedAdfHolder);
				PoseProvider.GetAreaDescriptionMetaData(savedAdfHolder);
				PoseProvider.AreaDescriptionMetaData_set(Common.MetaDataKeyType.KEY_NAME, keyboardString,savedAdfHolder);
				PoseProvider.SaveAreaDescriptionMetaData(savedAdfHolder);
				
				// Null terminator will cause invalid argument in the file system.
				string uuid = savedAdfHolder.GetStringDataUUID().Replace("\0","");
				string path = Application.persistentDataPath + "/" + uuid;
				FileParser.SaveBuildingDataToPath(BuildingManager.instance.buildingList, path);
				EventManager.instance.GameDataSaved(true);
				startedSaving = false;
			}
		}
	}

	public static void SaveData() {
		keyboard = TouchScreenKeyboard.Open(keyboardString,TouchScreenKeyboardType.Default, false);
		startedSaving = true;
	}
}
