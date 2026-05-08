// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrTeleportLocomotionBaseEditor.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Locomotion;
using UnityEditor;
using UnityEngine;

#pragma warning disable 0414

namespace UltimateXR.Editor.Locomotion
{
    /// <summary>
    ///     Base class for custom teleport locomotion components.
    /// </summary>
    public abstract class UxrTeleportLocomotionBaseEditor : UnityEditor.Editor
    {
        #region Unity

        /// <summary>
        ///     Creates references to the serialized properties
        /// </summary>
        protected virtual void OnEnable()
        {
            _propControllerHand                = serializedObject.FindProperty("_controllerHand");
            _propUseControllerForward          = serializedObject.FindProperty("_useControllerForward");
            _propParentToDestination           = serializedObject.FindProperty("_parentToDestination");
            _propShakeFilter                   = serializedObject.FindProperty("_shakeFilter");
            _propTranslationType               = serializedObject.FindProperty("_translationType");
            _propFadeTranslationColor          = serializedObject.FindProperty("_fadeTranslationColor");
            _propFadeTranslationSeconds        = serializedObject.FindProperty("_fadeTranslationSeconds");
            _propInterpolateTranslationSeconds = serializedObject.FindProperty("_interpolateTranslationSeconds");
            _propAllowJoystickBackStep         = serializedObject.FindProperty("_allowJoystickBackStep");
            _propBackStepDistance              = serializedObject.FindProperty("_backStepDistance");
            _propTurnType                      = serializedObject.FindProperty("_turnType");
            _propTurnStepDegrees               = serializedObject.FindProperty("_turnStepDegrees");
            _propFadeTurnColor                 = serializedObject.FindProperty("_fadeTurnColor");
            _propFadeTurnSeconds               = serializedObject.FindProperty("_fadeTurnSeconds");
            _propInterpolateTurnSeconds        = serializedObject.FindProperty("_interpolateTurnSeconds");
            _propSmoothTurnSpeedDeg            = serializedObject.FindProperty("_smoothTurnSpeedDeg");
            _propReorientationType             = serializedObject.FindProperty("_reorientationType");

            _propTarget                      = serializedObject.FindProperty("_target");
            _propTargetPlacementAboveHit     = serializedObject.FindProperty("_targetPlacementAboveHit");
            _propShowTargetAlsoWhenInvalid   = serializedObject.FindProperty("_showTargetAlsoWhenInvalid");
            _propValidMaterialColorTargets   = serializedObject.FindProperty("_validMaterialColorTargets");
            _propInvalidMaterialColorTargets = serializedObject.FindProperty("_invalidMaterialColorTargets");

            _propTriggerCollidersInteraction        = serializedObject.FindProperty("_triggerCollidersInteraction");
            _propMaxAllowedDistance                 = serializedObject.FindProperty("_maxAllowedDistance");
            _propMaxAllowedHeightDifference         = serializedObject.FindProperty("_maxAllowedHeightDifference");
            _propMaxAllowedSlopeDegrees             = serializedObject.FindProperty("_maxAllowedSlopeDegrees");
            _propDestinationValidationRadius        = serializedObject.FindProperty("_destinationValidationRadius");
            _propDestinationValidationMaxStepHeight = serializedObject.FindProperty("_destinationValidationMaxStepHeight");
            _propValidTargetLayers                  = serializedObject.FindProperty("_validTargetLayers");
            _propBlockingTargetLayers               = serializedObject.FindProperty("_blockingTargetLayers");
        }

        /// <summary>
        ///     Draws the custom inspector
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();

            _foldoutGeneral = UxrEditorUtils.FoldoutStylish("General Parameters", _foldoutGeneral);

            if (_foldoutGeneral)
            {
                EditorGUILayout.PropertyField(_propControllerHand,       ContentControllerHand);
                EditorGUILayout.PropertyField(_propUseControllerForward, ContentUseControllerForward);
                EditorGUILayout.PropertyField(_propParentToDestination,  ContentParentToDestination);
                EditorGUILayout.Slider(_propShakeFilter, 0.0f, 1.0f, ContentShakeFilter);
            }

            _foldoutTranslation = UxrEditorUtils.FoldoutStylish("Translation", _foldoutTranslation);

