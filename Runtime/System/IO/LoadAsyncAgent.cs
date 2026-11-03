using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    [DisallowMultipleComponent]
    public class LoadAsyncAgent : BaseMonoBehaviour
    {
        [SerializeField]
        private UnityEvent begin;


        [SerializeField]
        private UnityEvent beginTransitionEnter;
        [SerializeField]
        private UnityEvent beginTransitionExit;
        [SerializeField]
        private UnityEvent endTransitionEnter;
        [SerializeField]
        private UnityEvent endTransitionExit;

        [SerializeField]
        private UnityEvent end;

        protected override void OnInitializing() {}

        /// <summary>
        /// SetActive(true);
        /// SetAsLastSibling();
        /// </summary>
        public virtual IEnumerator BeginLoad()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            begin?.Invoke();
            beginTransitionEnter?.Invoke();
            yield return BeginTransition();
            beginTransitionExit?.Invoke();
        }
        protected internal virtual IEnumerator BeginTransition()
        {
            yield return null;
        }
        protected internal virtual IEnumerator EndTransition()
        {
            yield return null;
        }
        public virtual IEnumerator EndLoad()
        {
            endTransitionEnter?.Invoke();
            yield return EndTransition();
            endTransitionExit?.Invoke();
            gameObject.SetActive(false);
            end?.Invoke();
        }
    } 
}
