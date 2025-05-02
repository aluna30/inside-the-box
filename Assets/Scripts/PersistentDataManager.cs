using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class PersistentDataManager
{
    public static void SaveData(List<int> data)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + "/UnlockedLevels.itbul";
        FileStream stream = new FileStream(path, FileMode.Create);

        formatter.Serialize(stream, data);
        stream.Close();
        return;
    }

    public static List<int> LoadData()
    {
        string path = Application.persistentDataPath + "/UnlockedLevels.itbul";
        
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            List<int> data = formatter.Deserialize(stream) as List<int>;
            stream.Close();
            
            return data;
        }
        else
        {
            Debug.LogWarning("No save file found. Starting brand new game progress.");
            return new List<int>();
        }
    }

    public static void DeleteData()
    {
        string path = Application.persistentDataPath + "/UnlockedLevels.itbul";
        
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        else
        {
            Debug.LogWarning("Unable to delete save file. No save file found.");
        }

        return;
    }
}