            if (_foldoutTranslation)
            {
                EditorGUILayout.PropertyField(_propTranslationType, ContentTranslationType);

                if (_propTranslationType.enumValueIndex == (int)UxrTranslationType.Interpolate && _propReorientationType.enumValueIndex != (int)UxrReorientationType.KeepOrientation)
                {
                    EditorGUILayout.HelpBox("For Interpolate translation it is recommended to use Keep Orientation as Reorient After Teleport parameter in the Turn settings", MessageType.Warning);
                }

                if (_propTranslationType.enumValueIndex == (int)UxrTranslationType.Fade)
                {
                    EditorGUILayout.PropertyField(_propFadeTranslationColor, ContentFadeTranslationColor);
                    EditorGUILayout.Slider(_propFadeTranslationSeconds, 0.01f, 2.0f, ContentFadeTranslationSeconds);
                }
                else if (_propTranslationType.enumValueIndex == (int)UxrTranslationType.Interpolate)
                {
                    EditorGUILayout.Slider(_propInterpolateTranslationSeconds, 0.01f, 2.0f, ContentInterpolateTranslationSeconds);
                }

                EditorGUILayout.PropertyField(_propAllowJoystickBackStep, ContentAllowJoystickBackStep);
                EditorGUILayout.PropertyField(_propBackStepDistance,      ContentBackStepDistance);
            }

            _foldoutTurn = UxrEditorUtils.FoldoutStylish("Turn", _foldoutTurn);

            if (_foldoutTurn)
            {
                EditorGUILayout.PropertyField(_propTurnType, ContentTurnType);

                if (_propTurnType.enumValueIndex != (int)UxrTurnType.NotAllowed && _propTurnType.enumValueIndex != (int)UxrTurnType.Smooth)
                {
                    EditorGUILayout.Slider(_propTurnStepDegrees, 10.0f, 180.0f, ContentTurnStepDegrees);
                }

                if (_propTurnType.enumValueIndex == (int)UxrTurnType.Fade)
                {
                    EditorGUILayout.PropertyField(_propFadeTurnColor, ContentFadeTurnColor);
                    EditorGUILayout.Slider(_propFadeTurnSeconds, 0.01f, 2.0f, ContentFadeTurnSeconds);
                }

                if (_propTurnType.enumValueIndex == (int)UxrTurnType.Interpolate)
                {
                    EditorGUILayout.Slider(_propInterpolateTurnSeconds, 0.01f, 2.0f, ContentInterpolateTurnSeconds);
                }

                if (_propTurnType.enumValueIndex == (int)UxrTurnType.Smooth)
                {
                    EditorGUILayout.PropertyField(_propSmoothTurnSpeedDeg, ContentSmoothTurnSpeedDeg);
                }

                EditorGUILayout.PropertyField(_propReorientationType, ContentReorientationType);
            }

            EditorGUILayout.Space();

            _foldoutTarget = UxrEditorUtils.FoldoutStylish("Target", _foldoutTarget);

            if (_foldoutTarget)
            {
                Object previousTarget = _propTarget.objectReferenceValue;
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_propTarget, ContentTarget);
                if (EditorGUI.EndChangeCheck())
                {
                    Object newTarget = _propTarget.objectReferenceValue;

                    if (newTarget is Component targetComponent && !targetComponent.transform.IsChildOf(((Component)serializedObject.targetObject).transform))
                    {
                        EditorUtility.DisplayDialog("Invalid Target", "The target must be a reference in the same prefab.", "OK");
                        _propTarget.objectReferenceValue = previousTarget;
                    }
                }

                EditorGUILayout.Slider(_propTargetPlacementAboveHit, 0.0f, 1.0f, ContentTargetPlacementAboveHit);
                EditorGUILayout.PropertyField(_propShowTargetAlsoWhenInvalid, ContentShowTargetAlsoWhenInvalid);

                if (_propShowTargetAlsoWhenInvalid.boolValue)
                {
                    EditorGUILayout.PropertyField(_propValidMaterialColorTargets,   ContentValidMaterialColorTargets);
                    EditorGUILayout.PropertyField(_propInvalidMaterialColorTargets, ContentInvalidMaterialColorTargets);
                }
            }

