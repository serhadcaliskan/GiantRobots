using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class APIKeys
{
    public string OpenAIKey;
    public string WitAIKey;

    public static APIKeys Load()
    {
        // You can load this from a JSON file, but here's a default example
        //string filePath = Application.dataPath + "/Config/api_keys.json";
        //if (System.IO.File.Exists(filePath))
        //{
        //    string json = System.IO.File.ReadAllText(filePath);
        //    return JsonUtility.FromJson<APIKeys>(json);
        //}
        //else
        //{
        //    Debug.LogError("API keys file not found!");
        //    return null; 
        //}
        return new APIKeys
        {
            OpenAIKey = "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",
        };
    }
}
