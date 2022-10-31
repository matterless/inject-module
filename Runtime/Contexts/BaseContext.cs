using System;
using System.Collections.Generic;
using UnityEngine;

namespace Matterless.Inject
{
    public class BaseContext : MonoBehaviour, IContext
    {
        public enum InitialisationMethod { OnAwake, Manual }

        #region Inspector
        [SerializeField] InitialisationMethod m_InitialisationMethod;
        //[SerializeField] List<ScriptableObjectInstaller> m_ScriptableObjectInstallers;
        [SerializeField] List<MonoInstaller> m_MonoInstallers;
        #endregion

        private DiContainer m_DiContainer = new DiContainer();
        private Dictionary<Type, object> m_SettingsBindings;
        private IContext m_ParentContext;

        private List<IInitializable> m_Initializables;
        private List<IDisposable> m_Disposables;
        private List<ITickable> m_Tickables;
        private List<ILateTickable> m_LateTickables;
        private List<IFixedTickable> m_FixedTickables;
        private object[] m_ContextArguments;

        // root context reference
        private static IContext m_RootContext;
        // contexts registry
        private static Dictionary<string, IContext> m_ContextDictionary = new Dictionary<string, IContext>();
        protected IContext rootContext
        {
            get
            {
                if (m_RootContext == null)
                    m_RootContext = FindObjectOfType<RootContext>();

                return m_RootContext;
            }
        }

        public void StartContext(params object[] arguments)
        {
            m_ContextArguments = arguments;
            StartContextInternal();
        }

        protected virtual void StartContextInternal() { }

        protected IContext GetContextById(string contextId)
        {
            if (string.IsNullOrEmpty(contextId))
                return null;

            if (m_ContextDictionary.ContainsKey(contextId))
                return m_ContextDictionary[contextId];

            throw new Exception($"The parent context is missing: {contextId}");
        }

        protected void Init(IContext parentContext)
        {
            Debug.Log($"<color=cyan>[inject] Starting context: {id}</color>");

            // set parent
            m_ParentContext = parentContext;
            // init settings bindings
            m_SettingsBindings = new Dictionary<Type, object>();

            // install scriptable objects
            //foreach (var scriptableObjectInstaller in m_ScriptableObjectInstallers)
            //    scriptableObjectInstaller.Install(m_SettingsBindings);

            // install mono
            foreach (var monoInstaller in m_MonoInstallers)
                monoInstaller.Install(m_DiContainer, m_ContextArguments); // invoke all bindings

            // install instances
            m_DiContainer.Install(this);

            // get predefined interfaces
            m_Initializables = m_DiContainer.GetInstancesOfType<IInitializable>();
            m_Disposables = m_DiContainer.GetInstancesOfType<IDisposable>();
            m_Tickables = m_DiContainer.GetInstancesOfType<ITickable>();
            m_FixedTickables = m_DiContainer.GetInstancesOfType<IFixedTickable>();
            m_LateTickables = m_DiContainer.GetInstancesOfType<ILateTickable>();

            //invoke IInitializable
            if (m_Initializables != null)
            {
                for (int i = 0; i < m_Initializables.Count; i++)
                    m_Initializables[i].Initialize();
            }

            RegisterContext();
        }

        private void RegisterContext()
        {
            if (m_ContextDictionary.ContainsKey(id))
                throw new Exception($"A context with the same id exists: {id}");

            m_ContextDictionary.Add(id, this);
        }

        #region IContext
        public IContext parent => m_ParentContext;
        public virtual string id => "base";
        public TSettings GetSettings<TSettings>() => (TSettings)m_SettingsBindings[typeof(TSettings)];
        public object GetSettings(Type type) => m_SettingsBindings[type];
        public bool HasSettings(Type type) => m_SettingsBindings.ContainsKey(type);
        public object GetInstanceOfType(Type type) => m_DiContainer.GetInstanceOfType(type);
        public Type GetClassType(Type type) => m_DiContainer.GetClassType(type);

        // recursive search parents of type binding
        public object GetInstanceFromParent(Type type)
        {
            // if root context, end search 
            if (parent == null)
                return null;

            // search in parent
            object obj = parent.GetInstanceOfType(type);

            // if binding exists return 
            if (obj != null)
                return obj;
            // else search in parent's parent
            else
                return parent.GetInstanceFromParent(type);
        }

        // recursive search parents for interface binding
        public Type GetClassTypeFromParent(Type type)
        {
            // if root context, end search 
            if (parent == null)
                return null;

            // search in parent
            Type classType = parent.GetClassType(type);

            // if binding exists return 
            if (classType != null)
                return classType;
            // else search in parent's parent
            else
                return parent.GetClassTypeFromParent(type);
        }
        #endregion

        #region Unity Messages
        protected virtual void Awake()
        {
            if (m_InitialisationMethod != InitialisationMethod.OnAwake)
                return;

            StartContextInternal();
        }

        private void OnDestroy()
        {
            if (m_Disposables == null)
                return;

            for (int i = 0; i < m_Disposables.Count; i++)
                m_Disposables[i].Dispose();
        }

        private void Update()
        {
            if (m_Tickables == null)
                return;

            for (int i = 0; i < m_Tickables.Count; i++)
                m_Tickables[i].Tick(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void LateUpdate()
        {
            if (m_LateTickables == null)
                return;

            for (int i = 0; i < m_LateTickables.Count; i++)
                m_LateTickables[i].LateTick(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void FixedUpdate()
        {
            if (m_FixedTickables == null)
                return;

            for (int i = 0; i < m_FixedTickables.Count; i++)
                m_FixedTickables[i].FixedTick(Time.fixedDeltaTime, Time.fixedUnscaledDeltaTime);
        }
        #endregion
    }
}