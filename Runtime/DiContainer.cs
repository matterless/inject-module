using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Matterless.Inject
{
    public struct Binding
    {
        public Type interfaceType { get; set; }
        public Type classType { get; set; }
        public object instnace { get; set; }
        public string name { get; set; }
        public string context { get; set; }
    }

    public class DiContainer
    {
        // interface -> class
        private readonly Dictionary<Type, Type> m_InterfaceBindings = new Dictionary<Type, Type>();
        // type -> object
        private readonly Dictionary<Type, object> m_Bindings = new Dictionary<Type, object>();
        // dependencies
        private readonly Dictionary<Type, List<Type>> m_Dependencies = new Dictionary<Type, List<Type>>();
        // bindings to resolve list
        private readonly List<Type> m_BindingsToResolve = new List<Type>();
        // constructors
        private readonly Dictionary<Type, ConstructorInfo> m_Constructors = new Dictionary<Type, ConstructorInfo>();
        // constructors' arguments
        private readonly Dictionary<Type, object[]> m_Arguments = new Dictionary<Type, object[]>();
        // properties
        private readonly Dictionary<Type, List<PropertyInfo>> m_Properties = new Dictionary<Type, List<PropertyInfo>>();

        #region Exposed methods
        public void Bind<I, C>(params object[] arguments) => BindInternal<I, C>(null, null, arguments);
        public void BindInstance<I, C>(C instance) => BindInternal<I, C>(instance, null);
        public void Bind<T>(params object[] arguments) => BindInternal<T>(null, null, arguments);
        public void BindInstance<T>(T instance) => BindInternal<T>(instance, null);
        public void BindToName<T>(string name) => BindInternal<T>(name, null);
        #endregion

        private void BindInternal<I,C>(object instance, string name, params object[] arguments)
        {
            Type _interface = typeof(I);

            if (!_interface.IsInterface)
                throw new Exception($"{_interface} is not an interface!");

            // bind interface to class
            m_InterfaceBindings.Add(_interface, typeof(C));
            // bind type
            BindInternal<I>(instance, name, arguments);
        }

        private void BindInternal<T>(object instance, string name, params object[] arguments)
        {
            Type classType = GetClassType<T>();

            //Debug.Log($"<color=lightblue>Bind {interfaceType} to {classType}</color>");

            // bind instance
            if(instance != null)
            {
                Debug.Log($"<color=lightblue>[inject] Bind instance {classType}</color>");
                m_Bindings.Add(classType, instance);
                return;
            }

            // bind class
            var constructorsInfo = classType.GetConstructors();

            if (constructorsInfo.Length == 0)
            {
                Debug.LogError($"No constructor: {classType}");
                return;
            }

            // register arguments
            var argumentsCount = arguments?.Length ?? 0;
            m_Arguments.Add(classType, arguments);

            // register binding to resolve
            //Debug.Log(($"   Add to m_BindingsToResolve {classType}"));
            m_BindingsToResolve.Add(classType);

            // get first constructor from class
            var constructor = constructorsInfo[0];
            m_Constructors.Add(classType, constructor);

            // define dependencies
            m_Dependencies.Add(classType, constructor.GetParameters().Length == 0 ? null : new List<Type>());

            var parameters = constructor.GetParameters();
            
            for (var i = 0; i < parameters.Length - argumentsCount; i++)
            {
                //Debug.Log($"Constructor Param Type: {parameters[i].ParameterType}");
                m_Dependencies[classType].Add(parameters[i].ParameterType);
            }

            // TODO:: properties dependencies
            m_Properties.Add(classType, new List<PropertyInfo>());

            foreach (var property in classType.GetProperties())
            {
                // get [Inject] attribute
                var injectAttribute = Attribute.GetCustomAttribute(property, typeof(InjectAttribute));

                // if there is an attribute
                if(injectAttribute != null)
                {
                    throw new Exception("Property injections are not allowd in this version.");

                    if (m_Dependencies[classType] == null)
                        m_Dependencies[classType] = new List<Type>();

                    m_Dependencies[classType].Add(property.PropertyType);
                    m_Properties[classType].Add(property);
                }
            }
            
        }

        // return binding class if it is interface, else itself
        private Type GetClassType<T>() => GetClassType(typeof(T));

        internal Type GetClassType(Type type)
        {
            if (type.IsInterface)
            {
                if (m_InterfaceBindings.ContainsKey(type))
                    return m_InterfaceBindings[type];
                else
                {
                    Type classType = m_Context.GetClassTypeFromParent(type);

                    if (classType == null)
                        throw new Exception($"Missing interface binding {type}");
                    else
                        return classType;
                }   
            }

            return type;
        }

        private IContext m_Context;

        public void Install(IContext context)
        {
            m_Context = context;

            var typesToInstall = OrderTypesByDependencies(context);

            // foreach (var item in typesToInstall)
            //     Debug.Log($"     {item}");

            foreach (var type in typesToInstall)
            {
                Debug.Log($"<color=lightblue>[inject] installing {type}</color>");
                var constructorParams = GetConstructorParameterObjects(type, context);

                MermaidWriter.AddDependencies(type, constructorParams, GetArgumentsCount(type));

                // if we alredy have this binding
                if (m_Bindings.ContainsKey(type))
                    return;
                
                // instantiate object and inject dependencies in constructor
                var instance = m_Constructors[type].Invoke(constructorParams);
                // inject dependencies in properties
                foreach(var property in m_Properties[type])
                {
                    property.SetValue(instance, GetDependencyObject(property.PropertyType, context));
                }
                // register binding
                m_Bindings.Add(type, instance);
            }
        }

        private int GetArgumentsCount(Type type) => m_Arguments.ContainsKey(type) ? m_Arguments[type].Length : 0;

        public object GetDependencyObject(Type paramType, IContext context)
        {
            // if is settings get from object from context (scriptable objects)
            if (context.HasSettings(paramType))
                return context.GetSettings(paramType);

            // else get object from bindings
            else if (m_Bindings.ContainsKey(paramType))
                return m_Bindings[paramType];

            // not found
            // check object in parent contexts
            var paramObjectInParent = context.GetInstanceFromParent(paramType);

            if (paramObjectInParent == null)
                throw new Exception($"Missing binding {paramType} in context {context.id}");

            return paramObjectInParent;
        }

        private object[] GetConstructorParameterObjects (Type type, IContext context)
        {
            var constructor = m_Constructors[type];
            var dependencies = m_Dependencies[type];
            var properties = m_Properties[type];
            var arguments = m_Arguments[type];

            if (constructor == null || dependencies == null || arguments == null)
                return null;

            if (dependencies.Count + arguments.Length == 0)
                return null;

            var paramObjects = new List<object>();

            for (var i = 0; i < dependencies.Count - properties.Count; i++)
            {
                Type paramType = GetClassType(dependencies[i]);

                paramObjects.Add(GetDependencyObject(paramType, context));
            }

            // add arguments
            if (m_Arguments.ContainsKey(type))
                paramObjects.AddRange(m_Arguments[type]);

            return paramObjects.ToArray();
        }

        private List<Type> OrderTypesByDependencies (IContext context)
        {
            var orderedTypes = new List<Type>();

            while (m_BindingsToResolve.Count != 0)
            {
                Type type = GetNextBinding();

                // remove 
                m_BindingsToResolve.Remove(type);

                // if is not settings from scriptable object
                if (!context.HasSettings(type))
                    // add instance
                    orderedTypes.Add(type);
            }

            return orderedTypes;
        }

        private Type GetNextBinding ()
        {
            if (m_BindingsToResolve.Count == 0)
                return null;

            for (var index = 0; index < m_BindingsToResolve.Count; index++)
            {
                var type = m_BindingsToResolve[index];
                if (CanResolve(type))
                    return type;
            }

            m_BindingsToResolve.Clear();
            Debug.LogError("Unresolved dependencies!");
            return null;
        }

        private bool CanResolve(Type type)
        {
            if (m_Dependencies[type] == null)
                return true;
            
            for (var index = 0; index < m_Dependencies[type].Count; index++)
            {
                var interfaceDependency = m_Dependencies[type][index];
                if (m_BindingsToResolve.Contains(GetClassType(interfaceDependency)))
                    return false;
            }

            return true;
        }

        internal List<T> GetInstancesOfType<T>()
        {
            var instances = new List<T>();

            foreach (var item in m_Bindings.Values)
                if (item is T item1)
                    instances.Add(item1);

            return instances;
        }

        internal object GetInstanceOfType(Type type) => m_Bindings.ContainsKey(type) ? m_Bindings[type] : null;   
    }
}