            EditorGUILayout.Space();

            _foldoutConstraints = UxrEditorUtils.FoldoutStylish("Constraints", _foldoutConstraints);

            if (_foldoutConstraints)
            {
                EditorGUILayout.PropertyField(_propTriggerCollidersInteraction, ContentTriggerCollidersInteraction);
                EditorGUILayout.PropertyField(_propMaxAllowedDistance,          ContentMaxAllowedDistance);
                EditorGUILayout.PropertyField(_propMaxAllowedHeightDifference,  ContentMaxAllowedHeightDifference);
                EditorGUILayout.Slider(_propMaxAllowedSlopeDegrees, 0.0f, 90.0f, ContentMaxAllowedSlopeDegrees);
                EditorGUILayout.PropertyField(_propDestinationValidationRadius,        ContentDestinationValidationRadius);
                EditorGUILayout.PropertyField(_propDestinationValidationMaxStepHeight, ContentDestinationValidationMaxStepHeight);
                EditorGUILayout.PropertyField(_propValidTargetLayers,                  ContentValidTargetLayers);
                EditorGUILayout.PropertyField(_propBlockingTargetLayers,               ContentBlockingTargetLayers);
            }

            OnTeleportInspectorGUI();

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Event Trigger Methods

        protected abstract void OnTeleportInspectorGUI();

        #endregion

        #region Private Types & Data

        private GUIContent ContentControllerHand                     { get; } = new GUIContent("Controller Hand",                        "Which hand controls the input");
        private GUIContent ContentUseControllerForward               { get; } = new GUIContent("Use Controller Forward",                 "Will the teleport use the controller's forward vector instead of its own transform forward?");
        private GUIContent ContentParentToDestination                { get; } = new GUIContent("Parent To Destination",                  "Whether to parent the avatar to the destination object after teleport. Use it when building applications with moving vehicles or platforms the avatar can move on, so that the avatar keeps the relative position/orientation after teleporting.");
        private GUIContent ContentShakeFilter                        { get; } = new GUIContent("Shake Filter",                           "The amount of filtering to apply to the hand movement to smooth it out");
        private GUIContent ContentTranslationType                    { get; } = new GUIContent("Translation Type",                       "Which translation method to use");
        private GUIContent ContentFadeTranslationColor               { get; } = new GUIContent("Translation Fade Color",                 "The fade color when Fade translation type is used");
        private GUIContent ContentFadeTranslationSeconds             { get; } = new GUIContent("Translation Fade Seconds",               "The fade transition in seconds when Fade translation type is used");
        private GUIContent ContentInterpolateTranslationSeconds      { get; } = new GUIContent("Interpolate Translation Seconds",        "The translation duration in seconds when Interpolate translation type is used");
        private GUIContent ContentAllowJoystickBackStep              { get; } = new GUIContent("Allow Joystick Back Step",               "Whether to allow back steps by pressing the joystick down");
        private GUIContent ContentBackStepDistance                   { get; } = new GUIContent("Back Step Distance",                     "The distance of each back step");
        private GUIContent ContentTurnType                           { get; } = new GUIContent("Turn Type",                              "Which turn method to use");
        private GUIContent ContentTurnStepDegrees                    { get; } = new GUIContent("Turn Step Degrees",                      "The amount of degrees of each turn for Snap, Fade and Interpolate turns");
        private GUIContent ContentFadeTurnColor                      { get; } = new GUIContent("Turn Fade Color",                        "The fade color when Fade turn is used");
        private GUIContent ContentFadeTurnSeconds                    { get; } = new GUIContent("Turn Fade Seconds",                      "The fade transition in seconds when Fade turn is used");
        private GUIContent ContentInterpolateTurnSeconds             { get; } = new GUIContent("Interpolate Turn Seconds",               "The turn duration in seconds when Interpolate turn is used");
        private GUIContent ContentSmoothTurnSpeedDeg                 { get; } = new GUIContent("Turn Speed (Deg/Sec)",                   "The turn speed in degrees per second");
        private GUIContent ContentReorientationType                  { get; } = new GUIContent("Reorient After Teleport",                "How to orient the view right after teleporting");
        private GUIContent ContentTarget                             { get; } = new GUIContent("Target",                                 "A reference to the teleport target.");
        private GUIContent ContentTargetPlacementAboveHit            { get; } = new GUIContent("Target Placement Above Floor",           "Offset applied to the teleport target to help placing it a little above the floor");
        private GUIContent ContentShowTargetAlsoWhenInvalid          { get; } = new GUIContent("Show Target Also When Invalid",          "Whether to show the target object also when the destination is not valid");
        private GUIContent ContentValidMaterialColorTargets          { get; } = new GUIContent("Target Color When Valid",                "Target color to use when the destination is valid");
        private GUIContent ContentInvalidMaterialColorTargets        { get; } = new GUIContent("Target Color When Invalid",              "Target color to use when the destination is not valid");
        private GUIContent ContentTriggerCollidersInteraction        { get; } = new GUIContent("Trigger Colliders Interaction",          "Whether colliders with the trigger property set will interact with the teleport raycasts");
        private GUIContent ContentMaxAllowedDistance                 { get; } = new GUIContent("Max Allowed Distance Travel",            "Maximum allowed distance that can be travelled using each teleport");
        private GUIContent ContentMaxAllowedHeightDifference         { get; } = new GUIContent("Max Allowed Height Difference",          "Maximum allowed height difference to be able to teleport");
        private GUIContent ContentMaxAllowedSlopeDegrees             { get; } = new GUIContent("Max Allowed Slope Degrees",              "Maximum allowed slope degrees at destination");
        private GUIContent ContentDestinationValidationRadius        { get; } = new GUIContent("Destination Validation Radius",          "Radius of a cylinder that will be used to validate the destination surroundings to allow teleporting");
        private GUIContent ContentDestinationValidationMaxStepHeight { get; } = new GUIContent("Destination Validation Max Step Height", "The maximum step height when validating the surroundings of a teleport destination.");
        private GUIContent ContentValidTargetLayers                  { get; } = new GUIContent("Valid Target Layers",                    "Valid layers for teleporting destination objects");
        private GUIContent ContentBlockingTargetLayers               { get; } = new GUIContent("Blocking Target Layers",                 "Objects that will block teleporting raycasts");

