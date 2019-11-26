using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using UnityWeld.Binding.Internal;

namespace UnityWeld.Binding
{
    /// <summary>
    /// Bind a property in the view model to a parameter in an Animator, subscribing to OnPropertyChanged 
    /// and updating the Animator parameter accordingly (note that this does not update the view model when
    /// the parameter changes).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("Unity Weld/Animator Parameter Binding")]
    [HelpURL("https://github.com/Real-Serious-Games/Unity-Weld")]
    public class AnimatorParameterBinding : AbstractMemberBinding
    {
        /// <summary>
        /// Type of the adapter we're using to adapt between the view model property and UI property.
        /// </summary>
        public string ViewAdapterId
        {
            get { return viewAdapterId; }
            set { viewAdapterId = value; }
        }

        [FormerlySerializedAs("viewAdapterTypeName")] [SerializeField]
        private string viewAdapterId;

        /// <summary>
        /// Options for adapting from the view model to the UI property.
        /// </summary>
        public AdapterOptions ViewAdapterOptions
        {
            get { return viewAdapterOptions; }
            set { viewAdapterOptions = value; }
        }

        [SerializeField]
        private AdapterOptions viewAdapterOptions;

        /// <summary>
        /// Name of the property in the view model to bind.
        /// </summary>
        public string ViewModelPropertyName
        {
            get { return viewModelPropertyName; }
            set { viewModelPropertyName = value; }
        }

        [SerializeField]
        private string viewModelPropertyName;

        /// <summary>
        /// Parameter name on the Animator
        /// </summary>
        public string AnimatorParameterName
        {
            get { return animatorParameterName; }
            set { animatorParameterName = value; }
        }
        [SerializeField]
        private string animatorParameterName;

        /// <summary>
        /// The parameter type that we are binding to
        /// </summary>
        public AnimatorControllerParameterType AnimatorParameterType
        {
            get { return animatorParameterType; }
            set { animatorParameterType = value; }
        }
        [SerializeField]
        private AnimatorControllerParameterType animatorParameterType;

        /// <summary>
        /// Watches the view-model for changes that must be propagated to the view.
        /// </summary>
        private PropertyWatcher viewModelWatcher;

        /// <summary>
        /// Animator to use
        /// </summary>
        private Animator boundAnimator;

        //Properties to bind to
        public float FloatParameter
        {
            set
            {
                if(boundAnimator != null)
                {
                    boundAnimator.SetFloat(AnimatorParameterName, value);
                }
            }
        }

        public int IntParameter
        {
            set
            {
                if(boundAnimator != null)
                {
                    boundAnimator.SetInteger(AnimatorParameterName, value);
                }
            }
        }

        public bool BoolParameter
        {
            set
            {
                if(boundAnimator != null)
                {
                    boundAnimator.SetBool(AnimatorParameterName, value);
                }
            }
        }

        public bool TriggerParameter
        {
            set
            {
                if (boundAnimator != null)
                {
                    if (value)
                    {
                        boundAnimator.SetTrigger(AnimatorParameterName);
                    }
                    else
                    {
                        boundAnimator.ResetTrigger(AnimatorParameterName);
                    }
                }
            }
        }

        public override void Connect()
        {
            if (boundAnimator == null)
            {
                boundAnimator = GetComponent<Animator>();
            }

            Assert.IsTrue(
                boundAnimator != null,
                "Animator is null!"
            );

            Assert.IsTrue(
                !string.IsNullOrEmpty(AnimatorParameterName),
                "AnimatorParameter is not set"
            );

            string propertyName;
            switch (AnimatorParameterType)
            {
                case AnimatorControllerParameterType.Float:
                    propertyName = "FloatParameter";
                    break;
                case AnimatorControllerParameterType.Int:
                    propertyName = "IntParameter";
                    break;
                case AnimatorControllerParameterType.Bool:
                    propertyName = "BoolParameter";
                    break;
                case AnimatorControllerParameterType.Trigger:
                    propertyName = "TriggerParameter";
                    break;
                default:
                    throw new IndexOutOfRangeException("Unexpected animator parameter type");
            }

            var viewModelEndPoint = MakeViewModelEndPoint(viewModelPropertyName, null, null);

            // If the binding property is an AnimatorParameterTrigger,
            // we change the owner to the instance of the property
            // and change the property to "TriggerSetOrReset"
            if (AnimatorParameterType == AnimatorControllerParameterType.Trigger)
            {
                viewModelEndPoint = new PropertyEndPoint(viewModelEndPoint.GetValue(), "TriggerSetOrReset", null, null, "view-model", this);
            }

            var propertySync = new PropertySync(
                // Source
                viewModelEndPoint,

                // Dest
                new PropertyEndPoint(
                    this,
                    propertyName,
                    TypeResolver.GetAdapter(viewAdapterId),
                    viewAdapterOptions,
                    "Animator",
                    this
                ),

                // Errors, exceptions and validation.
                null, // Validation not needed

                this
            );

            viewModelWatcher = viewModelEndPoint.Watch(
                () => propertySync.SyncFromSource()
            );

            // Copy the initial value over from the view-model.
            propertySync.SyncFromSource();
        }

        public override void Disconnect()
        {
            if (viewModelWatcher != null)
            {
                viewModelWatcher.Dispose();
                viewModelWatcher = null;
            }
        }
    }
}
