using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Parts
{
    public class PartPlugin : MonoBehaviour
    {
        /// <summary>
        /// the Spacecraft this plugin belongs to
        /// </summary>
        public Spacecraft craft;

        /// <summary>
        /// the Part this plugin belongs to
        /// </summary>
        public Part part;

        /*
        /// <summary>
        /// <para>
        ///   .data configuration for this plugin provided in part definition.
        /// </para>
        /// <para>
        ///   these values are shared across plugins from parts of the same type
        /// </para>
        /// <para>
        ///   for example, this can contain properties like engine Isp or max. fuel resource capacity.
        ///   these values apply for the entire part type, not a specific instance of a part
        /// </para>
        /// </summary>
        public DataNode partConfig;
        /// <summary>
        /// <para>
        ///   .data configuration for this plugin from the craft file.
        /// </para>
        /// <para>
        ///   these values differ from a part-to-part basis within a craft.
        /// </para>
        /// <para>
        ///   for example, this can contain properties like current resource amount or engine throttle
        ///   that are relevant only to individual parts
        /// </para>
        /// </summary>
        public DataNode craftConfig;
        */

        public virtual void OnAwake() { }
        public virtual void OnStart() { }
        public virtual void OnFixedUpdate() { }
        public virtual void OnUpdate() { }
        public virtual void OnLateUpdate() { }

        /// <summary>
        /// called when this plugin's data is loaded.
        /// if the plugin is being loaded on part for the first time, this will run AFTER OnAwake().
        /// note that it is not guarenteed that this will run before OnStart().
        /// </summary>
        public virtual void OnLoad(DataNode config) {  }

        private void Awake() { OnAwake(); }
        private void Start() { OnStart(); }
        private void FixedUpdate() { OnFixedUpdate(); }
        private void Update() { OnUpdate(); }
        private void LateUpdate() { OnLateUpdate(); }
    }
}
