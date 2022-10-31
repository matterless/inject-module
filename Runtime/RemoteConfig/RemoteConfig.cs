using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Matterless.Inject
{
    [Serializable]
    [CreateAssetMenu(menuName = "Matterless/Remote Configuration")]
    public class RemoteConfig : ScriptableObject
    {
        [SerializeField] private string m_Url;
        [SerializeField] private string m_Id;
        [SerializeField] private TextAsset m_JsonAsset;

        public string id => m_Id;
        public string json => m_JsonAsset.text;
        public object GetData (Type type) => JsonConvert.DeserializeObject(m_JsonAsset.text, type);
    }
}