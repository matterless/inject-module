using UnityEngine;

namespace Matterless.Inject
{
    [DefaultExecutionOrder(-100)]
    public class ObjectContext : BaseContext
    {
        #region Inspector
        [Tooltip("This is the context id which you can use to define a parent context. Must not be empty.")]
        [SerializeField] string m_ContextId;
        [Tooltip("If this is empty, then the parent is the Root Context.")]
        [SerializeField] string m_ParentContextId;
        #endregion

        private void OnValidate()
        {
            if(m_ContextId != null)
                m_ContextId = m_ContextId.Trim();
            if(m_ParentContextId != null)
                m_ParentContextId = m_ParentContextId.Trim();
        }

        // define id as game object's name
        public override string id => m_ContextId;

        protected override void StartContextInternal()
        {
            if (string.IsNullOrEmpty(m_ContextId))
                throw new System.Exception($"Context id can not be empty: {name}");

            MermaidWriter.SetContext(id);

            // if m_ParentContext == null, set RootContext as parrent
            if (string.IsNullOrEmpty(m_ParentContextId))
                // init with root context as parent
                Init(rootContext);
            else
                // init with other parent context
                Init(GetContextById(m_ParentContextId));
            
            // complete mermaid
            MermaidWriter.Complete(id);
        }
    }
}