        private SerializedProperty _propControllerHand;
        private SerializedProperty _propUseControllerForward;
        private SerializedProperty _propParentToDestination;
        private SerializedProperty _propShakeFilter;
        private SerializedProperty _propTranslationType;
        private SerializedProperty _propFadeTranslationColor;
        private SerializedProperty _propFadeTranslationSeconds;
        private SerializedProperty _propInterpolateTranslationSeconds;
        private SerializedProperty _propAllowJoystickBackStep;
        private SerializedProperty _propBackStepDistance;
        private SerializedProperty _propTurnType;
        private SerializedProperty _propTurnStepDegrees;
        private SerializedProperty _propFadeTurnColor;
        private SerializedProperty _propFadeTurnSeconds;
        private SerializedProperty _propInterpolateTurnSeconds;
        private SerializedProperty _propSmoothTurnSpeedDeg;
        private SerializedProperty _propReorientationType;

        private SerializedProperty _propTarget;
        private SerializedProperty _propTargetPlacementAboveHit;
        private SerializedProperty _propShowTargetAlsoWhenInvalid;
        private SerializedProperty _propValidMaterialColorTargets;
        private SerializedProperty _propInvalidMaterialColorTargets;

        private SerializedProperty _propTriggerCollidersInteraction;
        private SerializedProperty _propMaxAllowedDistance;
        private SerializedProperty _propMaxAllowedHeightDifference;
        private SerializedProperty _propMaxAllowedSlopeDegrees;
        private SerializedProperty _propDestinationValidationRadius;
        private SerializedProperty _propDestinationValidationMaxStepHeight;
        private SerializedProperty _propValidTargetLayers;
        private SerializedProperty _propBlockingTargetLayers;

        private bool _foldoutGeneral     = true;
        private bool _foldoutTranslation = true;
        private bool _foldoutTurn        = true;
        private bool _foldoutTarget      = true;
        private bool _foldoutConstraints = true;

        #endregion
    }
}

#pragma warning restore 0414