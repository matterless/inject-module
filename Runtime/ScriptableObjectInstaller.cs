//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using UnityEngine;

//namespace Matterless.Inject
//{
//    public class ScriptableObjectInstaller : ScriptableObject
//    {
//        internal virtual void Install (Dictionary<Type, object> bindings)
//        {

//        }
//    }

//    public class ScriptableObjectInstaller<T> : ScriptableObjectInstaller where T : ScriptableObjectInstaller<T>
//    {
//        // install instances
//        internal override void Install(Dictionary<Type, object> bindings)
//        {
//            // get type
//            Type type = typeof(T);
//            Debug.Log($"Scriptable Object Install..... {type}");
//            // get public fields OR private files with SerializeField attribute
//            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
//                .Where(x => x.IsPublic || (x.IsPrivate && Attribute.IsDefined(x, typeof(UnityEngine.SerializeField)))).ToList();

//            Debug.Log(fields.Count);

//            // bind public files
//            foreach (var field in fields)
//            {
//                Debug.Log(field.Name);

//                object instance = field.GetValue(this);

//                // apply remote config to instance by field name id
//                if (RemoteConfigManager.HasConfig(field.Name))
//                    RemoteConfigManager.ApplyConfiguration(field.Name, instance);

//                // replace scriptable with remote settings
//                //if (RemoteConfigManager.HasConfig(field.Name))
//                //    instance = RemoteConfigManager.GetConfig(field.Name, field.FieldType);

//                // bind instnace to type of this context
//                bindings.Add(field.FieldType, instance);
//            }
//        }
//    }
//}