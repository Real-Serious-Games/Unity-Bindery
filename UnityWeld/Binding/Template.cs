using UnityEngine;
using UnityEngine.Assertions;

namespace UnityWeld.Binding
{
    /// <summary>
    /// Template for use in collection bindings.
    /// </summary>
    public interface ITemplate
    {
        /// <summary>
        /// Set the view model and initialise all binding objects down the hierarchy.
        /// </summary>
        void InitChildBindings(object viewModel);
    }

    /// <summary>
    /// Template for use in collection bindings.
    /// </summary>
    [AddComponentMenu("Unity Weld/Template")]
    [HelpURL("https://github.com/Real-Serious-Games/Unity-Weld")]
    public class Template : MonoBehaviour, IViewModelProvider, ITemplate
    {
        /// <summary>
        /// Get the view-model provided by this provider.
        /// </summary>
        public object GetViewModel()
        {
            return viewModel;
        }

        /// <summary>
        /// Get the name of the view-model's type.
        /// </summary>
        public string GetViewModelTypeName()
        {
            return viewModelTypeName;
        }

        public string ViewModelTypeName
        {
            get { return viewModelTypeName; }
            set { viewModelTypeName = value; }
        }

        [SerializeField]
        private string viewModelTypeName = string.Empty;

        /// <summary>
        /// Cached view-model object.
        /// </summary>
        private object viewModel;

        /// <summary>
        /// Set the view model and initialise all binding objects down the hierarchy.
        /// </summary>
        public void InitChildBindings(object viewModel)
        {
            Assert.IsNotNull(viewModel, "Cannot initialise child bindings with null view model.");

            // Set the bound view to the new view model.
            this.viewModel = viewModel;

            var buffer = Buffer.AbstractMemberBindings;
            GetComponentsInChildren<AbstractMemberBinding>(buffer);
            foreach (var binding in buffer)
            {
                binding.Init();
            }
        }
    }
} 