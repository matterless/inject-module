using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Matterless.Inject
{
    public sealed class RemoteConfigManager : MonoBehaviour
    {
        #region Internal exposed
        internal static bool HasConfig(string id) => s_Instance.HasConfigInternal(id);
        internal static object GetConfig(string id, Type type) => s_Instance.GetConfigInternal(id, type);
        internal static void ApplyConfiguration(string id, object target) => s_Instance.ApplyConfigurationInternal(id, target);
        #endregion

        [SerializeField] List<RemoteConfig> m_RemoteConfigs;

        private Dictionary<string, RemoteConfig> m_Dictionary = new Dictionary<string, RemoteConfig>();

        private static RemoteConfigManager s_Instance;

        private void Awake()
        {
            s_Instance = this;

            foreach (var config in m_RemoteConfigs)
            {
                Debug.Log($"Add configuration {config.id}");
                m_Dictionary.Add(config.id, config);
            }

            foreach (var item in m_Dictionary)
            {
                Debug.Log($"{item.Key}");
            }
        }

        private bool HasConfigInternal(string id)
        {
            Debug.Log($"Has remote config {id} = {m_Dictionary.ContainsKey(id)}");
            return m_Dictionary.ContainsKey(id);
        }

        private void ApplyConfigurationInternal(string id, object target)
        {
            Debug.Log($"Apply Remote Configuration for {id}");

            if (!HasConfigInternal(id))
                throw new Exception($"Remote configurtation does not have a record with id {id}");

            Debug.Log(m_Dictionary[id].json);

            JsonConvert.PopulateObject(m_Dictionary[id].json, target);
        }
            

        private object GetConfigInternal(string id, Type type)
        {
            if (HasConfigInternal(id))
                return m_Dictionary[id].GetData(type);

            return null;
        }
    }
}